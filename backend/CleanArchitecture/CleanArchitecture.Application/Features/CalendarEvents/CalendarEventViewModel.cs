using System;
using System.Collections.Generic;

namespace CleanArchitecture.Core.Features.CalendarEvents
{
    public class CalendarEventViewModel
    {
        public Guid Id { get; set; }
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public string CreatedByUserId { get; set; }
        public string VisibilityType { get; set; }
        public bool IsAllDay { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<CalendarEventParticipantViewModel> Participants { get; set; } = new();
    }
}
