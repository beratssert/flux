using System;

namespace CleanArchitecture.Core.DTOs.Expenses
{
    public class ExpenseSummaryRowDto
    {
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public int CategoryId { get; set; }
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
    }
}
