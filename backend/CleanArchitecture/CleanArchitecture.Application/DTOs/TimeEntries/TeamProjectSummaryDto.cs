namespace CleanArchitecture.Core.DTOs.TimeEntries
{
    public class TeamProjectSummaryDto
    {
        public int ProjectId { get; set; }
        public int TotalDurationMinutes { get; set; }
        public int EntryCount { get; set; }
        public int EmployeeCount { get; set; }
    }
}
