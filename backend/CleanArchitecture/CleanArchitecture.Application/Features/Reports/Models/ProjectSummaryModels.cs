namespace CleanArchitecture.Core.Features.Reports.Models
{
    /// <summary>Project-level aggregates: time, expenses, and billable share of time entries (%).</summary>
    public class ProjectSummaryResponse
    {
        public int ProjectId { get; set; }
        public int TotalMinutes { get; set; }
        public decimal TotalExpenseAmount { get; set; }
        /// <summary>Percentage (0–100) of non-deleted time entries with <c>IsBillable</c> true.</summary>
        public decimal BillableEntryRate { get; set; }
    }
}
