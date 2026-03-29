using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Reports.Queries.GetManagerTeamTimeSummary;
using CleanArchitecture.Core.Features.Reports.Queries.GetMyTimeSummary;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class Reports
    {
        private readonly Mock<ITimeEntryRepositoryAsync> _timeEntryRepository;
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService;

        public Reports()
        {
            _timeEntryRepository = new Mock<ITimeEntryRepositoryAsync>();
            _authenticatedUserService = new Mock<IAuthenticatedUserService>();
        }

        [Fact]
        public async Task GetMyTimeSummary_WhenGroupedByWeek_ShouldAggregateMinutes()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("employee-1");
            _timeEntryRepository
                .Setup(r => r.GetSummaryRowsByUserAsync("employee-1", null, null))
                .ReturnsAsync(new List<TimeSummaryRowDto>
                {
                    new TimeSummaryRowDto { UserId = "employee-1", ProjectId = 1, EntryDate = new DateTime(2026, 3, 23), DurationMinutes = 60 },
                    new TimeSummaryRowDto { UserId = "employee-1", ProjectId = 2, EntryDate = new DateTime(2026, 3, 24), DurationMinutes = 30 }
                });

            var handler = new GetMyTimeSummaryQueryHandler(_timeEntryRepository.Object, _authenticatedUserService.Object);
            var result = await handler.Handle(new GetMyTimeSummaryQuery { GroupBy = "week" }, CancellationToken.None);

            Assert.Equal(90, result.TotalMinutes);
            Assert.Single(result.Groups);
            Assert.Equal(90, result.Groups[0].Minutes);
            Assert.Contains("W", result.Groups[0].Key);
        }

        [Fact]
        public async Task GetManagerTeamTimeSummary_WhenManagerRole_ShouldUseManagedProjectsScope()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            _timeEntryRepository
                .Setup(r => r.GetSummaryRowsByManagedProjectsAsync("manager-1", 1, "employee-1", null, null))
                .ReturnsAsync(new List<TimeSummaryRowDto>
                {
                    new TimeSummaryRowDto { UserId = "employee-1", ProjectId = 1, EntryDate = new DateTime(2026, 3, 25), DurationMinutes = 120 }
                });

            var handler = new GetManagerTeamTimeSummaryQueryHandler(_timeEntryRepository.Object, _authenticatedUserService.Object);
            var result = await handler.Handle(new GetManagerTeamTimeSummaryQuery
            {
                ProjectId = 1,
                UserId = "employee-1",
                GroupBy = "user"
            }, CancellationToken.None);

            Assert.Equal(120, result.TotalMinutes);
            Assert.Single(result.Groups);
            Assert.Equal("employee-1", result.Groups[0].Key);

            _timeEntryRepository.Verify(r => r.GetSummaryRowsByManagedProjectsAsync("manager-1", 1, "employee-1", null, null), Times.Once);
            _timeEntryRepository.Verify(r => r.GetSummaryRowsAllAsync(It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task GetManagerTeamTimeSummary_WhenEmployeeRole_ShouldThrow()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("employee-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Employee");

            var handler = new GetManagerTeamTimeSummaryQueryHandler(_timeEntryRepository.Object, _authenticatedUserService.Object);

            await Assert.ThrowsAsync<ApiException>(() =>
                handler.Handle(new GetManagerTeamTimeSummaryQuery { GroupBy = "project" }, CancellationToken.None));
        }

        [Fact]
        public async Task GetManagerTeamTimeSummary_WhenInvalidGroupBy_ShouldThrow()
        {
            _authenticatedUserService.SetupGet(a => a.UserId).Returns("manager-1");
            _authenticatedUserService.SetupGet(a => a.Role).Returns("Manager");

            var handler = new GetManagerTeamTimeSummaryQueryHandler(_timeEntryRepository.Object, _authenticatedUserService.Object);

            await Assert.ThrowsAsync<ApiException>(() =>
                handler.Handle(new GetManagerTeamTimeSummaryQuery { GroupBy = "month" }, CancellationToken.None));
        }
    }
}
