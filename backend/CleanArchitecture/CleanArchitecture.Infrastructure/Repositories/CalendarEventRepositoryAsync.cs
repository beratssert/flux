using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class CalendarEventRepositoryAsync : GenericRepositoryAsync<CalendarEvent>, ICalendarEventRepositoryAsync
    {
        private readonly DbSet<CalendarEvent> _calendarEvents;
        private readonly DbSet<ProjectAssignment> _projectAssignments;
        private readonly DbSet<Project> _projects;

        public CalendarEventRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _calendarEvents = dbContext.Set<CalendarEvent>();
            _projectAssignments = dbContext.Set<ProjectAssignment>();
            _projects = dbContext.Set<Project>();
        }

        public Task<CalendarEvent> GetByIdAndUserAsync(int id, string userId, bool isManager)
        {
            if (isManager)
            {
                // Managers can edit events they created or on projects they manage
                var managedProjectIds = _projects
                    .Where(p => p.ManagerUserId == userId)
                    .Select(p => p.Id);

                return _calendarEvents.FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.DeletedAtUtc == null &&
                    (e.CreatedByUserId == userId || (e.ProjectId != null && managedProjectIds.Contains(e.ProjectId.Value))));
            }

            return _calendarEvents.FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.DeletedAtUtc == null &&
                e.CreatedByUserId == userId);
        }

        public async Task<IReadOnlyList<CalendarEvent>> GetForUserAsync(
            string userId,
            bool isManager,
            DateTime from,
            DateTime to,
            int? projectId = null)
        {
            IQueryable<CalendarEvent> query;

            if (isManager)
            {
                var managedProjectIds = _projects
                    .Where(p => p.ManagerUserId == userId)
                    .Select(p => p.Id);

                // Managers see: Personal events they created + Project/Team events on their managed projects
                query = _calendarEvents.Where(e =>
                    e.DeletedAtUtc == null &&
                    (
                        e.CreatedByUserId == userId ||
                        (e.ProjectId != null && managedProjectIds.Contains(e.ProjectId.Value))
                    ));
            }
            else
            {
                // Employees see: Events they created + Project/Team events on projects they are assigned to
                var assignedProjectIds = _projectAssignments
                    .Where(pa => pa.UserId == userId && pa.IsActive)
                    .Select(pa => pa.ProjectId);

                query = _calendarEvents.Where(e =>
                    e.DeletedAtUtc == null &&
                    (
                        e.CreatedByUserId == userId ||
                        (e.Visibility != "Personal" && e.ProjectId != null && assignedProjectIds.Contains(e.ProjectId.Value))
                    ));
            }

            // Filter by date range: event overlaps [from, to]
            query = query.Where(e => e.StartUtc <= to && e.EndUtc >= from);

            if (projectId.HasValue)
            {
                query = query.Where(e => e.ProjectId == projectId.Value);
            }

            return await query
                .OrderBy(e => e.StartUtc)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
