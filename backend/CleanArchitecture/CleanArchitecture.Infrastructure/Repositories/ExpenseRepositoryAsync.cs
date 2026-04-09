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
    public class ExpenseRepositoryAsync : GenericRepositoryAsync<Expense>, IExpenseRepositoryAsync
    {
        private readonly DbSet<Expense> _expenses;
        private readonly DbSet<Project> _projects;

        public ExpenseRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _expenses = dbContext.Set<Expense>();
            _projects = dbContext.Set<Project>();
        }

        public Task<Expense> GetActiveByIdAsync(int id)
        {
            return _expenses.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAtUtc == null);
        }

        public Task<Expense> GetByIdAndUserIdAsync(int id, string userId)
        {
            return _expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && e.DeletedAtUtc == null);
        }

        public Task<Expense> GetByIdInManagerScopeAsync(int id, string managerUserId)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            return _expenses.FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.DeletedAtUtc == null &&
                managedProjectIds.Contains(e.ProjectId) &&
                e.UserId != managerUserId);
        }

        public async Task<IReadOnlyList<Expense>> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = BuildUserQuery(userId, projectId, categoryId, status, from, to);
            return await ApplySorting(query, sortBy, sortDir)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountByUserIdAsync(
            string userId,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            return BuildUserQuery(userId, projectId, categoryId, status, from, to).CountAsync();
        }

        public async Task<IReadOnlyList<Expense>> GetPagedByManagedProjectsAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            string employeeUserId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = BuildManagedProjectsQuery(managerUserId, employeeUserId, projectId, categoryId, status, from, to);
            return await ApplySorting(query, sortBy, sortDir)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountByManagedProjectsAsync(
            string managerUserId,
            string employeeUserId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            return BuildManagedProjectsQuery(managerUserId, employeeUserId, projectId, categoryId, status, from, to).CountAsync();
        }

        public async Task<IReadOnlyList<Expense>> GetPagedByManagerVisibilityAsync(
            string managerUserId,
            int pageNumber,
            int pageSize,
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = BuildManagerVisibilityQuery(managerUserId, userId, projectId, categoryId, status, from, to);
            return await ApplySorting(query, sortBy, sortDir)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountByManagerVisibilityAsync(
            string managerUserId,
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            return BuildManagerVisibilityQuery(managerUserId, userId, projectId, categoryId, status, from, to).CountAsync();
        }

        public async Task<IReadOnlyList<Expense>> GetPagedAllAsync(
            int pageNumber,
            int pageSize,
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null)
        {
            var query = _expenses.Where(e => e.DeletedAtUtc == null);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            query = ApplyFilters(query, projectId, categoryId, status, from, to);
            return await ApplySorting(query, sortBy, sortDir)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<int> CountAllAsync(
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _expenses.Where(e => e.DeletedAtUtc == null);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            return ApplyFilters(query, projectId, categoryId, status, from, to).CountAsync();
        }

        private IQueryable<Expense> BuildUserQuery(
            string userId,
            int? projectId,
            int? categoryId,
            string status,
            DateTime? from,
            DateTime? to)
        {
            var query = _expenses.Where(e => e.UserId == userId && e.DeletedAtUtc == null);

            return ApplyFilters(query, projectId, categoryId, status, from, to);
        }

        private IQueryable<Expense> BuildManagedProjectsQuery(
            string managerUserId,
            string employeeUserId,
            int? projectId,
            int? categoryId,
            string status,
            DateTime? from,
            DateTime? to)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _expenses.Where(e =>
                e.DeletedAtUtc == null &&
                managedProjectIds.Contains(e.ProjectId));

            if (!string.IsNullOrWhiteSpace(employeeUserId))
            {
                query = query.Where(e => e.UserId == employeeUserId);
            }

            return ApplyFilters(query, projectId, categoryId, status, from, to);
        }

        private IQueryable<Expense> BuildManagerVisibilityQuery(
            string managerUserId,
            string userId,
            int? projectId,
            int? categoryId,
            string status,
            DateTime? from,
            DateTime? to)
        {
            var managedProjectIds = _projects
                .Where(p => p.ManagerUserId == managerUserId)
                .Select(p => p.Id);

            var query = _expenses.Where(e =>
                e.DeletedAtUtc == null &&
                (e.UserId == managerUserId || managedProjectIds.Contains(e.ProjectId)));

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            return ApplyFilters(query, projectId, categoryId, status, from, to);
        }

        private static IQueryable<Expense> ApplyFilters(
            IQueryable<Expense> query,
            int? projectId,
            int? categoryId,
            string status,
            DateTime? from,
            DateTime? to)
        {
            if (projectId.HasValue)
            {
                query = query.Where(e => e.ProjectId == projectId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            if (from.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.ExpenseDate <= to.Value.Date);
            }

            return query;
        }

        private static IOrderedQueryable<Expense> ApplySorting(IQueryable<Expense> query, string sortBy, string sortDir)
        {
            var isDesc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            return (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "amount" => isDesc ? query.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Id) : query.OrderBy(e => e.Amount).ThenBy(e => e.Id),
                "projectid" => isDesc ? query.OrderByDescending(e => e.ProjectId).ThenByDescending(e => e.Id) : query.OrderBy(e => e.ProjectId).ThenBy(e => e.Id),
                "categoryid" => isDesc ? query.OrderByDescending(e => e.CategoryId).ThenByDescending(e => e.Id) : query.OrderBy(e => e.CategoryId).ThenBy(e => e.Id),
                "status" => isDesc ? query.OrderByDescending(e => e.Status).ThenByDescending(e => e.Id) : query.OrderBy(e => e.Status).ThenBy(e => e.Id),
                _ => isDesc ? query.OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id) : query.OrderBy(e => e.ExpenseDate).ThenBy(e => e.Id)
            };
        }
    }
}
