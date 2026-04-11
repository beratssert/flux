using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Features.Projects.Queries.GetMyProjectAssignments;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CleanArchitecture.Core.DTOs.Account;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public UsersController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("me/assignments")]
        [Authorize(Policy = "Assignments.Read.Self")]
        [ProducesResponseType(typeof(List<MyProjectAssignmentViewModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyProjectAssignments()
        {
            return Ok(await Mediator.Send(new GetMyProjectAssignmentsQuery()));
        }

        [HttpGet("me")]
        [Authorize(Policy = "Users.Read.Self")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.GetMyProfileAsync(userId));
        }

        [HttpPatch("me")]
        [Authorize(Policy = "Users.Update.Self")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateMe(UpdateMyProfileRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.UpdateMyProfileAsync(userId, request));
        }

        [HttpGet]
        [Authorize(Policy = "Users.Read.Team")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.GetUsersAsync(request, userId));
        }
    }
}
