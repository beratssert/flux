using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class ExpenseCategoryRepositoryAsync : GenericRepositoryAsync<ExpenseCategory>, IExpenseCategoryRepositoryAsync
    {
        private readonly DbSet<ExpenseCategory> _expenseCategories;

        public ExpenseCategoryRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _expenseCategories = dbContext.Set<ExpenseCategory>();
        }

        public async Task<IReadOnlyList<ExpenseCategory>> GetActiveAsync()
        {
            return await _expenseCategories
                .Where(ec => ec.IsActive)
                .OrderBy(ec => ec.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<ExpenseCategory> GetByNameAsync(string name)
        {
            return _expenseCategories.FirstOrDefaultAsync(ec => ec.Name == name);
        }
    }
}
