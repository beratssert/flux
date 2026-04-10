using System.Collections.Generic;

namespace CleanArchitecture.Core.Features.Reports.Models
{
    public class ExpenseSummaryGroupItem
    {
        public string Key { get; set; }
        public decimal Amount { get; set; }
    }

    public class ExpenseSummaryResponse
    {
        public decimal TotalAmount { get; set; }
        public List<ExpenseSummaryGroupItem> Groups { get; set; } = new List<ExpenseSummaryGroupItem>();
    }
}
