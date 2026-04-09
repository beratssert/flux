using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Queries.GetExpenseById
{
    public class GetExpenseByIdQuery : IRequest<Expense>
    {
        public int Id { get; set; }
    }

    public class GetExpenseByIdQueryHandler : IRequestHandler<GetExpenseByIdQuery, Expense>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetExpenseByIdQueryHandler(
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<Expense> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var currentUserId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            Expense expense;
            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                expense = await _expenseRepository.GetActiveByIdAsync(request.Id);
            }
            else
            {
                expense = await _expenseRepository.GetByIdAndUserIdAsync(request.Id, currentUserId);
                if (expense == null && string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    expense = await _expenseRepository.GetByIdInManagerScopeAsync(request.Id, currentUserId);
                }
            }

            if (expense == null)
            {
                throw new ApiException("Expense not found.");
            }

            return expense;
        }
    }
}
