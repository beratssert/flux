using System;

namespace CleanArchitecture.Core.Features.Projects
{
    public class ProjectAssignmentItemViewModel
    {
        public string UserId { get; set; }
        public DateTime AssignedAtUtc { get; set; }
        public bool IsActive { get; set; }
    }
}
