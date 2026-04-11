using System;

namespace CleanArchitecture.Core.DTOs.Projects
{
    public class MyProjectAssignmentRowDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectStatus { get; set; }
        public DateTime AssignedAtUtc { get; set; }
    }
}
