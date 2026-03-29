using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Timers.Commands.StopTimer;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.UnitTests
{
    public class Timers
    {
        private readonly Mock<IRunningTimerRepositoryAsync> _runningTimerRepository;
        private readonly Mock<ITimeEntryRepositoryAsync> _timeEntryRepository;
        private readonly Mock<IAuthenticatedUserService> _authenticatedUserService;
        private readonly Mock<IAuditService> _auditService;

        public Timers()
        {
            _runningTimerRepository = new Mock<IRunningTimerRepositoryAsync>();
            _timeEntryRepository = new Mock<ITimeEntryRepositoryAsync>();
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
        }

        [Fact]
        public async Task StopTimer_WhenNoActiveTimer_ShouldThrowApiException()
        {
            _runningTimerRepository
                .Setup(r => r.GetActiveByUserIdAsync("user-1"))
                .ReturnsAsync((RunningTimer)null);

            var handler = new StopTimerCommandHandler(
                _runningTimerRepository.Object,
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(new StopTimerCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task StopTimer_WhenOverlapExists_ShouldThrowApiException()
        {
            var startedAt = DateTime.UtcNow.AddMinutes(-30);
            _runningTimerRepository
                .Setup(r => r.GetActiveByUserIdAsync("user-1"))
                .ReturnsAsync(new RunningTimer
                {
                    Id = 5,
                    UserId = "user-1",
                    ProjectId = 3,
                    StartedAtUtc = startedAt,
                    Description = "x",
                    IsBillable = false
                });

            _timeEntryRepository
                .Setup(r => r.HasOverlappingEntryAsync("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(true);

            var handler = new StopTimerCommandHandler(
                _runningTimerRepository.Object,
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            await Assert.ThrowsAsync<ApiException>(() => handler.Handle(new StopTimerCommand(), CancellationToken.None));
        }

        [Fact]
        public async Task StopTimer_WhenValid_ShouldCreateTimeEntryAndDeleteTimer()
        {
            var timer = new RunningTimer
            {
                Id = 9,
                UserId = "user-1",
                ProjectId = 2,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-40),
                Description = "Focus",
                IsBillable = true
            };

            _runningTimerRepository
                .Setup(r => r.GetActiveByUserIdAsync("user-1"))
                .ReturnsAsync(timer);

            _timeEntryRepository
                .Setup(r => r.HasOverlappingEntryAsync("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(false);

            _timeEntryRepository
                .Setup(r => r.AddAsync(It.IsAny<TimeEntry>()))
                .ReturnsAsync((TimeEntry entry) =>
                {
                    entry.Id = 77;
                    return entry;
                });

            var handler = new StopTimerCommandHandler(
                _runningTimerRepository.Object,
                _timeEntryRepository.Object,
                _authenticatedUserService.Object,
                _auditService.Object);

            var result = await handler.Handle(new StopTimerCommand(), CancellationToken.None);

            Assert.Equal(77, result);
            _timeEntryRepository.Verify(r => r.AddAsync(It.Is<TimeEntry>(te =>
                te.UserId == "user-1" &&
                te.ProjectId == timer.ProjectId &&
                te.SourceType == "Timer" &&
                te.DurationMinutes > 0)), Times.Once);
            _runningTimerRepository.Verify(r => r.DeleteAsync(timer), Times.Once);
            _auditService.Verify(a => a.WriteAsync(
                "TimeEntry",
                "77",
                "CreateFromTimer",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task StopTimer_WhenElapsedIsLessThanOneMinute_ShouldPersistOneMinute()
        {
            var timer = new RunningTimer
            {
                Id = 10,
                UserId = "user-1",
                ProjectId = 2,
                StartedAtUtc = DateTime.UtcNow.AddSeconds(-5),
                Description = "Quick",
                IsBillable = false
            };

            TimeEntry capturedTimeEntry = null;

            _runningTimerRepository
                .Setup(r => r.GetActiveByUserIdAsync("user-1"))
                .ReturnsAsync(timer);

            _timeEntryRepository
                .Setup(r => r.HasOverlappingEntryAsync("user-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
                .ReturnsAsync(false);

            _timeEntryRepository
                .Setup(r => r.AddAsync(It.IsAny<TimeEntry>()))
                .ReturnsAsync((TimeEntry entry) =>
                {
                    capturedTimeEntry = entry;
                    entry.Id = 90;
                    return entry;
                });

            var handler = new StopTimerCommandHandler(
                _runningTimerRepository.Object,
                _timeEntryRepository.Object,
                _authenticatedUserService.Object);

            var result = await handler.Handle(new StopTimerCommand(), CancellationToken.None);

            Assert.Equal(90, result);
            Assert.NotNull(capturedTimeEntry);
            Assert.Equal(1, capturedTimeEntry.DurationMinutes);
        }
    }
}
