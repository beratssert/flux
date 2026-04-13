using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.DTOs.Projects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IProjectAssignmentRepositoryAsync
    {
        Task<bool> IsUserAssignedToProjectAsync(string userId, int projectId);

        Task<bool> HasActiveAssignmentAsync(int projectId, string userId);

        Task<ProjectAssignment> AddAsync(ProjectAssignment assignment);

        Task<ProjectAssignment> GetActiveByProjectAndUserAsync(int projectId, string userId);

        Task UpdateAsync(ProjectAssignment assignment);

        Task<IReadOnlyList<ProjectAssignment>> GetActiveByProjectIdAsync(int projectId);

        Task<IReadOnlyList<MyProjectAssignmentRowDto>> GetActiveRowsForUserAsync(string userId);
    }
}

