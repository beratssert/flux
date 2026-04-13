using System;

namespace CleanArchitecture.Core.Entities
{
    public class CalendarEventParticipant
    {
        public Guid EventId { get; set; }
        public string UserId { get; set; }
        public string ParticipationType { get; set; }

        public CalendarEvent Event { get; set; }
    }
}
