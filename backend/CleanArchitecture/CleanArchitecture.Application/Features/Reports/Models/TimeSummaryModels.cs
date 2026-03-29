using System.Collections.Generic;

namespace CleanArchitecture.Core.Features.Reports.Models
{
    public class TimeSummaryGroupItem
    {
        public string Key { get; set; }
        public int Minutes { get; set; }
    }

    public class TimeSummaryResponse
    {
        public int TotalMinutes { get; set; }
        public List<TimeSummaryGroupItem> Groups { get; set; } = new List<TimeSummaryGroupItem>();
    }
}
