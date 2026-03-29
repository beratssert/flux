using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.DTOs.Email;
using CleanArchitecture.Core.Wrappers;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IAccountService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress);
        Task<string> RegisterAsync(RegisterRequest request, string origin);
        Task<string> CreateManagerAsync(CreateManagerRequest request);
        Task<string> UpdateUserRoleAsync(UpdateUserRoleRequest request);
        Task<string> UpdateUserStatusAsync(UpdateUserStatusRequest request);
        Task<UserProfileResponse> GetMyProfileAsync(string userId);
        Task<PagedResponse<UserProfileResponse>> GetUsersAsync(GetUsersRequest request, string requesterUserId);
        Task<UserProfileResponse> UpdateMyProfileAsync(string userId, UpdateMyProfileRequest request);
        Task<string> ConfirmEmailAsync(string userId, string code);
        Task<EmailRequest> ForgotPassword(ForgotPasswordRequest model, string origin);
        Task<string> ResetPassword(ResetPasswordRequest model);
    }
}
