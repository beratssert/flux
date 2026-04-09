using System;

namespace CleanArchitecture.Core.Features.Calendar.Queries.GetCalendarItems
{
    /// <summary>Represents a single entry on the calendar (either a CalendarEvent or a TimeEntry).</summary>
    public class CalendarItemViewModel
    {
        public string ItemType { get; set; }   // "Event" | "TimeEntry"
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public bool AllDay { get; set; }
        public int? ProjectId { get; set; }
        public string Visibility { get; set; }   // only for Event items
        public int? DurationMinutes { get; set; } // only for TimeEntry items
        public bool? IsBillable { get; set; }    // only for TimeEntry items
    }
}
