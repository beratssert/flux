using System;

namespace CleanArchitecture.Core.DTOs.TimeEntries
{
    public class TimeSummaryRowDto
    {
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime EntryDate { get; set; }
        public int DurationMinutes { get; set; }
    }
}
