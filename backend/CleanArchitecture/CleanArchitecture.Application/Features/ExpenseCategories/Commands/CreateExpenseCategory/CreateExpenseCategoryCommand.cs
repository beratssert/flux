using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.ExpenseCategories.Commands.CreateExpenseCategory
{
    public class CreateExpenseCategoryCommand : IRequest<int>
    {
        public string Name { get; set; }
    }

    public class CreateExpenseCategoryCommandHandler : IRequestHandler<CreateExpenseCategoryCommand, int>
    {
        private readonly IExpenseCategoryRepositoryAsync _expenseCategoryRepository;

        public CreateExpenseCategoryCommandHandler(IExpenseCategoryRepositoryAsync expenseCategoryRepository)
        {
            _expenseCategoryRepository = expenseCategoryRepository;
        }

        public async Task<int> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
        {
            var normalizedName = request.Name?.Trim();
            var existing = await _expenseCategoryRepository.GetByNameAsync(normalizedName);
            if (existing != null)
            {
                throw new ApiException("Expense category with the same name already exists.");
            }

            var entity = new ExpenseCategory
            {
                Name = normalizedName,
                IsActive = true
            };

            await _expenseCategoryRepository.AddAsync(entity);
            return entity.Id;
        }
    }
}
