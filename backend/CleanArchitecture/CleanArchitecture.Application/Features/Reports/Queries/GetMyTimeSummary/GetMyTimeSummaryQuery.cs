using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Reports.Queries.GetMyTimeSummary
{
    public class GetMyTimeSummaryQuery : IRequest<TimeSummaryResponse>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string GroupBy { get; set; }
    }

    public class GetMyTimeSummaryQueryHandler : IRequestHandler<GetMyTimeSummaryQuery, TimeSummaryResponse>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetMyTimeSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<TimeSummaryResponse> Handle(GetMyTimeSummaryQuery request, CancellationToken cancellationToken)
        {
            var groupBy = NormalizeGroupBy(request.GroupBy, new[] { "day", "week", "month", "project" });
            var rows = await _timeEntryRepository.GetSummaryRowsByUserAsync(
                _authenticatedUserService.UserId,
                request.From,
                request.To);

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

        internal static TimeSummaryResponse BuildSummary(IReadOnlyList<TimeSummaryRowDto> rows, string groupBy)
        {
            var groups = rows
                .GroupBy(r => BuildKey(r, groupBy))
                .Select(g => new TimeSummaryGroupItem
                {
                    Key = g.Key,
                    Minutes = g.Sum(x => x.DurationMinutes)
                })
                .OrderBy(g => g.Key)
                .ToList();

            return new TimeSummaryResponse
            {
                TotalMinutes = rows.Sum(r => r.DurationMinutes),
                Groups = groups
            };
        }

        private static string BuildKey(TimeSummaryRowDto row, string groupBy)
        {
            return groupBy switch
            {
                "week" => $"{ISOWeek.GetYear(row.EntryDate)}-W{ISOWeek.GetWeekOfYear(row.EntryDate):D2}",
                "month" => row.EntryDate.ToString("yyyy-MM"),
                "project" => row.ProjectId.ToString(),
                _ => row.EntryDate.ToString("yyyy-MM-dd")
            };
        }
    }
}
