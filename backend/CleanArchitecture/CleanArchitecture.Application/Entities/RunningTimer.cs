using System;

namespace CleanArchitecture.Core.Entities
{
    public class RunningTimer : AuditableBaseEntity
    {
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public string Description { get; set; }
        public bool IsBillable { get; set; }
    }
}
