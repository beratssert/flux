using System;

namespace CleanArchitecture.Core.Entities
{
    public class ProjectAssignment : AuditableBaseEntity
    {
        public int ProjectId { get; set; }
        public string UserId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UnassignedAtUtc { get; set; }
    }
}
