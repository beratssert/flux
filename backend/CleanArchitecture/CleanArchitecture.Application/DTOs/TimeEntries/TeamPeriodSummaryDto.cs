using System;

namespace CleanArchitecture.Core.DTOs.TimeEntries
{
    public class TeamPeriodSummaryDto
    {
        public DateTime EntryDate { get; set; }
        public int TotalDurationMinutes { get; set; }
        public int EntryCount { get; set; }
        public int ProjectCount { get; set; }
        public int EmployeeCount { get; set; }
    }
}
