using System.Collections.Generic;

namespace CleanArchitecture.Core.Features.Reports.Models
{
    /// <summary>One bucket in an expense report (category id, project id, or month key).</summary>
    public class ExpenseSummaryGroupItem
    {
        public string Key { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>Expense report payload grouped by category, project, or month.</summary>
    public class ExpenseSummaryResponse
    {
        public decimal TotalAmount { get; set; }
        public List<ExpenseSummaryGroupItem> Groups { get; set; } = new List<ExpenseSummaryGroupItem>();
    }
}
