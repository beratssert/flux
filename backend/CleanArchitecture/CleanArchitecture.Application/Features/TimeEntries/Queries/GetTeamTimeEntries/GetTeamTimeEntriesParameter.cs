using CleanArchitecture.Core.Filters;
using System;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamTimeEntries
{
    public class GetTeamTimeEntriesParameter : RequestParameter
    {
        public int? ProjectId { get; set; }
        public string EmployeeUserId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
