using AutoMapper;
using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects.Commands.CreateProject;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Mappings;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests
{
    public class Projects
    {
        private readonly Mock<IProjectRepositoryAsync> _projectRepository = new();
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService = new();
        private readonly IMapper _mapper;

        public Projects()
        {
            _mapper = new MapperConfiguration(c => c.AddProfile<GeneralProfile>()).CreateMapper();
        }

        [Fact]
        public async Task CreateProject_WhenCodeTaken_ShouldThrowConflict()
        {
            _authenticatedUserService.SetupGet(x => x.UserId).Returns("mgr-1");
            _projectRepository.Setup(r => r.CodeExistsAsync("DUP", null)).ReturnsAsync(true);

            var handler = new CreateProjectCommandHandler(
                _projectRepository.Object,
                _authenticatedUserService.Object,
                _mapper);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new CreateProjectCommand { Name = "N", Code = "DUP" },
                default));
        }

        [Fact]
        public async Task CreateProject_WhenValid_ShouldPersistActiveAndManager()
        {
            _authenticatedUserService.SetupGet(x => x.UserId).Returns("mgr-1");
            _projectRepository.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
            Project? captured = null;
            _projectRepository.Setup(r => r.AddAsync(It.IsAny<Project>()))
                .Callback<Project>(p => captured = p)
                .ReturnsAsync((Project p) => p);

            var handler = new CreateProjectCommandHandler(
                _projectRepository.Object,
                _authenticatedUserService.Object,
                _mapper);

            var vm = await handler.Handle(
                new CreateProjectCommand { Name = "Alpha", Code = "A-1", Description = "d" },
                default);

            Assert.NotNull(captured);
            Assert.Equal(ProjectStatuses.Active, captured!.Status);
            Assert.Equal("mgr-1", captured.ManagerUserId);
            Assert.Equal("Alpha", vm.Name);
        }
    }
}
