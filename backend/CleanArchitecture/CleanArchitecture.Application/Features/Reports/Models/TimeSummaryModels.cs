using System.Collections.Generic;

namespace CleanArchitecture.Core.Features.Reports.Models
{
    /// <summary>One bucket in a time report (e.g. ISO week key or project id as string).</summary>
    public class TimeSummaryGroupItem
    {
        public string Key { get; set; }
        public int Minutes { get; set; }
    }

    /// <summary>Time report payload: sum of <see cref="TimeSummaryGroupItem.Minutes"/> equals <see cref="TotalMinutes"/>.</summary>
    public class TimeSummaryResponse
    {
        public int TotalMinutes { get; set; }
        public List<TimeSummaryGroupItem> Groups { get; set; } = new List<TimeSummaryGroupItem>();
    }
}
