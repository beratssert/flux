namespace CleanArchitecture.Core.Entities
{
    public class Project : AuditableBaseEntity
    {
        public string Name { get; set; }
        public string ManagerUserId { get; set; }
        public string Status { get; set; }
    }
}
