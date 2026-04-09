namespace CleanArchitecture.Core.Entities
{
    public class ExpenseCategory : AuditableBaseEntity
    {
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
