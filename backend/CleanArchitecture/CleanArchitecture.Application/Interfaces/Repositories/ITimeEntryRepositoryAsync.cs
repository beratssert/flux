using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.DTOs.TimeEntries;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface ITimeEntryRepositoryAsync : IGenericRepositoryAsync<TimeEntry>
    {
        Task<TimeEntry> GetByIdAndUserIdAsync(int id, string userId);
        Task<IReadOnlyList<TimeEntry>> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null,
            string sortBy = null,
            string sortDir = null);
        Task<int> CountByUserIdAsync(
            string userId,
            int? projectId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null);
        Task<IReadOnlyList<TimeEntry>> GetPagedByManagedProjectsAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null,
            string sortBy = null,
            string sortDir = null);
        Task<int> CountByManagedProjectsAsync(
            string managerUserId,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null,
            bool? isBillable = null);
        Task<IReadOnlyList<TeamProjectSummaryDto>> GetProjectSummaryByManagedProjectsAsync(
            string managerUserId,
            DateTime? from = null,
            DateTime? to = null,
            string employeeUserId = null);
        Task<IReadOnlyList<TeamPeriodSummaryDto>> GetPeriodSummaryByManagedProjectsAsync(
            string managerUserId,
            DateTime? from = null,
            DateTime? to = null,
            int? projectId = null,
            string employeeUserId = null);
        Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsByUserAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null);
        Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsByManagedProjectsAsync(
            string managerUserId,
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<IReadOnlyList<TimeSummaryRowDto>> GetSummaryRowsAllAsync(
            int? projectId = null,
            string employeeUserId = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<(int TotalMinutes, int TotalEntries, int BillableEntries)> GetProjectAggregateByManagedProjectsAsync(
            string managerUserId,
            int projectId,
            DateTime? from = null,
            DateTime? to = null);
        Task<(int TotalMinutes, int TotalEntries, int BillableEntries)> GetProjectAggregateAllAsync(
            int projectId,
            DateTime? from = null,
            DateTime? to = null);
        Task<bool> IsProjectManagedByAsync(string managerUserId, int projectId);
        Task<bool> ProjectExistsAsync(int projectId);
        Task<bool> HasOverlappingEntryAsync(string userId, DateTime startUtc, DateTime endUtc, int? excludeId = null);
    }
}
