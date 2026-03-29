using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.DTOs.Email;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Core.Wrappers;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Helpers;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly JWTSettings _jwtSettings;
        private readonly IDateTimeService _dateTimeService;
        private readonly ApplicationDbContext _dbContext;

        public AccountService(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JWTSettings> jwtSettings,
            IDateTimeService dateTimeService,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
            _dateTimeService = dateTimeService;
            _signInManager = signInManager;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new ApiException($"No Accounts Registered with {request.Email}.");
            }

            var effectiveStatus = string.IsNullOrWhiteSpace(user.Status) ? UserStatus.Active.ToString() : user.Status;
            if (!string.Equals(effectiveStatus, UserStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiException($"Account is not active for '{request.Email}'.");
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                throw new ApiException($"Invalid Credentials for '{request.Email}'.");
            }
            JwtSecurityToken jwtSecurityToken = await GenerateJWToken(user);
            AuthenticationResponse response = new AuthenticationResponse();
            response.Id = user.Id;
            response.JWToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            response.AccessToken = response.JWToken;
            response.ExpiresAtUtc = jwtSecurityToken.ValidTo;
            response.Email = user.Email;
            response.UserName = user.UserName;
            var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            response.Roles = rolesList.ToList();
            response.IsVerified = user.EmailConfirmed;
            var refreshToken = GenerateRefreshToken(ipAddress);
            response.RefreshToken = refreshToken.Token;

            user.LastLoginAtUtc = _dateTimeService.NowUtc;
            await _userManager.UpdateAsync(user);

            return response;
        }

        public async Task<string> RegisterAsync(RegisterRequest request, string origin)
        {
            var generatedUserName = !string.IsNullOrWhiteSpace(request.UserName)
                ? request.UserName
                : request.Email.Split('@')[0];

            var userWithSameUserName = await _userManager.FindByNameAsync(generatedUserName);
            if (userWithSameUserName != null)
            {
                throw new ApiException($"Username '{generatedUserName}' is already taken.");
            }

            var user = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = generatedUserName,
                Status = UserStatus.Active.ToString(),
                EmailConfirmed = true,
            };
            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail == null)
            {
                var result = await _userManager.CreateAsync(user, request.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Roles.Employee.ToString());
                    return "User registered successfully.";
                }
                else
                {
                    throw new ApiException($"{result.Errors}");
                }
            }
            else
            {
                throw new ApiException($"Email {request.Email } is already registered.");
            }
        }

        public async Task<string> CreateManagerAsync(CreateManagerRequest request)
        {
            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail != null)
            {
                throw new ApiException($"Email {request.Email} is already registered.");
            }

            var userNameBase = request.Email.Split('@')[0];
            var userName = userNameBase;
            var suffix = 1;

            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{userNameBase}{suffix}";
                suffix++;
            }

            var manager = new ApplicationUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = userName,
                EmailConfirmed = true,
                Status = UserStatus.Active.ToString(),
            };

            var createResult = await _userManager.CreateAsync(manager, request.Password);
            if (!createResult.Succeeded)
            {
                throw new ApiException("Unable to create manager account.");
            }

            await _userManager.AddToRoleAsync(manager, Roles.Manager.ToString());
            return "Manager account created.";
        }

        public async Task<string> UpdateUserRoleAsync(UpdateUserRoleRequest request)
        {
            if (!Enum.TryParse<Roles>(request.Role, true, out var parsedRole))
            {
                throw new ApiException("Invalid role.");
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await _userManager.AddToRoleAsync(user, parsedRole.ToString());
            return "User role updated.";
        }

        public async Task<string> UpdateUserStatusAsync(UpdateUserStatusRequest request)
        {
            if (!Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
            {
                throw new ApiException("Invalid status.");
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }

            user.Status = parsedStatus.ToString();
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ApiException("Unable to update user status.");
            }

            return "User status updated.";
        }

        public async Task<UserProfileResponse> GetMyProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Employee.ToString();

            return new UserProfileResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = role,
                IsActive = string.IsNullOrWhiteSpace(user.Status) || string.Equals(user.Status, UserStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase),
                LastLoginAtUtc = user.LastLoginAtUtc,
            };
        }

        public async Task<PagedResponse<UserProfileResponse>> GetUsersAsync(GetUsersRequest request, string requesterUserId)
        {
            var requester = await _userManager.FindByIdAsync(requesterUserId);
            if (requester == null)
            {
                throw new ApiException("Requester not found.");
            }

            var requesterRoles = (await _userManager.GetRolesAsync(requester)) ?? new List<string>();
            var isAdmin = requesterRoles.Contains(Roles.Admin.ToString());
            var isManager = requesterRoles.Contains(Roles.Manager.ToString());

            if (!isAdmin && !isManager)
            {
                throw new ApiException("You are not authorized to read users list.");
            }

            var usersQuery = _dbContext.Users.AsQueryable();

            if (isManager && !isAdmin)
            {
                var managedProjectIds = await _dbContext.Projects
                    .Where(p => p.ManagerUserId == requesterUserId)
                    .Select(p => p.Id)
                    .ToListAsync();

                var managedUserIds = await _dbContext.ProjectAssignments
                    .Where(pa => pa.IsActive && managedProjectIds.Contains(pa.ProjectId))
                    .Select(pa => pa.UserId)
                    .Distinct()
                    .ToListAsync();

                if (!managedUserIds.Contains(requesterUserId))
                {
                    managedUserIds.Add(requesterUserId);
                }

                usersQuery = usersQuery.Where(u => managedUserIds.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                var q = request.Q.ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Email.ToLower().Contains(q) ||
                    u.UserName.ToLower().Contains(q) ||
                    u.FirstName.ToLower().Contains(q) ||
                    u.LastName.ToLower().Contains(q));
            }

            if (request.IsActive.HasValue)
            {
                var activeStatus = UserStatus.Active.ToString();
                usersQuery = request.IsActive.Value
                    ? usersQuery.Where(u => string.IsNullOrWhiteSpace(u.Status) || u.Status == activeStatus)
                    : usersQuery.Where(u => !string.IsNullOrWhiteSpace(u.Status) && u.Status != activeStatus);
            }

            if (request.ProjectId.HasValue)
            {
                var projectId = request.ProjectId.Value;

                if (isManager && !isAdmin)
                {
                    var canAccessProject = await _dbContext.Projects.AnyAsync(p => p.Id == projectId && p.ManagerUserId == requesterUserId);
                    if (!canAccessProject)
                    {
                        return new PagedResponse<UserProfileResponse>(new List<UserProfileResponse>(), request.PageNumber, request.PageSize);
                    }
                }

                var userIdsForProject = await _dbContext.ProjectAssignments
                    .Where(pa => pa.IsActive && pa.ProjectId == projectId)
                    .Select(pa => pa.UserId)
                    .Distinct()
                    .ToListAsync();

                usersQuery = usersQuery.Where(u => userIdsForProject.Contains(u.Id));
            }

            var validPageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var validPageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

            var users = await usersQuery
                .OrderBy(u => u.Email)
                .Skip((validPageNumber - 1) * validPageSize)
                .Take(validPageSize)
                .ToListAsync();

            var responseData = new List<UserProfileResponse>();
            foreach (var user in users)
            {
                var userRoles = (await _userManager.GetRolesAsync(user)) ?? new List<string>();
                var userRole = userRoles.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(request.Role) && !string.Equals(userRole, request.Role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                responseData.Add(new UserProfileResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = userRole,
                    IsActive = string.IsNullOrWhiteSpace(user.Status) || string.Equals(user.Status, UserStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase),
                    LastLoginAtUtc = user.LastLoginAtUtc,
                });
            }

            return new PagedResponse<UserProfileResponse>(responseData, validPageNumber, validPageSize);
        }

        public async Task<UserProfileResponse> UpdateMyProfileAsync(string userId, UpdateMyProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApiException("User not found.");
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ApiException("Unable to update profile.");
            }

            return await GetMyProfileAsync(userId);
        }

        private async Task<JwtSecurityToken> GenerateJWToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));

            string ipAddress = IpHelper.GetIpAddress();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id),
                new Claim("ip", ipAddress)
            }
            .Union(userClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }

        private string RandomTokenString()
        {
            using var rngCryptoServiceProvider = RandomNumberGenerator.Create();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }

        private async Task<string> SendVerificationEmail(ApplicationUser user, string origin)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var route = "api/account/confirm-email/";
            var _enpointUri = new Uri(string.Concat($"{origin}/", route));
            var verificationUri = QueryHelpers.AddQueryString(_enpointUri.ToString(), "userId", user.Id);
            verificationUri = QueryHelpers.AddQueryString(verificationUri, "code", code);
            //Email Service Call Here
            return verificationUri;
        }

        public async Task<string> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                return  $"Account Confirmed for {user.Email}. You can now use the /api/Account/authenticate endpoint.";
            }
            else
            {
                throw new ApiException($"An error occured while confirming {user.Email}.");
            }
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = RandomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }

        public async Task<EmailRequest> ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);

            // always return ok response to prevent email enumeration
            if (account == null) throw new ApiException("User not found");

            var code = await _userManager.GeneratePasswordResetTokenAsync(account);
            var route = "api/account/reset-password/";
            var _enpointUri = new Uri(string.Concat($"{origin}/", route));
            var emailRequest = new EmailRequest()
            {
                Body = $"You reset token is - {code}",
                To = model.Email,
                Subject = "Reset Password",
            };
            //TODO: Attach Email Service here and configure it via appsettings
            //await _emailService.SendAsync(emailRequest);
            return emailRequest;
        }

        public async Task<string> ResetPassword(ResetPasswordRequest model)
        {
            var account = await _userManager.FindByEmailAsync(model.Email);
            if (account == null) throw new ApiException($"No Accounts Registered with {model.Email}.");
            var result = await _userManager.ResetPasswordAsync(account, model.Token, model.Password);
            if (result.Succeeded)
            {
                return  $"Password Resetted.";
            }
            else
            {
                throw new ApiException($"Error occured while reseting the password.");
            }
        }
    }
}
