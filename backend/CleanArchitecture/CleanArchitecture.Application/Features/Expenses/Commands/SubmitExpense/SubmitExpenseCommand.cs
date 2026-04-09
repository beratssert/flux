using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Commands.SubmitExpense
{
    public class SubmitExpenseCommand : IRequest<int>
    {
        public int Id { get; set; }
    }

    public class SubmitExpenseCommandHandler : IRequestHandler<SubmitExpenseCommand, int>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public SubmitExpenseCommandHandler(
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<int> Handle(SubmitExpenseCommand request, CancellationToken cancellationToken)
        {
            var expense = await _expenseRepository.GetByIdAndUserIdAsync(request.Id, _authenticatedUserService.UserId);
            if (expense == null)
            {
                throw new ApiException("Expense not found.");
            }

            if (expense.Status != ExpenseStatuses.Draft && expense.Status != ExpenseStatuses.Rejected)
            {
                throw new ApiException("Only Draft or Rejected expenses can be submitted.");
            }

            expense.Status = ExpenseStatuses.Submitted;
            expense.RejectionReason = null;
            expense.ReviewedAtUtc = null;
            expense.ReviewedByUserId = null;
            await _expenseRepository.UpdateAsync(expense);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Expense",
                    expense.Id.ToString(),
                    "Submit",
                    "Expense submitted.",
                    null,
                    JsonSerializer.Serialize(new { expense.Status }));
            }

            return expense.Id;
        }
    }
}
