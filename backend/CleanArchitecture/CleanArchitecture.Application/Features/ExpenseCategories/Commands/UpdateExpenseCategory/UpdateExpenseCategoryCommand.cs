using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.ExpenseCategories.Commands.UpdateExpenseCategory
{
    public class UpdateExpenseCategoryCommand : IRequest<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateExpenseCategoryCommandHandler : IRequestHandler<UpdateExpenseCategoryCommand, int>
    {
        private readonly IExpenseCategoryRepositoryAsync _expenseCategoryRepository;

        public UpdateExpenseCategoryCommandHandler(IExpenseCategoryRepositoryAsync expenseCategoryRepository)
        {
            _expenseCategoryRepository = expenseCategoryRepository;
        }

        public async Task<int> Handle(UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _expenseCategoryRepository.GetByIdAsync(request.Id);
            if (category == null)
            {
                throw new ApiException("Expense category not found.");
            }

            var normalizedName = request.Name?.Trim();
            var existing = await _expenseCategoryRepository.GetByNameAsync(normalizedName);
            if (existing != null && existing.Id != request.Id)
            {
                throw new ApiException("Expense category with the same name already exists.");
            }

            category.Name = normalizedName;
            category.IsActive = request.IsActive;
            await _expenseCategoryRepository.UpdateAsync(category);
            return category.Id;
        }
    }
}
