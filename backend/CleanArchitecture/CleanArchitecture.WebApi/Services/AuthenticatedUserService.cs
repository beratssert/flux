using CleanArchitecture.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CleanArchitecture.WebApi.Services
{
    public class AuthenticatedUserService : IAuthenticatedUserService
    {
        public AuthenticatedUserService(IHttpContextAccessor httpContextAccessor)
        {
            UserId = httpContextAccessor.HttpContext?.User?.FindFirstValue("uid");
            Role = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
        }

        public string UserId { get; }
        public string Role { get; }
    }
}
