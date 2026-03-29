using AutoFixture;
using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.TimeEntries.Commands.CreateTimeEntry;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetAllTimeEntries;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamPeriodSummary;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamTimeEntries;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class TimeEntries
    {
        private readonly Fixture _fixture;
        private readonly Mock<ITimeEntryRepositoryAsync> _timeEntryRepository;
        private readonly Mock<IProjectAssignmentRepositoryAsync> _projectAssignmentRepository;
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService;
        private readonly Mock<IAuditService> _auditService;

        public TimeEntries()
        {
            _fixture = new Fixture();
            _timeEntryRepository = new Mock<ITimeEntryRepositoryAsync>();
            _projectAssignmentRepository = new Mock<IProjectAssignmentRepositoryAsync>();
            _authenticatedUserService = new Mock<IAuthenticatedUserService>();
            _auditService = new Mock<IAuditService>();
            _auditService
                .Setup(a => a.WriteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("user-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Employee");
        }

        [Fact]
        public async Task CreateTimeEntry_WhenUserIsNotAssigned_ShouldThrowApiException()
        {
            _projectAssignmentRepository
                .Setup(r => r.IsUserAssignedToProjectAsync("user-1", It.IsAny<int>()))
                .ReturnsAsync(false);

            var handler = new CreateTimeEntryCommandHandler(
                _timeEntryRepository.Object,
                _projectAssignmentRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            var command = new CreateTimeEntryCommand
            {
                ProjectId = 42,
                EntryDate = DateTime.UtcNow,
                DurationMinutes = 30
            };

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task CreateTimeEntry_WhenOverlapping_ShouldThrowApiException()
        {
            _projectAssignmentRepository
                .Setup(r => r.IsUserAssignedToProjectAsync("user-1", It.IsAny<int>()))
                .ReturnsAsync(true);

            _timeEntryRepository
                .Setup(r => r.HasOverlappingEntryAsync("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(true);

            var handler = new CreateTimeEntryCommandHandler(
                _timeEntryRepository.Object,
                _projectAssignmentRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            var start = DateTime.UtcNow.Date.AddHours(9);
            var end = start.AddMinutes(30);
            var command = new CreateTimeEntryCommand
            {
                ProjectId = 1,
                EntryDate = start.Date,
                StartTimeUtc = start,
                EndTimeUtc = end
            };

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task CreateTimeEntry_WithManualDuration_ShouldPersistAndReturnId()
        {
            _projectAssignmentRepository
                .Setup(r => r.IsUserAssignedToProjectAsync("user-1", It.IsAny<int>()))
                .ReturnsAsync(true);

            _timeEntryRepository
                .Setup(r => r.AddAsync(It.IsAny<TimeEntry>()))
                .ReturnsAsync((TimeEntry entry) =>
                {
                    entry.Id = 101;
                    return entry;
                });

            var handler = new CreateTimeEntryCommandHandler(
                _timeEntryRepository.Object,
                _projectAssignmentRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            var command = new CreateTimeEntryCommand
            {
                ProjectId = 7,
                EntryDate = DateTime.UtcNow,
                DurationMinutes = 50,
                Description = _fixture.Create<string>(),
                IsBillable = true
            };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(101, result);
            _timeEntryRepository.Verify(r => r.AddAsync(It.Is<TimeEntry>(te =>
                te.UserId == "user-1" &&
                te.ProjectId == command.ProjectId &&
                te.DurationMinutes == 50 &&
                te.SourceType == "Manual")), Times.Once);
            _auditService.Verify(a => a.WriteAsync(
                "TimeEntry",
                "101",
                "Create",
                It.IsAny<string>(),
                null,
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetAllTimeEntries_WhenFilterAndSortProvided_ShouldPassArgumentsAndBuildPage()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("employee-1");

            var from = DateTime.UtcNow.Date.AddDays(-5);
            var to = DateTime.UtcNow.Date;
            var query = new GetAllTimeEntriesQuery
            {
                PageNumber = 1,
                PageSize = 20,
                ProjectId = 100,
                From = from,
                To = to,
                IsBillable = true,
                SortBy = "durationMinutes",
                SortDir = "asc"
            };

            var mappedFilter = new GetAllTimeEntriesParameter
            {
                PageNumber = 1,
                PageSize = 20,
                ProjectId = 100,
                From = from,
                To = to,
                IsBillable = true,
                SortBy = "durationMinutes",
                SortDir = "asc"
            };

            var repositoryEntries = new List<TimeEntry>
            {
                new TimeEntry { Id = 10, UserId = "employee-1", ProjectId = 100, DurationMinutes = 30, EntryDate = from, IsBillable = true, SourceType = "Manual" }
            };

            var mappedEntries = new List<GetAllTimeEntriesViewModel>
            {
                new GetAllTimeEntriesViewModel { Id = 10, UserId = "employee-1", ProjectId = 100, DurationMinutes = 30, IsBillable = true, SourceType = "Manual" }
            };

            mapper.Setup(m => m.Map<GetAllTimeEntriesParameter>(query)).Returns(mappedFilter);
            mapper.Setup(m => m.Map<List<GetAllTimeEntriesViewModel>>(repositoryEntries)).Returns(mappedEntries);

            _timeEntryRepository
                .Setup(r => r.GetPagedByUserIdAsync("employee-1", 1, 20, 100, from, to, true, "durationMinutes", "asc"))
                .ReturnsAsync(repositoryEntries);

            _timeEntryRepository
                .Setup(r => r.CountByUserIdAsync("employee-1", 100, from, to, true))
                .ReturnsAsync(1);

            var handler = new GetAllTimeEntriesQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(1, result.Page);
            Assert.Equal(20, result.PageSize);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);

            _timeEntryRepository.Verify(r => r.GetPagedByUserIdAsync(
                "employee-1", 1, 20, 100, from, to, true, "durationMinutes", "asc"), Times.Once);
            _timeEntryRepository.Verify(r => r.CountByUserIdAsync(
                "employee-1", 100, from, to, true), Times.Once);
        }

        [Fact]
        public async Task GetTeamTimeEntries_WhenManagerRequests_ShouldPassScopeFiltersAndReturnMappedPage()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            var repositoryEntries = new List<TimeEntry>
            {
                new TimeEntry { Id = 10, UserId = "employee-1", ProjectId = 100, DurationMinutes = 30, EntryDate = DateTime.UtcNow.Date, SourceType = "Manual" },
                new TimeEntry { Id = 11, UserId = "employee-2", ProjectId = 100, DurationMinutes = 45, EntryDate = DateTime.UtcNow.Date, SourceType = "Manual" }
            };

            var mappedEntries = new List<GetAllTimeEntriesViewModel>
            {
                new GetAllTimeEntriesViewModel { Id = 10, UserId = "employee-1", ProjectId = 100, DurationMinutes = 30, SourceType = "Manual" },
                new GetAllTimeEntriesViewModel { Id = 11, UserId = "employee-2", ProjectId = 100, DurationMinutes = 45, SourceType = "Manual" }
            };

            var from = DateTime.UtcNow.Date.AddDays(-7);
            var to = DateTime.UtcNow.Date;

            _timeEntryRepository
                .Setup(r => r.GetPagedByManagedProjectsAsync("manager-1", 2, 5, 100, "employee-1", from, to, null, null, null))
                .ReturnsAsync(repositoryEntries);

            _timeEntryRepository
                .Setup(r => r.CountByManagedProjectsAsync("manager-1", 100, "employee-1", from, to, null))
                .ReturnsAsync(7);

            mapper
                .Setup(m => m.Map<List<GetAllTimeEntriesViewModel>>(repositoryEntries))
                .Returns(mappedEntries);

            var handler = new GetTeamTimeEntriesQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object,
                _auditService.Object);

            var query = new GetTeamTimeEntriesQuery
            {
                PageNumber = 2,
                PageSize = 5,
                ProjectId = 100,
                EmployeeUserId = "employee-1",
                From = from,
                To = to
            };

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(7, result.TotalCount);
            Assert.Equal(2, result.TotalPages);
            Assert.True(result.HasPrevious);
            Assert.False(result.HasNext);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("employee-1", result.Items[0].UserId);

            _timeEntryRepository.Verify(r => r.GetPagedByManagedProjectsAsync(
                "manager-1",
                2,
                5,
                100,
                "employee-1",
                from,
                to,
                null,
                null,
                null), Times.Once);
            _timeEntryRepository.Verify(r => r.CountByManagedProjectsAsync(
                "manager-1",
                100,
                "employee-1",
                from,
                to,
                null), Times.Once);
            _auditService.Verify(a => a.WriteAsync(
                "TeamTimeEntries",
                "manager-1",
                "Read",
                It.IsAny<string>(),
                null,
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetTeamTimeEntries_WhenNoTeamEntries_ShouldReturnEmptyPage()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            _timeEntryRepository
                .Setup(r => r.GetPagedByManagedProjectsAsync("manager-1", 1, 10, null, null, null, null, null, null, null))
                .ReturnsAsync(new List<TimeEntry>());

            _timeEntryRepository
                .Setup(r => r.CountByManagedProjectsAsync("manager-1", null, null, null, null, null))
                .ReturnsAsync(0);

            mapper
                .Setup(m => m.Map<List<GetAllTimeEntriesViewModel>>(It.IsAny<List<TimeEntry>>()))
                .Returns(new List<GetAllTimeEntriesViewModel>());

            var handler = new GetTeamTimeEntriesQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object);

            var result = await handler.Handle(new GetTeamTimeEntriesQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(0, result.TotalCount);
            Assert.Equal(0, result.TotalPages);
            Assert.False(result.HasNext);
            Assert.False(result.HasPrevious);
        }

        [Fact]
        public async Task GetTeamTimeEntries_WhenEmployeeRole_ShouldThrowApiException()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("employee-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Employee");

            var handler = new GetTeamTimeEntriesQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object);

            await Assert.ThrowsAsync<ApiException>(() =>
                handler.Handle(new GetTeamTimeEntriesQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None));

            _timeEntryRepository.Verify(r => r.GetPagedByManagedProjectsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetTeamProjectSummary_WhenManagerRequests_ShouldReturnMappedProjectTotals()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            var summary = new List<TeamProjectSummaryDto>
            {
                new TeamProjectSummaryDto { ProjectId = 100, TotalDurationMinutes = 180, EntryCount = 3, EmployeeCount = 2 }
            };

            var mapped = new List<GetTeamProjectSummaryViewModel>
            {
                new GetTeamProjectSummaryViewModel { ProjectId = 100, TotalDurationMinutes = 180, EntryCount = 3, EmployeeCount = 2 }
            };

            var from = DateTime.UtcNow.Date.AddDays(-14);
            var to = DateTime.UtcNow.Date;

            _timeEntryRepository
                .Setup(r => r.GetProjectSummaryByManagedProjectsAsync("manager-1", from, to, "employee-1"))
                .ReturnsAsync(summary);

            mapper
                .Setup(m => m.Map<List<GetTeamProjectSummaryViewModel>>(summary))
                .Returns(mapped);

            var handler = new GetTeamProjectSummaryQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object);

            var result = await handler.Handle(new GetTeamProjectSummaryQuery
            {
                From = from,
                To = to,
                EmployeeUserId = "employee-1"
            }, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(100, result[0].ProjectId);
            Assert.Equal(180, result[0].TotalDurationMinutes);

            _timeEntryRepository.Verify(r => r.GetProjectSummaryByManagedProjectsAsync("manager-1", from, to, "employee-1"), Times.Once);
        }

        [Fact]
        public async Task GetTeamPeriodSummary_WhenManagerRequests_ShouldReturnMappedPeriodTotals()
        {
            var mapper = new Mock<IMapper>();
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            var day = DateTime.UtcNow.Date;
            var summary = new List<TeamPeriodSummaryDto>
            {
                new TeamPeriodSummaryDto { EntryDate = day, TotalDurationMinutes = 240, EntryCount = 4, ProjectCount = 1, EmployeeCount = 2 }
            };

            var mapped = new List<GetTeamPeriodSummaryViewModel>
            {
                new GetTeamPeriodSummaryViewModel { EntryDate = day, TotalDurationMinutes = 240, EntryCount = 4, ProjectCount = 1, EmployeeCount = 2 }
            };

            var from = day.AddDays(-7);
            var to = day;

            _timeEntryRepository
                .Setup(r => r.GetPeriodSummaryByManagedProjectsAsync("manager-1", from, to, 100, "employee-1"))
                .ReturnsAsync(summary);

            mapper
                .Setup(m => m.Map<List<GetTeamPeriodSummaryViewModel>>(summary))
                .Returns(mapped);

            var handler = new GetTeamPeriodSummaryQueryHandler(
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                mapper.Object);

            var result = await handler.Handle(new GetTeamPeriodSummaryQuery
            {
                From = from,
                To = to,
                ProjectId = 100,
                EmployeeUserId = "employee-1"
            }, CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(day, result[0].EntryDate);
            Assert.Equal(240, result[0].TotalDurationMinutes);

            _timeEntryRepository.Verify(r => r.GetPeriodSummaryByManagedProjectsAsync("manager-1", from, to, 100, "employee-1"), Times.Once);
        }

    }
}
