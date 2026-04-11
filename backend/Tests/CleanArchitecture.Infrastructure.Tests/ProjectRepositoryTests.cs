using AutoFixture;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.Infrastructure.Tests
{
    public class ProjectRepositoryTests
    {
        private readonly Fixture _fixture;
        private readonly Mock<IDateTimeService> _dateTimeService;
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService;
        private readonly ApplicationDbContext _context;

        public ProjectRepositoryTests()
        {
            _fixture = new Fixture();
            _dateTimeService = new Mock<IDateTimeService>();
            _authenticatedUserService = new Mock<IAuthenticatedUserService>();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_fixture.Create<string>());

            _context = new ApplicationDbContext(optionsBuilder.Options, _dateTimeService.Object, _authenticatedUserService.Object);
        }

        [Fact]
        public async Task GetPagedForEmployeeAsync_ShouldReturnOnlyAssignedActiveProjects()
        {
            const string employeeId = "emp-a";

            _context.Projects.AddRange(
                new Project { Id = 1, Name = "A", ManagerUserId = "mgr-1", Status = "Active" },
                new Project { Id = 2, Name = "B", ManagerUserId = "mgr-1", Status = "Active" });

            _context.ProjectAssignments.Add(new ProjectAssignment
            {
                UserId = employeeId,
                ProjectId = 1,
                AssignedAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await _context.SaveChangesAsync();

            var repository = new ProjectRepositoryAsync(_context);
            var (items, total) = await repository.GetPagedForEmployeeAsync(employeeId, 1, 10, null, null, null);

            Assert.Equal(1, total);
            Assert.Single(items);
            Assert.Equal(1, items[0].Id);
        }

        [Fact]
        public async Task GetPagedForManagerAsync_ShouldReturnOnlyManagedProjects()
        {
            const string managerId = "mgr-x";

            _context.Projects.AddRange(
                new Project { Id = 10, Name = "Mine", ManagerUserId = managerId, Status = "Active" },
                new Project { Id = 11, Name = "Other", ManagerUserId = "other", Status = "Active" });

            await _context.SaveChangesAsync();

            var repository = new ProjectRepositoryAsync(_context);
            var (items, total) = await repository.GetPagedForManagerAsync(managerId, 1, 10, null, null, null);

            Assert.Equal(1, total);
            Assert.Equal(10, items[0].Id);
        }

        [Fact]
        public async Task CodeExistsAsync_ShouldRespectExcludeId()
        {
            _context.Projects.Add(new Project { Id = 5, Name = "P", Code = "CODE-1", ManagerUserId = "m", Status = "Active" });
            await _context.SaveChangesAsync();

            var repository = new ProjectRepositoryAsync(_context);
            Assert.True(await repository.CodeExistsAsync("CODE-1", null));
            Assert.False(await repository.CodeExistsAsync("CODE-1", 5));
        }
    }
}
