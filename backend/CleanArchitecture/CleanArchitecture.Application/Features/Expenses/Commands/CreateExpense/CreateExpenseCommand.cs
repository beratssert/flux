using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Commands.CreateExpense
{
    public class CreateExpenseCommand : IRequest<int>
    {
        public int ProjectId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int CategoryId { get; set; }
        public string Notes { get; set; }
        public string ReceiptUrl { get; set; }
    }

    public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, int>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IProjectAssignmentRepositoryAsync _projectAssignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public CreateExpenseCommandHandler(
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

        public async Task<int> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var isAssigned = await _projectAssignmentRepository.IsUserAssignedToProjectAsync(userId, request.ProjectId);
            if (!isAssigned)
            {
                throw new ApiException("User is not assigned to this project.");
            }

            var expense = new Expense
            {
                UserId = userId,
                ProjectId = request.ProjectId,
                ExpenseDate = request.ExpenseDate.Date,
                Amount = request.Amount,
                CurrencyCode = request.CurrencyCode?.Trim().ToUpperInvariant(),
                CategoryId = request.CategoryId,
                Notes = request.Notes,
                ReceiptUrl = request.ReceiptUrl,
                Status = ExpenseStatuses.Draft
            };

            await _expenseRepository.AddAsync(expense);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Expense",
                    expense.Id.ToString(),
                    "Create",
                    "Expense draft created.",
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
