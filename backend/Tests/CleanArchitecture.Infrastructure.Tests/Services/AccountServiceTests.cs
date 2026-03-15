using AutoFixture;
using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests.Services
{
    public class AccountServiceTests
    {
        private readonly Fixture _fixture;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IOptions<JWTSettings>> _jwtSettingsMock;
        private readonly Mock<IDateTimeService> _dateTimeServiceMock;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _fixture = new Fixture();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(roleStoreMock.Object, null, null, null, null);

            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(_userManagerMock.Object, contextAccessorMock.Object, claimsFactoryMock.Object, null, null, null, null);

            _emailServiceMock = new Mock<IEmailService>();
            
            _jwtSettingsMock = new Mock<IOptions<JWTSettings>>();
            _jwtSettingsMock.Setup(x => x.Value).Returns(new JWTSettings { Key = "C1CF4B7DC4C4175B6618DE4F55CA4ADUFDDNAJ12FDG", Issuer = "test", Audience = "test", DurationInMinutes = 60 });
            
            _dateTimeServiceMock = new Mock<IDateTimeService>();

            _accountService = new AccountService(
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _jwtSettingsMock.Object,
                _dateTimeServiceMock.Object,
                _signInManagerMock.Object,
                _emailServiceMock.Object
            );
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidEmail_ThrowsApiException()
        {
            // Arrange
            var request = new AuthenticationRequest { Email = "wrong@test.com", Password = "123" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.AuthenticateAsync(request, "127.0.0.1"));
            Assert.Contains($"No Accounts Registered with {request.Email}", ex.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidPassword_ThrowsApiException()
        {
            // Arrange
            var request = new AuthenticationRequest { Email = "test@test.com", Password = "wrongpassword" };
            var user = new ApplicationUser { Email = request.Email, UserName = "testuser" };
            
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.PasswordSignInAsync(user.UserName, request.Password, false, false)).ReturnsAsync(SignInResult.Failed);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.AuthenticateAsync(request, "127.0.0.1"));
            Assert.Contains($"Invalid Credentials for '{request.Email}'", ex.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_WithUnconfirmedEmail_ThrowsApiException()
        {
            // Arrange
            var request = new AuthenticationRequest { Email = "test@test.com", Password = "password" };
            var user = new ApplicationUser { Email = request.Email, UserName = "testuser", EmailConfirmed = false };
            
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.PasswordSignInAsync(user.UserName, request.Password, false, false)).ReturnsAsync(SignInResult.Success);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.AuthenticateAsync(request, "127.0.0.1"));
            Assert.Contains($"Account Not Confirmed for '{request.Email}'", ex.Message);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ReturnsAuthenticationResponse()
        {
            // Arrange
            var request = new AuthenticationRequest { Email = "test@test.com", Password = "password" };
            var user = new ApplicationUser { Id = "1", Email = request.Email, UserName = "testuser", EmailConfirmed = true };
            
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.PasswordSignInAsync(user.UserName, request.Password, false, false)).ReturnsAsync(SignInResult.Success);
            _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { Roles.Basic.ToString() });

            // Act
            var result = await _accountService.AuthenticateAsync(request, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.UserName, result.UserName);
            Assert.Contains(Roles.Basic.ToString(), result.Roles);
            Assert.True(result.IsVerified);
            Assert.NotNull(result.JWToken);
            Assert.NotNull(result.RefreshToken);
        }

        [Fact]
        public async Task RegisterAsync_WithInvalidRole_ThrowsApiException()
        {
            // Arrange
            var request = new RegisterRequest { UserName = "test", Email = "test@test.com", Role = "Admin" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.RegisterAsync(request, "http://localhost"));
            Assert.Contains("Invalid role 'Admin'. You can only register as Basic or Moderator.", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUserName_ThrowsApiException()
        {
            // Arrange
            var request = new RegisterRequest { UserName = "existinguser", Email = "test@test.com", Role = "Basic" };
            _userManagerMock.Setup(x => x.FindByNameAsync(request.UserName)).ReturnsAsync(new ApplicationUser());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.RegisterAsync(request, "http://localhost"));
            Assert.Contains($"Username '{request.UserName}' is already taken.", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ThrowsApiException()
        {
            // Arrange
            var request = new RegisterRequest { UserName = "newuser", Email = "existing@test.com", Role = "Basic" };
            _userManagerMock.Setup(x => x.FindByNameAsync(request.UserName)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(new ApplicationUser());

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApiException>(() => _accountService.RegisterAsync(request, "http://localhost"));
            Assert.Contains($"Email {request.Email } is already registered.", ex.Message);
        }

        [Fact]
        public async Task RegisterAsync_WithValidDataAndModeratorRole_ReturnsSuccessMessage()
        {
            // Arrange
            var request = new RegisterRequest { UserName = "newuser", Email = "new@test.com", Password = "password", Role = "Moderator" };
            _userManagerMock.Setup(x => x.FindByNameAsync(request.UserName)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Moderator.ToString())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("token");

            // Act
            var result = await _accountService.RegisterAsync(request, "http://localhost");

            // Assert
            Assert.Contains("User Registered", result);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Moderator.ToString()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithValidDataAndNoRole_AssignsBasicRole()
        {
            // Arrange
            var request = new RegisterRequest { UserName = "newuser2", Email = "new2@test.com", Password = "password", Role = null };
            _userManagerMock.Setup(x => x.FindByNameAsync(request.UserName)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Basic.ToString())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>())).ReturnsAsync("token");

            // Act
            var result = await _accountService.RegisterAsync(request, "http://localhost");

            // Assert
            Assert.Contains("User Registered", result);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Basic.ToString()), Times.Once);
        }
    }
}