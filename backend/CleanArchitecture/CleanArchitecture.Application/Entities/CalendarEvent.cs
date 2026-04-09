using System;

namespace CleanArchitecture.Core.Entities
{
    public class CalendarEvent : AuditableBaseEntity
    {
        public string CreatedByUserId { get; set; }
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public bool AllDay { get; set; }
        /// <summary>Personal, Project, or Team</summary>
        public string Visibility { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
