namespace CleanArchitecture.Core.Features.Reports.Models
{
    public class ProjectSummaryResponse
    {
        public int ProjectId { get; set; }
        public int TotalMinutes { get; set; }
        public decimal TotalExpenseAmount { get; set; }
        public decimal BillableEntryRate { get; set; }
    }
}
