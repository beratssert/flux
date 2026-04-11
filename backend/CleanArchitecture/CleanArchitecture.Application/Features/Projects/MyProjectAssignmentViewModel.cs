using System;

namespace CleanArchitecture.Core.Features.Projects
{
    public class MyProjectAssignmentViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectStatus { get; set; }
        public DateTime AssignedAtUtc { get; set; }
    }
}
