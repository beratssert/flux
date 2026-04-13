using CleanArchitecture.Core.DTOs.Account;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Settings;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using CleanArchitecture.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task RegisterAsync_ShouldAssignEmployeeRole()
        {
            var (service, userManager, _, _, _) = CreateSut();

            userManager
                .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);
            userManager
                .Setup(x => x.FindByEmailAsync("new@flux.local"))
                .ReturnsAsync((ApplicationUser)null);
            userManager
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            userManager
                .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Employee.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await service.RegisterAsync(new RegisterRequest
            {
                FirstName = "New",
                LastName = "User",
                Email = "new@flux.local",
                Password = "123Pa$$word!"
            }, "http://localhost");

            Assert.Equal("User registered successfully.", result);
            userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Employee.ToString()), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenSuspended_ShouldThrowApiException()
        {
            var (service, userManager, _, _, _) = CreateSut();

            userManager
                .Setup(x => x.FindByEmailAsync("employee@flux.local"))
                .ReturnsAsync(new ApplicationUser
                {
                    Id = "user-1",
                    Email = "employee@flux.local",
                    UserName = "employee",
                    Status = UserStatus.Suspended.ToString(),
                    EmailConfirmed = true,
                });

            await Assert.ThrowsAsync<ApiException>(() => service.AuthenticateAsync(new AuthenticationRequest
            {
                Email = "employee@flux.local",
                Password = "123Pa$$word!"
            }, "127.0.0.1"));
        }

        [Fact]
        public async Task CreateManagerAsync_ShouldAssignManagerRole()
        {
            var (service, userManager, _, _, _) = CreateSut();

            userManager
                .Setup(x => x.FindByEmailAsync("manager2@flux.local"))
                .ReturnsAsync((ApplicationUser)null);
            userManager
                .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((ApplicationUser)null);
            userManager
                .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            userManager
                .Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Manager.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await service.CreateManagerAsync(new CreateManagerRequest
            {
                FirstName = "Mina",
                LastName = "Manager",
                Email = "manager2@flux.local",
                Password = "123Pa$$word!"
            });

            Assert.Equal("Manager account created.", result);
            userManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Roles.Manager.ToString()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserRoleAsync_InvalidRole_ShouldThrowApiException()
        {
            var (service, _, _, _, _) = CreateSut();

            await Assert.ThrowsAsync<ApiException>(() => service.UpdateUserRoleAsync(new UpdateUserRoleRequest
            {
                UserId = "any-user",
                Role = "UnknownRole"
            }));
        }

        [Fact]
        public async Task AuthenticateAsync_ShouldReturnAccessTokenAndRoles()
        {
            var (service, userManager, _, signInManager, _) = CreateSut();
            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "employee@flux.local",
                UserName = "employee",
                Status = UserStatus.Active.ToString(),
                EmailConfirmed = true,
            };

            userManager
                .Setup(x => x.FindByEmailAsync("employee@flux.local"))
                .ReturnsAsync(user);
            signInManager
                .Setup(x => x.PasswordSignInAsync("employee", "123Pa$$word!", false, false))
                .ReturnsAsync(SignInResult.Success);
            userManager
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { Roles.Employee.ToString() });
            userManager
                .Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());
            userManager
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var response = await service.AuthenticateAsync(new AuthenticationRequest
            {
                Email = "employee@flux.local",
                Password = "123Pa$$word!"
            }, "127.0.0.1");

            Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
            Assert.Contains(Roles.Employee.ToString(), response.Roles);
            Assert.Equal(response.AccessToken, response.JWToken);
        }

        [Fact]
        public async Task UpdateMyProfileAsync_ShouldUpdateNames()
        {
            var (service, userManager, _, _, _) = CreateSut();

            var user = new ApplicationUser
            {
                Id = "user-1",
                Email = "employee@flux.local",
                UserName = "employee",
                FirstName = "Old",
                LastName = "Name"
            };

            userManager.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
            userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { Roles.Employee.ToString() });

            var response = await service.UpdateMyProfileAsync("user-1", new UpdateMyProfileRequest
            {
                FirstName = "New",
                LastName = "Surname"
            });

            Assert.Equal("New", response.FirstName);
            Assert.Equal("Surname", response.LastName);
        }

        [Fact]
        public async Task GetUsersAsync_WhenRequesterIsManager_ShouldReturnManagedUsers()
        {
            var (service, userManager, _, _, dbContext) = CreateSut();

            var manager = new ApplicationUser { Id = "mgr-1", Email = "manager@flux.local", UserName = "manager", FirstName = "M", LastName = "G" };
            var employee = new ApplicationUser { Id = "emp-1", Email = "employee@flux.local", UserName = "employee", FirstName = "E", LastName = "M" };

            dbContext.Users.Add(manager);
            dbContext.Users.Add(employee);
            dbContext.Projects.Add(new CleanArchitecture.Core.Entities.Project { Id = 10, Name = "P", ManagerUserId = "mgr-1", Status = "Active" });
            dbContext.ProjectAssignments.Add(new CleanArchitecture.Core.Entities.ProjectAssignment
            {
                ProjectId = 10,
                UserId = "emp-1",
                AssignedAtUtc = System.DateTime.UtcNow,
                IsActive = true
            });
            await dbContext.SaveChangesAsync();

            userManager.Setup(x => x.FindByIdAsync("mgr-1")).ReturnsAsync(manager);
            userManager.Setup(x => x.GetRolesAsync(manager)).ReturnsAsync(new List<string> { Roles.Manager.ToString() });
            userManager.Setup(x => x.GetRolesAsync(employee)).ReturnsAsync(new List<string> { Roles.Employee.ToString() });

            var result = await service.GetUsersAsync(new GetUsersRequest { PageNumber = 1, PageSize = 10 }, "mgr-1");

            Assert.True(result.Data.Count >= 2);
        }

        private static (AccountService service, Mock<UserManager<ApplicationUser>> userManager, Mock<RoleManager<IdentityRole>> roleManager, Mock<SignInManager<ApplicationUser>> signInManager, ApplicationDbContext dbContext) CreateSut()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            var roleStore = new Mock<IRoleStore<IdentityRole>>();
            var roleManager = new Mock<RoleManager<IdentityRole>>(
                roleStore.Object,
                null,
                null,
                null,
                null);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(x => x.Value).Returns(new IdentityOptions());
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            var signInManager = new Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            var emailService = new Mock<IEmailService>();
            var jwt = Options.Create(new JWTSettings
            {
                Key = "C1CF4B7DC4C4175B6618DE4F55CA4ADUFDDNAJ12FDG",
                Issuer = "CoreIdentity",
                Audience = "CoreIdentityUser",
                DurationInMinutes = 60,
            });

            var dateTime = new Mock<IDateTimeService>();
            dateTime.SetupGet(x => x.NowUtc).Returns(DateTime.UtcNow);

            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var authenticatedUserService = new Mock<IAuthenticatedUserService>();
            var dbContext = new ApplicationDbContext(dbOptions, dateTime.Object, authenticatedUserService.Object);

            var service = new AccountService(
                userManager.Object,
                roleManager.Object,
                jwt,
                dateTime.Object,
                signInManager.Object,
                emailService.Object,
                dbContext);

            return (service, userManager, roleManager, signInManager, dbContext);
        }
    }
}
