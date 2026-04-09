using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Commands.UpdateExpense
{
    public class UpdateExpenseCommand : IRequest<int>
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int CategoryId { get; set; }
        public string Notes { get; set; }
        public string ReceiptUrl { get; set; }
    }

    public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, int>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IProjectAssignmentRepositoryAsync _projectAssignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public UpdateExpenseCommandHandler(
            IExpenseRepositoryAsync expenseRepository,
            IProjectAssignmentRepositoryAsync projectAssignmentRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _expenseRepository = expenseRepository;
            _projectAssignmentRepository = projectAssignmentRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<int> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var expense = await _expenseRepository.GetByIdAndUserIdAsync(request.Id, userId);
            if (expense == null)
            {
                throw new ApiException("Expense not found.");
            }

            if (expense.Status != ExpenseStatuses.Draft && expense.Status != ExpenseStatuses.Rejected)
            {
                throw new ApiException("Only Draft or Rejected expenses can be updated.");
            }

            var isAssigned = await _projectAssignmentRepository.IsUserAssignedToProjectAsync(userId, request.ProjectId);
            if (!isAssigned)
            {
                throw new ApiException("User is not assigned to this project.");
            }

            expense.ProjectId = request.ProjectId;
            expense.ExpenseDate = request.ExpenseDate.Date;
            expense.Amount = request.Amount;
            expense.CurrencyCode = request.CurrencyCode?.Trim().ToUpperInvariant();
            expense.CategoryId = request.CategoryId;
            expense.Notes = request.Notes;
            expense.ReceiptUrl = request.ReceiptUrl;

            await _expenseRepository.UpdateAsync(expense);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Expense",
                    expense.Id.ToString(),
                    "Update",
                    "Expense updated.",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        expense.UserId,
                        expense.ProjectId,
                        expense.ExpenseDate,
                        expense.Amount,
                        expense.CurrencyCode,
                        expense.CategoryId,
                        expense.Status
                    }));
            }

            return expense.Id;
        }
    }
}
