using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Commands.RejectExpense
{
    public class RejectExpenseCommand : IRequest<int>
    {
        public int Id { get; set; }
        public string Reason { get; set; }
    }

    public class RejectExpenseCommandHandler : IRequestHandler<RejectExpenseCommand, int>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public RejectExpenseCommandHandler(
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<int> Handle(RejectExpenseCommand request, CancellationToken cancellationToken)
        {
            if (!string.Equals(_authenticatedUserService.Role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiException("Only manager can reject expense.");
            }

            var expense = await _expenseRepository.GetByIdInManagerScopeAsync(request.Id, _authenticatedUserService.UserId);
            if (expense == null)
            {
                throw new ApiException("Expense not found.");
            }

            if (expense.Status != ExpenseStatuses.Submitted)
            {
                throw new ApiException("Only Submitted expenses can be rejected.");
            }

            expense.Status = ExpenseStatuses.Rejected;
            expense.RejectionReason = request.Reason?.Trim();
            expense.ReviewedByUserId = _authenticatedUserService.UserId;
            expense.ReviewedAtUtc = DateTime.UtcNow;
            await _expenseRepository.UpdateAsync(expense);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Expense",
                    expense.Id.ToString(),
                    "Reject",
                    "Expense rejected by manager.",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        expense.Status,
                        expense.RejectionReason,
                        expense.ReviewedByUserId,
                        expense.ReviewedAtUtc
                    }));
            }

            return expense.Id;
        }
    }
}
