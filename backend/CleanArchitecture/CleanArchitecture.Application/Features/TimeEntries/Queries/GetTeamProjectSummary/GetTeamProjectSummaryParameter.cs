using System;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary
{
    public class GetTeamProjectSummaryParameter
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string EmployeeUserId { get; set; }
    }
}
