using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Reports.Queries.GetProjectSummary
{
    public class GetProjectSummaryQuery : IRequest<ProjectSummaryResponse>
    {
        public int ProjectId { get; set; }
    }

    public class GetProjectSummaryQueryHandler : IRequestHandler<GetProjectSummaryQuery, ProjectSummaryResponse>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetProjectSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IExpenseRepositoryAsync expenseRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _timeEntryRepository = timeEntryRepository;
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<ProjectSummaryResponse> Handle(GetProjectSummaryQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var exists = await _timeEntryRepository.ProjectExistsAsync(request.ProjectId);
            if (!exists)
            {
                throw new ApiException("Project not found.");
            }

            int totalMinutes;
            int totalEntries;
            int billableEntries;
            decimal totalExpense;

            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var timeAggregate = await _timeEntryRepository.GetProjectAggregateAllAsync(request.ProjectId);
                totalMinutes = timeAggregate.TotalMinutes;
                totalEntries = timeAggregate.TotalEntries;
                billableEntries = timeAggregate.BillableEntries;
                totalExpense = await _expenseRepository.GetProjectTotalAmountAllAsync(request.ProjectId);
            }
            else if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var canAccess = await _timeEntryRepository.IsProjectManagedByAsync(_authenticatedUserService.UserId, request.ProjectId);
                if (!canAccess)
                {
                    throw new ApiException("Project not found.");
                }

                var timeAggregate = await _timeEntryRepository.GetProjectAggregateByManagedProjectsAsync(_authenticatedUserService.UserId, request.ProjectId);
                totalMinutes = timeAggregate.TotalMinutes;
                totalEntries = timeAggregate.TotalEntries;
                billableEntries = timeAggregate.BillableEntries;
                totalExpense = await _expenseRepository.GetProjectTotalAmountByManagedProjectsAsync(_authenticatedUserService.UserId, request.ProjectId);
            }
            else
            {
                throw new ApiException("Only manager or admin can access project summary.");
            }

            var billableRate = totalEntries == 0
                ? 0m
                : Math.Round((decimal)billableEntries * 100m / totalEntries, 2);

            return new ProjectSummaryResponse
            {
                ProjectId = request.ProjectId,
                TotalMinutes = totalMinutes,
                TotalExpenseAmount = totalExpense,
                BillableEntryRate = billableRate
            };
        }
    }
}
