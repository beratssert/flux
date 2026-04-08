using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Expenses.Queries.GetAllExpenses
{
    public class GetAllExpensesQuery : IRequest<PagedResponse<GetAllExpensesViewModel>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string UserId { get; set; }
        public int? ProjectId { get; set; }
        public int? CategoryId { get; set; }
        public string Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string SortBy { get; set; }
        public string SortDir { get; set; }
    }

    public class GetAllExpensesQueryHandler : IRequestHandler<GetAllExpensesQuery, PagedResponse<GetAllExpensesViewModel>>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetAllExpensesQueryHandler(
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<PagedResponse<GetAllExpensesViewModel>> Handle(GetAllExpensesQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var currentUserId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var filter = _mapper.Map<GetAllExpensesParameter>(request);
            IReadOnlyList<Entities.Expense> expenses;
            int totalCount;

            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                expenses = await _expenseRepository.GetPagedAllAsync(
                    filter.PageNumber,
                    filter.PageSize,
                    filter.UserId,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To,
                    filter.SortBy,
                    filter.SortDir);
                totalCount = await _expenseRepository.CountAllAsync(
                    filter.UserId,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To);
            }
            else if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                expenses = await _expenseRepository.GetPagedByManagerVisibilityAsync(
                    currentUserId,
                    filter.PageNumber,
                    filter.PageSize,
                    filter.UserId,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To,
                    filter.SortBy,
                    filter.SortDir);
                totalCount = await _expenseRepository.CountByManagerVisibilityAsync(
                    currentUserId,
                    filter.UserId,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To);
            }
            else
            {
                expenses = await _expenseRepository.GetPagedByUserIdAsync(
                    currentUserId,
                    filter.PageNumber,
                    filter.PageSize,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To,
                    filter.SortBy,
                    filter.SortDir);
                totalCount = await _expenseRepository.CountByUserIdAsync(
                    currentUserId,
                    filter.ProjectId,
                    filter.CategoryId,
                    filter.Status,
                    filter.From,
                    filter.To);
            }

            var vm = _mapper.Map<List<GetAllExpensesViewModel>>(expenses);
            return new PagedResponse<GetAllExpensesViewModel>(vm, filter.PageNumber, filter.PageSize, totalCount);
        }
    }
}
