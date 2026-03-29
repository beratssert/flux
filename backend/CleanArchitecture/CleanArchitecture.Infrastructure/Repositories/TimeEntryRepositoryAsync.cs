using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class TimeEntryRepositoryAsync : GenericRepositoryAsync<TimeEntry>, ITimeEntryRepositoryAsync
    {
        private readonly DbSet<TimeEntry> _timeEntries;
        private readonly DbSet<Project> _projects;

        public TimeEntryRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _timeEntries = dbContext.Set<TimeEntry>();
            _projects = dbContext.Set<Project>();
        }

        public Task<TimeEntry> GetByIdAndUserIdAsync(int id, string userId)
        {
            return _timeEntries.FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId && te.DeletedAtUtc == null);
        }

        public async Task<IReadOnlyList<TimeEntry>> GetPagedByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            return await _timeEntries
                .Where(te => te.UserId == userId && te.DeletedAtUtc == null)
                .OrderByDescending(te => te.EntryDate)
                .ThenByDescending(te => te.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

            public Task<int> CountByUserIdAsync(string userId)
            {
                return _timeEntries.CountAsync(te => te.UserId == userId && te.DeletedAtUtc == null);
            }

        public async Task<IReadOnlyList<TimeEntry>> GetPagedByManagedProjectsAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = BuildManagedProjectsQuery(managerUserId, projectId, employeeUserId, from, to);

            return await query
                .OrderByDescending(te => te.EntryDate)
                .ThenByDescending(te => te.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountByManagedProjectsAsync(
            string managerUserId,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            return BuildManagedProjectsQuery(managerUserId, projectId, employeeUserId, from, to).CountAsync();
        }

        public async Task<IReadOnlyList<TeamProjectSummaryDto>> GetProjectSummaryByManagedProjectsAsync(
            string managerUserId,
            DateTime? from = null,
            DateTime? to = null,
            string employeeUserId = null)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _timeEntries.Where(te =>
                te.DeletedAtUtc == null &&
                managedProjectIds.Contains(te.ProjectId) &&
                te.UserId != managerUserId);

            if (!string.IsNullOrWhiteSpace(employeeUserId))
            {
                query = query.Where(te => te.UserId == employeeUserId);
            }

            if (from.HasValue)
            {
                query = query.Where(te => te.EntryDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(te => te.EntryDate <= to.Value.Date);
            }

            return await query
                .GroupBy(te => te.ProjectId)
                .Select(group => new TeamProjectSummaryDto
                {
                    ProjectId = group.Key,
                    TotalDurationMinutes = group.Sum(x => x.DurationMinutes),
                    EntryCount = group.Count(),
                    EmployeeCount = group.Select(x => x.UserId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalDurationMinutes)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TeamPeriodSummaryDto>> GetPeriodSummaryByManagedProjectsAsync(
            string managerUserId,
            DateTime? from = null,
            DateTime? to = null,
            int? projectId = null,
            string employeeUserId = null)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _timeEntries.Where(te =>
                te.DeletedAtUtc == null &&
                managedProjectIds.Contains(te.ProjectId) &&
                te.UserId != managerUserId);

            if (projectId.HasValue)
            {
                query = query.Where(te => te.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(employeeUserId))
            {
                query = query.Where(te => te.UserId == employeeUserId);
            }

            if (from.HasValue)
            {
                query = query.Where(te => te.EntryDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(te => te.EntryDate <= to.Value.Date);
            }

            return await query
                .GroupBy(te => te.EntryDate.Date)
                .Select(group => new TeamPeriodSummaryDto
                {
                    EntryDate = group.Key,
                    TotalDurationMinutes = group.Sum(x => x.DurationMinutes),
                    EntryCount = group.Count(),
                    ProjectCount = group.Select(x => x.ProjectId).Distinct().Count(),
                    EmployeeCount = group.Select(x => x.UserId).Distinct().Count()
                })
                .OrderBy(x => x.EntryDate)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<bool> HasOverlappingEntryAsync(string userId, DateTime startUtc, DateTime endUtc, int? excludeId = null)
        {
            var query = _timeEntries.Where(te =>
                te.UserId == userId &&
                te.DeletedAtUtc == null &&
                te.StartTimeUtc.HasValue &&
                te.EndTimeUtc.HasValue &&
                te.StartTimeUtc.Value < endUtc &&
                startUtc < te.EndTimeUtc.Value);

            if (excludeId.HasValue)
            {
                query = query.Where(te => te.Id != excludeId.Value);
            }

            return query.AnyAsync();
        }

        private IQueryable<TimeEntry> BuildManagedProjectsQuery(
            string managerUserId,
            int? projectId,
            string employeeUserId,
            DateTime? from,
            DateTime? to)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _timeEntries.Where(te =>
                te.DeletedAtUtc == null &&
                managedProjectIds.Contains(te.ProjectId) &&
                te.UserId != managerUserId);

            if (projectId.HasValue)
            {
                query = query.Where(te => te.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(employeeUserId))
            {
                query = query.Where(te => te.UserId == employeeUserId);
            }

            if (from.HasValue)
            {
                query = query.Where(te => te.EntryDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(te => te.EntryDate <= to.Value.Date);
            }

            return query;
        }
    }
}
