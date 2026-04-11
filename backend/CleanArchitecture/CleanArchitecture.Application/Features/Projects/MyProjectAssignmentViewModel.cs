using System;

namespace CleanArchitecture.Core.Features.Projects
{
    /// <summary>One active assignment row for <c>GET /users/me/assignments</c>.</summary>
    public class MyProjectAssignmentViewModel
    {
        /// <summary>Project primary key (int).</summary>
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectStatus { get; set; }
        public DateTime AssignedAtUtc { get; set; }
    }
}
