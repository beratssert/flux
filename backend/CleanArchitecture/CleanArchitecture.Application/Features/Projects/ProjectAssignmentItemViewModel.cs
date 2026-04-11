using System;

namespace CleanArchitecture.Core.Features.Projects
{
    /// <summary>Active assignment entry for <c>GET /projects/{projectId}/assignments</c>.</summary>
    public class ProjectAssignmentItemViewModel
    {
        /// <summary>Assigned employee user id.</summary>
        public string UserId { get; set; }
        public DateTime AssignedAtUtc { get; set; }
        /// <summary>Always true for this list response.</summary>
        public bool IsActive { get; set; }
    }
}
