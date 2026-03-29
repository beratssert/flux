using System;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamPeriodSummary
{
    public class GetTeamPeriodSummaryParameter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? ProjectId { get; set; }
        public string EmployeeUserId { get; set; }
    }
}
