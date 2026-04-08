using CleanArchitecture.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IExpenseCategoryRepositoryAsync : IGenericRepositoryAsync<ExpenseCategory>
    {
        Task<IReadOnlyList<ExpenseCategory>> GetActiveAsync();
        Task<ExpenseCategory> GetByNameAsync(string name);
    }
}
