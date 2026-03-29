using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class ProjectAssignmentRepositoryAsync : IProjectAssignmentRepositoryAsync
    {
        private readonly DbSet<ProjectAssignment> _projectAssignments;

        public ProjectAssignmentRepositoryAsync(ApplicationDbContext dbContext)
        {
            _projectAssignments = dbContext.Set<ProjectAssignment>();
        }

        public Task<bool> IsUserAssignedToProjectAsync(string userId, int projectId)
        {
            return _projectAssignments.AnyAsync(pa => pa.UserId == userId && pa.ProjectId == projectId && pa.IsActive);
        }
    }
}
