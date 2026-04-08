using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Commands.DeleteExpenseById
{
    public class DeleteExpenseByIdCommand : IRequest<int>
    {
        public int Id { get; set; }
    }

    public class DeleteExpenseByIdCommandHandler : IRequestHandler<DeleteExpenseByIdCommand, int>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public DeleteExpenseByIdCommandHandler(
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<int> Handle(DeleteExpenseByIdCommand request, CancellationToken cancellationToken)
        {
            var expense = await _expenseRepository.GetByIdAndUserIdAsync(request.Id, _authenticatedUserService.UserId);
            if (expense == null)
            {
                throw new ApiException("Expense not found.");
            }

            if (expense.Status != ExpenseStatuses.Draft)
            {
                throw new ApiException("Only Draft expenses can be deleted.");
            }

            expense.DeletedAtUtc = DateTime.UtcNow;
            await _expenseRepository.UpdateAsync(expense);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Expense",
                    expense.Id.ToString(),
                    "Delete",
                    "Expense soft-deleted.",
                    JsonSerializer.Serialize(new { expense.Status }),
                    JsonSerializer.Serialize(new { expense.DeletedAtUtc }));
            }

            return expense.Id;
        }
    }
}
