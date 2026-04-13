using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Features.Projects.Commands.CreateProject;
using CleanArchitecture.Core.Features.Projects.Commands.CreateProjectAssignment;
using CleanArchitecture.Core.Features.Projects.Commands.RemoveProjectAssignment;
using CleanArchitecture.Core.Features.Projects.Commands.ReassignProjectManager;
using CleanArchitecture.Core.Features.Projects.Commands.UpdateProject;
using CleanArchitecture.Core.Features.Projects.Commands.UpdateProjectStatus;
using CleanArchitecture.Core.Features.Projects.Queries.GetAllProjects;
using CleanArchitecture.Core.Features.Projects.Queries.GetProjectAssignments;
using CleanArchitecture.Core.Features.Projects.Queries.GetProjectById;
using CleanArchitecture.Core.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    /// <summary>
    /// Projects and project assignments (manager-owned CRUD, role-scoped reads, admin manager reassignment).
    /// </summary>
    /// <remarks>
    /// All project identifiers (<c>id</c>, <c>projectId</c>) are <strong>integers</strong>, not UUIDs.
    /// </remarks>
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/projects")]
    public class ProjectsController : BaseApiController
    {
        /// <summary>Create a project (manager becomes owner; status Active).</summary>
        /// <remarks>Policy: <c>Projects.Manage.Own</c> (Manager). Optional unique <c>code</c>.</remarks>
        [HttpPost]
        [Authorize(Policy = "Projects.Manage.Own")]
        [ProducesResponseType(typeof(ProjectViewModel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Post([FromBody] CreateProjectCommand command)
        {
            var vm = await Mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = vm.Id }, vm);
        }

        /// <summary>List projects visible to the current role (employee: assigned; manager: managed; admin: all).</summary>
        /// <remarks>Query: <c>page</c>/<c>pageNumber</c>, <c>pageSize</c>, <c>status</c>, <c>managerUserId</c>, <c>q</c>.</remarks>
        [HttpGet]
        [Authorize(Policy = "Projects.Read.Assigned")]
        [ProducesResponseType(typeof(PagedResponse<ProjectViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<PagedResponse<ProjectViewModel>> Get([FromQuery] GetAllProjectsParameter filter)
        {
            return await Mediator.Send(new GetAllProjectsQuery
            {
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                Status = filter.Status,
                ManagerUserId = filter.ManagerUserId,
                Q = filter.Q
            });
        }

        /// <summary>Get one project if allowed by assignment or management scope.</summary>
        /// <remarks>Unknown or forbidden ids return 404.</remarks>
        [HttpGet("{id:int}")]
        [Authorize(Policy = "Projects.Read.Assigned")]
        [ProducesResponseType(typeof(ProjectViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            return Ok(await Mediator.Send(new GetProjectByIdQuery { Id = id }));
        }

        /// <summary>Update name, code, description, start/end dates (manager of project only).</summary>
        /// <remarks>Policy: <c>Projects.Manage.Own</c>. Admin cannot use this endpoint. Omit fields to leave unchanged; <c>code</c> must stay unique.</remarks>
        [HttpPatch("{id:int}")]
        [Authorize(Policy = "Projects.Manage.Own")]
        [ProducesResponseType(typeof(ProjectViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Patch(int id, [FromBody] UpdateProjectCommand body)
        {
            body.Id = id;
            return Ok(await Mediator.Send(body));
        }

        /// <summary>Set project lifecycle status: Active, Archived, or Closed.</summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = "Projects.Manage.Own")]
        [ProducesResponseType(typeof(ProjectViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchStatus(int id, [FromBody] UpdateProjectStatusCommand body)
        {
            body.Id = id;
            return Ok(await Mediator.Send(body));
        }

        /// <summary>Reassign project manager (Admin only; new user must have Manager role).</summary>
        [HttpPatch("{id:int}/manager")]
        [Authorize(Policy = "Projects.Reassign.Manager")]
        [ProducesResponseType(typeof(ProjectViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchManager(int id, [FromBody] ReassignProjectManagerCommand body)
        {
            body.Id = id;
            return Ok(await Mediator.Send(body));
        }

        /// <summary>Assign an employee to the project (active assignment).</summary>
        /// <remarks>Policy: <c>Assignments.Manage.OwnProject</c>. Target user must have Employee role. 409 if already actively assigned.</remarks>
        [HttpPost("{projectId:int}/assignments")]
        [Authorize(Policy = "Assignments.Manage.OwnProject")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PostAssignment(int projectId, [FromBody] CreateProjectAssignmentCommand body)
        {
            body.ProjectId = projectId;
            await Mediator.Send(body);
            return CreatedAtAction(nameof(GetAssignments), new { projectId }, null);
        }

        /// <summary>List active assignments for a project.</summary>
        /// <remarks>Roles: Manager (own project only) or Admin (any). Returns user ids and assignment metadata.</remarks>
        [HttpGet("{projectId:int}/assignments")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(List<ProjectAssignmentItemViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssignments(int projectId)
        {
            var list = await Mediator.Send(new GetProjectAssignmentsQuery { ProjectId = projectId });
            return Ok(list);
        }

        /// <summary>Remove assignment (soft: inactive + unassigned timestamp).</summary>
        [HttpDelete("{projectId:int}/assignments/{userId}")]
        [Authorize(Policy = "Assignments.Manage.OwnProject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAssignment(int projectId, string userId)
        {
            await Mediator.Send(new RemoveProjectAssignmentCommand { ProjectId = projectId, UserId = userId });
            return NoContent();
        }
    }
}
