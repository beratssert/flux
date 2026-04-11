using CleanArchitecture.Core.Filters;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetAllProjects
{
    /// <summary>Query parameters for <c>GET /projects</c> (inherits paging: <c>page</c>/<c>pageNumber</c>, <c>pageSize</c>).</summary>
    public class GetAllProjectsParameter : RequestParameter
    {
        /// <summary>Filter by exact status (Active, Archived, Closed).</summary>
        public string Status { get; set; }
        /// <summary>Filter by manager user id (mainly for admin-scoped lists).</summary>
        public string ManagerUserId { get; set; }
        /// <summary>Search in project name or code.</summary>
        public string Q { get; set; }
    }
}
