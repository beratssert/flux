using CleanArchitecture.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IProjectRepositoryAsync
    {
        Task<Project> AddAsync(Project project);

        Task<Project> GetByIdAsync(int id, bool tracked = false);

        Task UpdateAsync(Project project);

        Task<bool> CodeExistsAsync(string code, int? excludeProjectId);

        Task<bool> IsManagedByAsync(string managerUserId, int projectId);

        Task<bool> CanEmployeeViewAsync(string userId, int projectId);

        Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForAdminAsync(
            int pageNumber,
            int pageSize,
            string status,
            string managerUserId,
            string q);

        Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForManagerAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            string status,
            string managerUserIdFilter,
            string q);

        Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForEmployeeAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string status,
            string managerUserIdFilter,
            string q);
    }
}
