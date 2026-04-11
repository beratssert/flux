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
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/projects")]
    public class ProjectsController : BaseApiController
    {
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

        [HttpGet("{projectId:int}/assignments")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<ProjectAssignmentItemViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAssignments(int projectId)
        {
            var list = await Mediator.Send(new GetProjectAssignmentsQuery { ProjectId = projectId });
            return Ok(list);
        }

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
