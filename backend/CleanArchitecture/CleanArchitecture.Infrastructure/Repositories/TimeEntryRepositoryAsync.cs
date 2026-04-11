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

        public async Task<IReadOnlyList<TimeEntry>> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = BuildUserQuery(userId, projectId, from, to, isBillable);

            return await ApplySorting(query, sortBy, sortDir)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountByUserIdAsync(
            string userId,
            int? projectId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null)
        {
            return BuildUserQuery(userId, projectId, from, to, isBillable).CountAsync();
        }

        public async Task<IReadOnlyList<TimeEntry>> GetPagedByManagedProjectsAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = BuildManagedProjectsQuery(managerUserId, projectId, employeeUserId, from, to, isBillable);

            return await ApplySorting(query, sortBy, sortDir)
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
            DateTime? to = null,
            bool? isBillable = null)
        {
            return BuildManagedProjectsQuery(managerUserId, projectId, employeeUserId, from, to, isBillable).CountAsync();
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

        public async Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsByUserAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _timeEntries.Where(te => te.UserId == userId && te.DeletedAtUtc == null);

            if (from.HasValue)
            {
                query = query.Where(te => te.EntryDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(te => te.EntryDate <= to.Value.Date);
            }

            return await query
                .Select(te => new TimeSummaryRowDto
                {
                    UserId = te.UserId,
                    ProjectId = te.ProjectId,
                    EntryDate = te.EntryDate,
                    DurationMinutes = te.DurationMinutes
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsByManagedProjectsAsync(
            string managerUserId,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = BuildManagedProjectsQuery(managerUserId, projectId, employeeUserId, from, to, null);

            return await query
                .Select(te => new TimeSummaryRowDto
                {
                    UserId = te.UserId,
                    ProjectId = te.ProjectId,
                    EntryDate = te.EntryDate,
                    DurationMinutes = te.DurationMinutes
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsAllAsync(
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _timeEntries.Where(te => te.DeletedAtUtc == null);

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
                .Select(te => new TimeSummaryRowDto
                {
                    UserId = te.UserId,
                    ProjectId = te.ProjectId,
                    EntryDate = te.EntryDate,
                    DurationMinutes = te.DurationMinutes
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<(int TotalMinutes, int TotalEntries, int BillableEntries)> GetProjectAggregateByManagedProjectsAsync(
            string managerUserId,
            int projectId)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _timeEntries.Where(te =>
                te.DeletedAtUtc == null &&
                te.ProjectId == projectId &&
                managedProjectIds.Contains(te.ProjectId));

            var result = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalMinutes = g.Sum(x => x.DurationMinutes),
                    TotalEntries = g.Count(),
                    BillableEntries = g.Count(x => x.IsBillable)
                })
                .FirstOrDefaultAsync();

            return result == null
                ? (0, 0, 0)
                : (result.TotalMinutes, result.TotalEntries, result.BillableEntries);
        }

        public async Task<(int TotalMinutes, int TotalEntries, int BillableEntries)> GetProjectAggregateAllAsync(int projectId)
        {
            var query = _timeEntries.Where(te => te.DeletedAtUtc == null && te.ProjectId == projectId);
            var result = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalMinutes = g.Sum(x => x.DurationMinutes),
                    TotalEntries = g.Count(),
                    BillableEntries = g.Count(x => x.IsBillable)
                })
                .FirstOrDefaultAsync();

            return result == null
                ? (0, 0, 0)
                : (result.TotalMinutes, result.TotalEntries, result.BillableEntries);
        }

        public Task<bool> IsProjectManagedByAsync(string managerUserId, int projectId)
        {
            return _projects.AnyAsync(p => p.Id == projectId && p.ManagerUserId == managerUserId);
        }

        public Task<bool> ProjectExistsAsync(int projectId)
        {
            return _projects.AnyAsync(p => p.Id == projectId);
        }

        private IQueryable<TimeEntry> BuildManagedProjectsQuery(
            string managerUserId,
            int? projectId,
            string employeeUserId,
            DateTime? from,
            DateTime? to,
            bool? isBillable)
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

            if (isBillable.HasValue)
            {
                query = query.Where(te => te.IsBillable == isBillable.Value);
            }

            return query;
        }

        private IQueryable<TimeEntry> BuildUserQuery(
            string userId,
            int? projectId,
            DateTime? from,
            DateTime? to,
            bool? isBillable)
        {
            var query = _timeEntries.Where(te => te.UserId == userId && te.DeletedAtUtc == null);

            if (projectId.HasValue)
            {
                query = query.Where(te => te.ProjectId == projectId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(te => te.EntryDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(te => te.EntryDate <= to.Value.Date);
            }

            if (isBillable.HasValue)
            {
                query = query.Where(te => te.IsBillable == isBillable.Value);
            }

            return query;
        }

        private static IOrderedQueryable<TimeEntry> ApplySorting(IQueryable<TimeEntry> query, string sortBy, string sortDir)
        {
            var isDesc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

            return (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "durationminutes" => isDesc
                    ? query.OrderByDescending(te => te.DurationMinutes).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.DurationMinutes).ThenBy(te => te.Id),
                "projectid" => isDesc
                    ? query.OrderByDescending(te => te.ProjectId).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.ProjectId).ThenBy(te => te.Id),
                "isbillable" => isDesc
                    ? query.OrderByDescending(te => te.IsBillable).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.IsBillable).ThenBy(te => te.Id),
                "starttimeutc" => isDesc
                    ? query.OrderByDescending(te => te.StartTimeUtc).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.StartTimeUtc).ThenBy(te => te.Id),
                "endtimeutc" => isDesc
                    ? query.OrderByDescending(te => te.EndTimeUtc).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.EndTimeUtc).ThenBy(te => te.Id),
                _ => isDesc
                    ? query.OrderByDescending(te => te.EntryDate).ThenByDescending(te => te.Id)
                    : query.OrderBy(te => te.EntryDate).ThenBy(te => te.Id)
            };
        }
    }
}
