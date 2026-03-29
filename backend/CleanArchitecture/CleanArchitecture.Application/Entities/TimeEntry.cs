using System;

namespace CleanArchitecture.Core.Entities
{
    public class TimeEntry : AuditableBaseEntity
    {
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public int DurationMinutes { get; set; }
        public string Description { get; set; }
        public bool IsBillable { get; set; }
        public string SourceType { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
