using System;

namespace CleanArchitecture.Core.Entities
{
    public class Expense : AuditableBaseEntity
    {
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public int CategoryId { get; set; }
        public string Notes { get; set; }
        public string ReceiptUrl { get; set; }
        public string Status { get; set; }
        public string RejectionReason { get; set; }
        public string ReviewedByUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
    }
}
