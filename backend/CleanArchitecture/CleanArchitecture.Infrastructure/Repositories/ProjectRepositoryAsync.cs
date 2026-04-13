using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class ProjectRepositoryAsync : IProjectRepositoryAsync
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DbSet<Project> _projects;
        private readonly DbSet<ProjectAssignment> _assignments;

        public ProjectRepositoryAsync(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _projects = dbContext.Set<Project>();
            _assignments = dbContext.Set<ProjectAssignment>();
        }

        public async Task<Project> AddAsync(Project project)
        {
            await _projects.AddAsync(project);
            await _dbContext.SaveChangesAsync();
            return project;
        }

        public Task<Project> GetByIdAsync(int id, bool tracked = false)
        {
            var q = _projects.AsQueryable();
            if (!tracked)
            {
                q = q.AsNoTracking();
            }

            return q.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task UpdateAsync(Project project)
        {
            _dbContext.Entry(project).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> CodeExistsAsync(string code, int? excludeProjectId)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.FromResult(false);
            }

            var normalized = code.Trim();
            var q = _projects.AsNoTracking().Where(p => p.Code == normalized);
            if (excludeProjectId.HasValue)
            {
                q = q.Where(p => p.Id != excludeProjectId.Value);
            }

            return q.AnyAsync();
        }

        public Task<bool> IsManagedByAsync(string managerUserId, int projectId)
        {
            return _projects.AsNoTracking().AnyAsync(p => p.Id == projectId && p.ManagerUserId == managerUserId);
        }

        public Task<bool> CanEmployeeViewAsync(string userId, int projectId)
        {
            return _assignments.AsNoTracking().AnyAsync(pa =>
                pa.ProjectId == projectId &&
                pa.UserId == userId &&
                pa.IsActive);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForAdminAsync(
            int pageNumber,
            int pageSize,
            string status,
            string managerUserId,
            string q)
        {
            var query = ApplyFilters(_projects.AsNoTracking(), status, managerUserId, q);
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForManagerAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            string status,
            string managerUserIdFilter,
            string q)
        {
            var query = _projects.AsNoTracking().Where(p => p.ManagerUserId == managerUserId);
            query = ApplyFilters(query, status, managerUserIdFilter, q);
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedForEmployeeAsync(
            string userId,
            int pageNumber,
            int pageSize,
            string status,
            string managerUserIdFilter,
            string q)
        {
            var assignedIds = _assignments.AsNoTracking()
                .Where(pa => pa.UserId == userId && pa.IsActive)
                .Select(pa => pa.ProjectId);

            var query = _projects.AsNoTracking().Where(p => assignedIds.Contains(p.Id));
            query = ApplyFilters(query, status, managerUserIdFilter, q);
            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, total);
        }

        private static IQueryable<Project> ApplyFilters(
            IQueryable<Project> query,
            string status,
            string managerUserId,
            string search)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var norm = ProjectStatuses.Normalize(status.Trim());
                query = query.Where(p => p.Status == norm);
            }

            if (!string.IsNullOrWhiteSpace(managerUserId))
            {
                query = query.Where(p => p.ManagerUserId == managerUserId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(term) ||
                    (p.Code != null && p.Code.Contains(term)));
            }

            return query;
        }
    }
}
