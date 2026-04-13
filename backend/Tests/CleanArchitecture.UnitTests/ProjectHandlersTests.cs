using AutoMapper;
using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Features.Projects.Commands.CreateProjectAssignment;
using CleanArchitecture.Core.Features.Projects.Commands.ReassignProjectManager;
using CleanArchitecture.Core.Features.Projects.Commands.RemoveProjectAssignment;
using CleanArchitecture.Core.Features.Projects.Commands.UpdateProject;
using CleanArchitecture.Core.Features.Projects.Commands.UpdateProjectStatus;
using CleanArchitecture.Core.Features.Projects.Queries.GetAllProjects;
using CleanArchitecture.Core.Features.Projects.Queries.GetProjectAssignments;
using CleanArchitecture.Core.Features.Projects.Queries.GetProjectById;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Mappings;
using CleanArchitecture.Core.Wrappers;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.UnitTests;

public class ProjectHandlersTests
{
    private readonly Mock<IProjectRepositoryAsync> _projects = new();
    private readonly Mock<IProjectAssignmentRepositoryAsync> _assignments = new();
    private readonly Mock<IAuthenticatedUserService> _auth = new();
    private readonly Mock<IUserRolesService> _roles = new();
    private readonly IMapper _mapper;

    public ProjectHandlersTests()
    {
        _mapper = new MapperConfiguration(c => c.AddProfile<GeneralProfile>()).CreateMapper();
    }

    [Fact]
    public async Task UpdateProject_WhenNotManager_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.GetByIdAsync(1, true))
            .ReturnsAsync(new Project { Id = 1, Name = "P", ManagerUserId = "other", Status = ProjectStatuses.Active });

        var handler = new UpdateProjectCommandHandler(_projects.Object, _auth.Object, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new UpdateProjectCommand { Id = 1, Name = "X" },
            default));
    }

    [Fact]
    public async Task UpdateProject_WhenEndBeforeStart_ShouldThrowApiException()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        var project = new Project
        {
            Id = 1,
            Name = "P",
            ManagerUserId = "mgr-1",
            Status = ProjectStatuses.Active,
            StartDate = null,
            EndDate = null
        };
        _projects.Setup(p => p.GetByIdAsync(1, true)).ReturnsAsync(project);

        var handler = new UpdateProjectCommandHandler(_projects.Object, _auth.Object, _mapper);

        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new UpdateProjectCommand
            {
                Id = 1,
                StartDate = new DateTime(2026, 6, 10),
                EndDate = new DateTime(2026, 6, 1)
            },
            default));
    }

    [Fact]
    public async Task UpdateProject_WhenCodeTaken_ShouldThrowConflict()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        var project = new Project { Id = 1, Name = "P", ManagerUserId = "mgr-1", Status = ProjectStatuses.Active };
        _projects.Setup(p => p.GetByIdAsync(1, true)).ReturnsAsync(project);
        _projects.Setup(p => p.CodeExistsAsync("USED", 1)).ReturnsAsync(true);

        var handler = new UpdateProjectCommandHandler(_projects.Object, _auth.Object, _mapper);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new UpdateProjectCommand { Id = 1, Code = "USED" },
            default));
    }

    [Fact]
    public async Task UpdateProject_WhenValid_ShouldUpdateAndReturnVm()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        var project = new Project { Id = 1, Name = "Old", ManagerUserId = "mgr-1", Status = ProjectStatuses.Active };
        _projects.Setup(p => p.GetByIdAsync(1, true)).ReturnsAsync(project);
        _projects.Setup(p => p.CodeExistsAsync(It.IsAny<string>(), 1)).ReturnsAsync(false);
        Project? updated = null;
        _projects.Setup(p => p.UpdateAsync(It.IsAny<Project>()))
            .Callback<Project>(p => updated = p)
            .Returns(Task.CompletedTask);
        _projects.Setup(p => p.GetByIdAsync(1, false))
            .ReturnsAsync(() => updated ?? project);

        var handler = new UpdateProjectCommandHandler(_projects.Object, _auth.Object, _mapper);
        var vm = await handler.Handle(new UpdateProjectCommand { Id = 1, Name = "New" }, default);

        Assert.Equal("New", vm.Name);
        Assert.NotNull(updated);
        Assert.Equal("New", updated!.Name);
    }

    [Fact]
    public async Task UpdateProjectStatus_WhenInvalidStatus_ShouldThrow()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        var handler = new UpdateProjectStatusCommandHandler(_projects.Object, _auth.Object, _mapper);

        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new UpdateProjectStatusCommand { Id = 1, Status = "Suspended" },
            default));
    }

    [Fact]
    public async Task UpdateProjectStatus_WhenValid_ShouldNormalizeArchived()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        var project = new Project { Id = 1, Name = "P", ManagerUserId = "mgr-1", Status = ProjectStatuses.Active };
        _projects.Setup(p => p.GetByIdAsync(1, true)).ReturnsAsync(project);
        _projects.Setup(p => p.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
        _projects.Setup(p => p.GetByIdAsync(1, false)).ReturnsAsync(() => project);

        var handler = new UpdateProjectStatusCommandHandler(_projects.Object, _auth.Object, _mapper);
        var vm = await handler.Handle(
            new UpdateProjectStatusCommand { Id = 1, Status = "archived" },
            default);

        Assert.Equal(ProjectStatuses.Archived, project.Status);
        Assert.Equal(ProjectStatuses.Archived, vm.Status);
    }

    [Fact]
    public async Task ReassignManager_WhenTargetNotManagerRole_ShouldThrow()
    {
        _auth.SetupGet(a => a.UserId).Returns("admin-1");
        _roles.Setup(r => r.UserExistsAsync("u1")).ReturnsAsync(true);
        _roles.Setup(r => r.GetRolesAsync("u1")).ReturnsAsync(new List<string> { "Employee" });

        var handler = new ReassignProjectManagerCommandHandler(
            _projects.Object,
            _auth.Object,
            _roles.Object,
            _mapper);

        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(
            new ReassignProjectManagerCommand { Id = 1, ManagerUserId = "u1" },
            default));
    }

    [Fact]
    public async Task ReassignManager_WhenValid_ShouldUpdateManager()
    {
        _auth.SetupGet(a => a.UserId).Returns("admin-1");
        _roles.Setup(r => r.UserExistsAsync("new-mgr")).ReturnsAsync(true);
        _roles.Setup(r => r.GetRolesAsync("new-mgr")).ReturnsAsync(new List<string> { Roles.Manager.ToString() });
        var project = new Project { Id = 1, Name = "P", ManagerUserId = "old-mgr", Status = ProjectStatuses.Active };
        _projects.Setup(p => p.GetByIdAsync(1, true)).ReturnsAsync(project);
        _projects.Setup(p => p.UpdateAsync(It.IsAny<Project>())).Returns(Task.CompletedTask);
        _projects.Setup(p => p.GetByIdAsync(1, false)).ReturnsAsync(() => project);

        var handler = new ReassignProjectManagerCommandHandler(
            _projects.Object,
            _auth.Object,
            _roles.Object,
            _mapper);

        var vm = await handler.Handle(
            new ReassignProjectManagerCommand { Id = 1, ManagerUserId = "new-mgr" },
            default);

        Assert.Equal("new-mgr", project.ManagerUserId);
        Assert.Equal("new-mgr", vm.ManagerUserId);
    }

    [Fact]
    public async Task CreateAssignment_WhenNotManaged_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.IsManagedByAsync("mgr-1", 9)).ReturnsAsync(false);

        var handler = new CreateProjectAssignmentCommandHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object,
            _roles.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateProjectAssignmentCommand { ProjectId = 9, UserId = "emp-1" },
            default));
    }

    [Fact]
    public async Task CreateAssignment_WhenDuplicateActive_ShouldThrowConflict()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.IsManagedByAsync("mgr-1", 1)).ReturnsAsync(true);
        _roles.Setup(r => r.UserExistsAsync("emp-1")).ReturnsAsync(true);
        _roles.Setup(r => r.GetRolesAsync("emp-1")).ReturnsAsync(new List<string> { Roles.Employee.ToString() });
        _assignments.Setup(a => a.HasActiveAssignmentAsync(1, "emp-1")).ReturnsAsync(true);

        var handler = new CreateProjectAssignmentCommandHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object,
            _roles.Object);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CreateProjectAssignmentCommand { ProjectId = 1, UserId = "emp-1" },
            default));
    }

    [Fact]
    public async Task CreateAssignment_WhenValid_ShouldAdd()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.IsManagedByAsync("mgr-1", 1)).ReturnsAsync(true);
        _roles.Setup(r => r.UserExistsAsync("emp-1")).ReturnsAsync(true);
        _roles.Setup(r => r.GetRolesAsync("emp-1")).ReturnsAsync(new List<string> { Roles.Employee.ToString() });
        _assignments.Setup(a => a.HasActiveAssignmentAsync(1, "emp-1")).ReturnsAsync(false);
        ProjectAssignment? added = null;
        _assignments.Setup(a => a.AddAsync(It.IsAny<ProjectAssignment>()))
            .Callback<ProjectAssignment>(x => added = x)
            .ReturnsAsync((ProjectAssignment x) => x);

        var handler = new CreateProjectAssignmentCommandHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object,
            _roles.Object);

        await handler.Handle(
            new CreateProjectAssignmentCommand { ProjectId = 1, UserId = "emp-1" },
            default);

        Assert.NotNull(added);
        Assert.Equal(1, added!.ProjectId);
        Assert.Equal("emp-1", added.UserId);
        Assert.True(added.IsActive);
    }

    [Fact]
    public async Task RemoveAssignment_WhenMissing_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.IsManagedByAsync("mgr-1", 1)).ReturnsAsync(true);
        _assignments.Setup(a => a.GetActiveByProjectAndUserAsync(1, "emp-1")).ReturnsAsync((ProjectAssignment)null!);

        var handler = new RemoveProjectAssignmentCommandHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new RemoveProjectAssignmentCommand { ProjectId = 1, UserId = "emp-1" },
            default));
    }

    [Fact]
    public async Task RemoveAssignment_WhenValid_ShouldDeactivate()
    {
        _auth.SetupGet(a => a.UserId).Returns("mgr-1");
        _projects.Setup(p => p.IsManagedByAsync("mgr-1", 1)).ReturnsAsync(true);
        var row = new ProjectAssignment
        {
            Id = 10,
            ProjectId = 1,
            UserId = "emp-1",
            IsActive = true
        };
        _assignments.Setup(a => a.GetActiveByProjectAndUserAsync(1, "emp-1")).ReturnsAsync(row);
        _assignments.Setup(a => a.UpdateAsync(It.IsAny<ProjectAssignment>())).Returns(Task.CompletedTask);

        var handler = new RemoveProjectAssignmentCommandHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object);

        await handler.Handle(
            new RemoveProjectAssignmentCommand { ProjectId = 1, UserId = "emp-1" },
            default);

        Assert.False(row.IsActive);
        Assert.NotNull(row.UnassignedAtUtc);
    }

    [Fact]
    public async Task GetAllProjects_Admin_ShouldCallAdminRepository()
    {
        _auth.SetupGet(a => a.UserId).Returns("a1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Admin.ToString());
        var list = new List<Project>
        {
            new() { Id = 1, Name = "P1", ManagerUserId = "m", Status = ProjectStatuses.Active }
        };
        _projects.Setup(p => p.GetPagedForAdminAsync(1, 10, null, null, null))
            .ReturnsAsync((list, 1));

        var handler = new GetAllProjectsQueryHandler(_projects.Object, _auth.Object, _mapper);
        var page = await handler.Handle(
            new GetAllProjectsQuery { PageNumber = 1, PageSize = 10 },
            default);

        Assert.Single(page.Items);
        _projects.Verify(p => p.GetPagedForAdminAsync(1, 10, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetAllProjects_NormalizesPageDefaults()
    {
        _auth.SetupGet(a => a.UserId).Returns("e1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Employee.ToString());
        _projects.Setup(p => p.GetPagedForEmployeeAsync("e1", 1, 10, null, null, null))
            .ReturnsAsync((new List<Project>(), 0));

        var handler = new GetAllProjectsQueryHandler(_projects.Object, _auth.Object, _mapper);
        await handler.Handle(
            new GetAllProjectsQuery { PageNumber = 0, PageSize = 0 },
            default);

        _projects.Verify(p => p.GetPagedForEmployeeAsync("e1", 1, 10, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetProjectById_WhenEmployeeNotAllowed_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("e1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Employee.ToString());
        _projects.Setup(p => p.GetByIdAsync(5, false))
            .ReturnsAsync(new Project { Id = 5, Name = "X", ManagerUserId = "m", Status = ProjectStatuses.Active });
        _projects.Setup(p => p.CanEmployeeViewAsync("e1", 5)).ReturnsAsync(false);

        var handler = new GetProjectByIdQueryHandler(_projects.Object, _auth.Object, _mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetProjectByIdQuery { Id = 5 },
            default));
    }

    [Fact]
    public async Task GetProjectById_WhenEmployeeAllowed_ShouldReturnVm()
    {
        _auth.SetupGet(a => a.UserId).Returns("e1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Employee.ToString());
        var project = new Project { Id = 5, Name = "Visible", ManagerUserId = "m", Status = ProjectStatuses.Active };
        _projects.Setup(p => p.GetByIdAsync(5, false)).ReturnsAsync(project);
        _projects.Setup(p => p.CanEmployeeViewAsync("e1", 5)).ReturnsAsync(true);

        var handler = new GetProjectByIdQueryHandler(_projects.Object, _auth.Object, _mapper);
        var vm = await handler.Handle(new GetProjectByIdQuery { Id = 5 }, default);

        Assert.Equal("Visible", vm.Name);
    }

    [Fact]
    public async Task GetProjectAssignments_WhenEmployee_ShouldThrowNotFound()
    {
        _auth.SetupGet(a => a.UserId).Returns("e1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Employee.ToString());
        _projects.Setup(p => p.GetByIdAsync(1, false))
            .ReturnsAsync(new Project { Id = 1, Name = "P", ManagerUserId = "m", Status = ProjectStatuses.Active });

        var handler = new GetProjectAssignmentsQueryHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new GetProjectAssignmentsQuery { ProjectId = 1 },
            default));
    }

    [Fact]
    public async Task GetProjectAssignments_WhenManagerOwnProject_ShouldReturnRows()
    {
        _auth.SetupGet(a => a.UserId).Returns("m1");
        _auth.SetupGet(a => a.Role).Returns(Roles.Manager.ToString());
        _projects.Setup(p => p.GetByIdAsync(1, false))
            .ReturnsAsync(new Project { Id = 1, Name = "P", ManagerUserId = "m1", Status = ProjectStatuses.Active });
        var rows = new List<ProjectAssignment>
        {
            new()
            {
                UserId = "u1",
                AssignedAtUtc = DateTime.UtcNow,
                IsActive = true
            }
        };
        _assignments.Setup(a => a.GetActiveByProjectIdAsync(1)).ReturnsAsync(rows);

        var handler = new GetProjectAssignmentsQueryHandler(
            _projects.Object,
            _assignments.Object,
            _auth.Object);

        var list = await handler.Handle(new GetProjectAssignmentsQuery { ProjectId = 1 }, default);

        Assert.Single(list);
        Assert.Equal("u1", list[0].UserId);
    }
}
