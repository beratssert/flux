using CleanArchitecture.Core.Filters;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetAllProjects
{
    public class GetAllProjectsParameter : RequestParameter
    {
        public string Status { get; set; }
        public string ManagerUserId { get; set; }
        public string Q { get; set; }
    }
}
