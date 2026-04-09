using CleanArchitecture.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IExpenseRepositoryAsync : IGenericRepositoryAsync<Expense>
    {
        Task<Expense> GetActiveByIdAsync(int id);
        Task<Expense> GetByIdAndUserIdAsync(int id, string userId);
        Task<Expense> GetByIdInManagerScopeAsync(int id, string managerUserId);
        Task<IReadOnlyList<Expense>> GetPagedByUserIdAsync(
            string userId,
            int pageNumber,
            int pageSize,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null);
        Task<int> CountByUserIdAsync(
            string userId,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<IReadOnlyList<Expense>> GetPagedByManagedProjectsAsync(
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
            string sortDir = null);
        Task<int> CountByManagedProjectsAsync(
            string managerUserId,
            string employeeUserId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<IReadOnlyList<Expense>> GetPagedByManagerVisibilityAsync(
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
            string sortDir = null);
        Task<int> CountByManagerVisibilityAsync(
            string managerUserId,
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null);
        Task<IReadOnlyList<Expense>> GetPagedAllAsync(
            int pageNumber,
            int pageSize,
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null,
            string sortBy = null,
            string sortDir = null);
        Task<int> CountAllAsync(
            string userId = null,
            int? projectId = null,
            int? categoryId = null,
            string status = null,
            DateTime? from = null,
            DateTime? to = null);
    }
}
