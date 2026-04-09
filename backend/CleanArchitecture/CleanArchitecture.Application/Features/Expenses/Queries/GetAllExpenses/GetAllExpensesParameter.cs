using CleanArchitecture.Core.Filters;
using System;

namespace CleanArchitecture.Core.Features.Expenses.Queries.GetAllExpenses
{
    public class GetAllExpensesParameter : RequestParameter
    {
        public string UserId { get; set; }
        public int? ProjectId { get; set; }
        public int? CategoryId { get; set; }
        public string Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string SortBy { get; set; }
        public string SortDir { get; set; }
    }
}
