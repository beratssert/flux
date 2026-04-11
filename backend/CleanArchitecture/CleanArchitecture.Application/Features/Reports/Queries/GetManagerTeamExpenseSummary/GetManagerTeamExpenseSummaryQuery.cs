using CleanArchitecture.Core.DTOs.Expenses;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamExpenseSummary
{
    public class GetManagerTeamExpenseSummaryQuery : IRequest<ExpenseSummaryResponse>
    {
        public int? ProjectId { get; set; }
        public string UserId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string GroupBy { get; set; }
        public string CurrencyCode { get; set; }
    }

    public class GetManagerTeamExpenseSummaryQueryHandler : IRequestHandler<GetManagerTeamExpenseSummaryQuery, ExpenseSummaryResponse>
    {
        private readonly IExpenseRepositoryAsync _expenseRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetManagerTeamExpenseSummaryQueryHandler(IExpenseRepositoryAsync expenseRepository, IAuthenticatedUserService authenticatedUserService)
        {
            _expenseRepository = expenseRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<ExpenseSummaryResponse> Handle(GetManagerTeamExpenseSummaryQuery request, CancellationToken cancellationToken)
        {
            var groupBy = NormalizeGroupBy(request.GroupBy, new[] { "user", "project", "month" });
            IReadOnlyList<ExpenseSummaryRowDto> rows;

            if (string.Equals(_authenticatedUserService.Role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rows = await _expenseRepository.GetSummaryRowsAllAsync(
                    request.ProjectId, request.UserId, request.CategoryId, request.From, request.To, request.CurrencyCode);
            }
            else if (string.Equals(_authenticatedUserService.Role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rows = await _expenseRepository.GetSummaryRowsByManagedProjectsAsync(
                    _authenticatedUserService.UserId, request.ProjectId, request.UserId, request.CategoryId, request.From, request.To, request.CurrencyCode);
            }
            else
            {
                throw new ApiException("Only manager or admin can access team expense summary.");
            }

            return BuildSummary(rows, groupBy);
        }

        private static string NormalizeGroupBy(string rawGroupBy, string[] allowed)
        {
            var groupBy = string.IsNullOrWhiteSpace(rawGroupBy) ? allowed[0] : rawGroupBy.Trim().ToLowerInvariant();
            if (!allowed.Contains(groupBy))
            {
                throw new ApiException($"Unsupported groupBy value: {rawGroupBy}");
            }

            return groupBy;
        }

        internal static ExpenseSummaryResponse BuildSummary(IReadOnlyList<ExpenseSummaryRowDto> rows, string groupBy)
        {
            var groups = rows
                .GroupBy(r => BuildKey(r, groupBy))
                .Select(g => new ExpenseSummaryGroupItem { Key = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderBy(g => g.Key)
                .ToList();

            return new ExpenseSummaryResponse
            {
                TotalAmount = rows.Sum(r => r.Amount),
                Groups = groups
            };
        }

        private static string BuildKey(ExpenseSummaryRowDto row, string groupBy)
        {
            return groupBy switch
            {
                "project" => row.ProjectId.ToString(),
                "month" => row.ExpenseDate.ToString("yyyy-MM"),
                _ => row.UserId
            };
        }
    }
}
