using System;

namespace CleanArchitecture.Core.Entities
{
    public class AuditLog : AuditableBaseEntity
    {
        public string ActorUserId { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string ActionType { get; set; }
        public string OldValuesJson { get; set; }
        public string NewValuesJson { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public string Note { get; set; }
    }
}
