using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CleanArchitecture.WebApi.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Policy = "Users.Manage.All")]
    [Route("api/v{version:apiVersion}/admin/users")]
    public class AdminUsersController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public AdminUsersController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("manager")]
        public async Task<IActionResult> CreateManager(CreateManagerRequest request)
        {
            return Ok(await _accountService.CreateManagerAsync(request));
        }

        [HttpPatch("{userId}/role")]
        public async Task<IActionResult> UpdateRole(string userId, UpdateUserRoleRequest request)
        {
            request.UserId = userId;
            return Ok(await _accountService.UpdateUserRoleAsync(request));
        }

        [HttpPatch("{userId}/status")]
        public async Task<IActionResult> UpdateStatus(string userId, UpdateUserStatusRequest request)
        {
            request.UserId = userId;
            return Ok(await _accountService.UpdateUserStatusAsync(request));
        }
    }
}
