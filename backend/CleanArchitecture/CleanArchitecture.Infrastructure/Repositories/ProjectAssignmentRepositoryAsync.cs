using CleanArchitecture.Core.DTOs.Projects;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class ProjectAssignmentRepositoryAsync : IProjectAssignmentRepositoryAsync
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbSet<ProjectAssignment> _projectAssignments;

        public ProjectAssignmentRepositoryAsync(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _projectAssignments = dbContext.Set<ProjectAssignment>();
        }

        public Task<bool> IsUserAssignedToProjectAsync(string userId, int projectId)
        {
            return _projectAssignments.AnyAsync(pa =>
                pa.UserId == userId &&
                pa.ProjectId == projectId &&
                pa.IsActive);
        }

        public Task<bool> HasActiveAssignmentAsync(int projectId, string userId)
        {
            return _projectAssignments.AnyAsync(pa =>
                pa.ProjectId == projectId &&
                pa.UserId == userId &&
                pa.IsActive);
        }

        public async Task<ProjectAssignment> AddAsync(ProjectAssignment assignment)
        {
            await _projectAssignments.AddAsync(assignment);
            await _dbContext.SaveChangesAsync();
            return assignment;
        }

        public Task<ProjectAssignment> GetActiveByProjectAndUserAsync(int projectId, string userId)
        {
            return _projectAssignments.FirstOrDefaultAsync(pa =>
                pa.ProjectId == projectId &&
                pa.UserId == userId &&
                pa.IsActive);
        }

        public async Task UpdateAsync(ProjectAssignment assignment)
        {
            _dbContext.Entry(assignment).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ProjectAssignment>> GetActiveByProjectIdAsync(int projectId)
        {
            return await _projectAssignments.AsNoTracking()
                .Where(pa => pa.ProjectId == projectId && pa.IsActive)
                .OrderBy(pa => pa.UserId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<MyProjectAssignmentRowDto>> GetActiveRowsForUserAsync(string userId)
        {
            return await _projectAssignments.AsNoTracking()
                .Where(pa => pa.UserId == userId && pa.IsActive)
                .Join(
                    _dbContext.Set<Project>().AsNoTracking(),
                    pa => pa.ProjectId,
                    p => p.Id,
                    (pa, p) => new MyProjectAssignmentRowDto
                    {
                        ProjectId = p.Id,
                        ProjectName = p.Name,
                        ProjectCode = p.Code,
                        ProjectStatus = p.Status,
                        AssignedAtUtc = pa.AssignedAtUtc
                    })
                .OrderBy(r => r.ProjectName)
                .ToListAsync();
        }
    }
}
