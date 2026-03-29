namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary
{
    public class GetTeamProjectSummaryViewModel
    {
        public int ProjectId { get; set; }
        public int TotalDurationMinutes { get; set; }
        public int EntryCount { get; set; }
        public int EmployeeCount { get; set; }
    }
}
