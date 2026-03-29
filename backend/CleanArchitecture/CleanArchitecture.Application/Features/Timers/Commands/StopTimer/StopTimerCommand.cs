using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Timers.Commands.StopTimer
{
    public class StopTimerCommand : IRequest<int>
    {
    }

    public class StopTimerCommandHandler : IRequestHandler<StopTimerCommand, int>
    {
        private readonly IRunningTimerRepositoryAsync _runningTimerRepository;
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public StopTimerCommandHandler(
            IRunningTimerRepositoryAsync runningTimerRepository,
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _runningTimerRepository = runningTimerRepository;
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(StopTimerCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var activeTimer = await _runningTimerRepository.GetActiveByUserIdAsync(userId);
            if (activeTimer == null)
            {
                throw new ApiException("No active timer found.");
            }

            var endedAtUtc = DateTime.UtcNow;
            if (endedAtUtc <= activeTimer.StartedAtUtc)
            {
                throw new ApiException("Invalid timer duration.");
            }

            var durationMinutes = (int)Math.Ceiling((endedAtUtc - activeTimer.StartedAtUtc).TotalMinutes);
            if (durationMinutes <= 0)
            {
                throw new ApiException("Timer duration must be greater than zero.");
            }

            var hasOverlap = await _timeEntryRepository.HasOverlappingEntryAsync(userId, activeTimer.StartedAtUtc, endedAtUtc);
            if (hasOverlap)
            {
                throw new ApiException("Timer range overlaps with another time entry.");
            }

            var entry = new TimeEntry
            {
                UserId = userId,
                ProjectId = activeTimer.ProjectId,
                EntryDate = activeTimer.StartedAtUtc.Date,
                StartTimeUtc = activeTimer.StartedAtUtc,
                EndTimeUtc = endedAtUtc,
                DurationMinutes = durationMinutes,
                Description = activeTimer.Description,
                IsBillable = activeTimer.IsBillable,
                SourceType = "Timer",
                IsLocked = false
            };

            await _timeEntryRepository.AddAsync(entry);
            await _runningTimerRepository.DeleteAsync(activeTimer);

            return entry.Id;
        }
    }
}
