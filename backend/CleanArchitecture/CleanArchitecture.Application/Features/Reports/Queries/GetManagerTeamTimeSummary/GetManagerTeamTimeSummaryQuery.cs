using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Models;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamTimeSummary
{
    public class GetManagerTeamTimeSummaryQuery : IRequest<TimeSummaryResponse>
    {
        public int? ProjectId { get; set; }
        public string UserId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string GroupBy { get; set; }
    }

    public class GetManagerTeamTimeSummaryQueryHandler : IRequestHandler<GetManagerTeamTimeSummaryQuery, TimeSummaryResponse>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public GetManagerTeamTimeSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<TimeSummaryResponse> Handle(GetManagerTeamTimeSummaryQuery request, CancellationToken cancellationToken)
        {
            var groupBy = NormalizeGroupBy(request.GroupBy, new[] { "user", "project", "week" });
            var role = _authenticatedUserService.Role;

            IReadOnlyList<TimeSummaryRowDto> rows;
            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rows = await _timeEntryRepository.GetSummaryRowsAllAsync(
                    request.ProjectId,
                    request.UserId,
                    request.From,
                    request.To);
            }
            else if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rows = await _timeEntryRepository.GetSummaryRowsByManagedProjectsAsync(
                    _authenticatedUserService.UserId,
                    request.ProjectId,
                    request.UserId,
                    request.From,
                    request.To);
            }
            else
            {
                throw new ApiException("Only manager or admin can access team time summary.");
            }

            var response = BuildSummary(rows, groupBy);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "ManagerTeamTimeSummary",
                    _authenticatedUserService.UserId,
                    "Read",
                    "Manager/admin accessed team time summary report.",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        request.ProjectId,
                        request.UserId,
                        request.From,
                        request.To,
                        GroupBy = groupBy,
                        RowCount = rows.Count,
                        GroupCount = response.Groups.Count,
                        response.TotalMinutes
                    }));
            }

            return response;
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
                "project" => row.ProjectId.ToString(),
                "week" => $"{ISOWeek.GetYear(row.EntryDate)}-W{ISOWeek.GetWeekOfYear(row.EntryDate):D2}",
                _ => row.UserId
            };
        }
    }
}
