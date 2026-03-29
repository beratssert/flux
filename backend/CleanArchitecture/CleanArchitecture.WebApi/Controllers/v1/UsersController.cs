using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("me")]
        [Authorize(Policy = "Users.Read.Self")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.GetMyProfileAsync(userId));
        }

        [HttpPatch("me")]
        [Authorize(Policy = "Users.Update.Self")]
        public async Task<IActionResult> UpdateMe(UpdateMyProfileRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.UpdateMyProfileAsync(userId, request));
        }

        [HttpGet]
        [Authorize(Policy = "Users.Read.Team")]
        public async Task<IActionResult> GetUsers([FromQuery] GetUsersRequest request)
        {
            var userId = User.FindFirstValue("uid");
            return Ok(await _accountService.GetUsersAsync(request, userId));
        }
    }
}
