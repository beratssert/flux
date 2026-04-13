using System;

namespace CleanArchitecture.Core.Features.Projects
{
    /// <summary>Project returned by the projects API. <see cref="Id"/> is an integer primary key.</summary>
    public class ProjectViewModel
    {
        /// <summary>Database primary key (int).</summary>
        public int Id { get; set; }
        /// <summary>Display name.</summary>
        public string Name { get; set; }
        /// <summary>Optional business code; unique when set.</summary>
        public string Code { get; set; }
        /// <summary>Optional long description.</summary>
        public string Description { get; set; }
        /// <summary>ASP.NET Identity user id of the project manager.</summary>
        public string ManagerUserId { get; set; }
        /// <summary>One of: Active, Archived, Closed.</summary>
        public string Status { get; set; }
        /// <summary>Optional start date (date-only in API).</summary>
        public DateTime? StartDate { get; set; }
        /// <summary>Optional end date (date-only in API).</summary>
        public DateTime? EndDate { get; set; }
    }
}
