using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class CalendarEventRepositoryAsync : ICalendarEventRepositoryAsync
    {
        private readonly ApplicationDbContext _db;

        public CalendarEventRepositoryAsync(ApplicationDbContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<CalendarEvent> AddAsync(
            CalendarEvent calendarEvent,
            IReadOnlyList<CalendarEventParticipant> participants,
            CancellationToken cancellationToken = default)
        {
            calendarEvent.Id = calendarEvent.Id == Guid.Empty ? Guid.NewGuid() : calendarEvent.Id;
            foreach (var p in participants)
            {
                p.EventId = calendarEvent.Id;
                calendarEvent.Participants.Add(p);
            }

            await _db.CalendarEvents.AddAsync(calendarEvent, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return calendarEvent;
        }

        public Task<CalendarEvent> GetByIdWithParticipantsAsync(Guid id, bool tracked, CancellationToken cancellationToken = default)
        {
            var q = _db.CalendarEvents.AsQueryable();
            if (!tracked)
            {
                q = q.AsNoTracking();
            }

            return q.Include(e => e.Participants).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public async Task<CalendarEvent> GetByIdVisibleAsync(Guid id, string userId, string role, CancellationToken cancellationToken = default)
        {
            var e = await GetByIdWithParticipantsAsync(id, false, cancellationToken);
            if (e == null)
            {
                return null;
            }

            return await CanUserViewAsync(e, userId, role, cancellationToken) ? e : null;
        }

        public async Task<(IReadOnlyList<CalendarEvent> Items, int TotalCount)> GetPagedVisibleAsync(
            CalendarEventListCriteria c,
            CancellationToken cancellationToken = default)
        {
            IQueryable<CalendarEvent> q = _db.CalendarEvents.AsNoTracking().Include(e => e.Participants);

            if (c.FromUtc.HasValue)
            {
                q = q.Where(e => e.EndAtUtc > c.FromUtc.Value);
            }

            if (c.ToUtc.HasValue)
            {
                q = q.Where(e => e.StartAtUtc < c.ToUtc.Value);
            }

            if (c.ProjectId.HasValue)
            {
                q = q.Where(e => e.ProjectId == c.ProjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(c.VisibilityType))
            {
                var v = c.VisibilityType.Trim();
                q = q.Where(e => e.VisibilityType == v);
            }

            if (!string.IsNullOrWhiteSpace(c.FilterUserId))
            {
                var uid = c.FilterUserId.Trim();
                q = q.Where(e =>
                    e.Participants.Any(p => p.UserId == uid) ||
                    (e.ProjectId != null &&
                     _db.ProjectAssignments.Any(pa =>
                         pa.ProjectId == e.ProjectId &&
                         pa.UserId == uid &&
                         pa.IsActive)));
            }

            q = ApplyVisibilityFilter(q, c.CurrentUserId, c.CurrentRole);

            var total = await q.CountAsync(cancellationToken);
            var page = c.Page < 1 ? 1 : c.Page;
            var size = c.PageSize < 1 ? 20 : Math.Min(c.PageSize, 100);
            var items = await q
                .OrderBy(e => e.StartAtUtc)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task UpdateAsync(CalendarEvent calendarEvent, IReadOnlyList<CalendarEventParticipant> participants, CancellationToken cancellationToken = default)
        {
            var existing = await _db.CalendarEvents
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == calendarEvent.Id, cancellationToken);
            if (existing == null)
            {
                throw new NotFoundException("Calendar event not found.");
            }

            existing.Title = calendarEvent.Title;
            existing.Description = calendarEvent.Description;
            existing.StartAtUtc = calendarEvent.StartAtUtc;
            existing.EndAtUtc = calendarEvent.EndAtUtc;
            existing.VisibilityType = calendarEvent.VisibilityType;
            existing.IsAllDay = calendarEvent.IsAllDay;
            existing.ProjectId = calendarEvent.ProjectId;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            _db.CalendarEventParticipants.RemoveRange(existing.Participants);
            existing.Participants.Clear();
            foreach (var p in participants)
            {
                p.EventId = existing.Id;
                existing.Participants.Add(p);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existing = await _db.CalendarEvents
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (existing == null)
            {
                return;
            }

            _db.CalendarEvents.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> AreAllUsersActiveAssigneesOnManagedProjectsAsync(
            string managerUserId,
            IReadOnlyCollection<string> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds == null || userIds.Count == 0)
            {
                return false;
            }

            var distinct = userIds.Distinct().ToList();
            var covered = await (
                from pa in _db.ProjectAssignments.AsNoTracking()
                join p in _db.Projects.AsNoTracking() on pa.ProjectId equals p.Id
                where pa.IsActive && p.ManagerUserId == managerUserId && distinct.Contains(pa.UserId)
                select pa.UserId).Distinct().ToListAsync(cancellationToken);

            return distinct.All(id => covered.Contains(id));
        }

        private IQueryable<CalendarEvent> ApplyVisibilityFilter(IQueryable<CalendarEvent> query, string userId, string role)
        {
            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return query;
            }

            if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return query.Where(e =>
                    (e.ProjectId != null &&
                     _db.Projects.Any(p => p.Id == e.ProjectId && p.ManagerUserId == userId)) ||
                    (e.VisibilityType == CalendarVisibilityTypes.Personal &&
                     (e.CreatedByUserId == userId || e.Participants.Any(p => p.UserId == userId))));
            }

            return query.Where(e =>
                (e.VisibilityType == CalendarVisibilityTypes.Personal && e.Participants.Any(p => p.UserId == userId)) ||
                ((e.VisibilityType == CalendarVisibilityTypes.Project || e.VisibilityType == CalendarVisibilityTypes.Team) &&
                 e.ProjectId != null &&
                 _db.ProjectAssignments.Any(pa =>
                     pa.ProjectId == e.ProjectId &&
                     pa.UserId == userId &&
                     pa.IsActive)));
        }

        private async Task<bool> CanUserViewAsync(CalendarEvent e, string userId, string role, CancellationToken cancellationToken)
        {
            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (e.ProjectId.HasValue &&
                    await _db.Projects.AsNoTracking().AnyAsync(
                        p => p.Id == e.ProjectId && p.ManagerUserId == userId,
                        cancellationToken))
                {
                    return true;
                }

                if (e.VisibilityType == CalendarVisibilityTypes.Personal &&
                    (e.CreatedByUserId == userId || e.Participants.Any(p => p.UserId == userId)))
                {
                    return true;
                }

                return false;
            }

            if (e.VisibilityType == CalendarVisibilityTypes.Personal)
            {
                return e.Participants.Any(p => p.UserId == userId);
            }

            if (e.ProjectId.HasValue &&
                (e.VisibilityType == CalendarVisibilityTypes.Project || e.VisibilityType == CalendarVisibilityTypes.Team))
            {
                return await _db.ProjectAssignments.AsNoTracking().AnyAsync(
                    pa => pa.ProjectId == e.ProjectId && pa.UserId == userId && pa.IsActive,
                    cancellationToken);
            }

            return false;
        }
    }
}
