using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
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
        private readonly IAuditService _auditService;

        public StopTimerCommandHandler(
            IRunningTimerRepositoryAsync runningTimerRepository,
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _runningTimerRepository = runningTimerRepository;
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
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

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "TimeEntry",
                    entry.Id.ToString(),
                    "CreateFromTimer",
                    "Time entry created by stopping active timer.",
                    JsonSerializer.Serialize(new
                    {
                        RunningTimerId = activeTimer.Id,
                        activeTimer.UserId,
                        activeTimer.ProjectId,
                        activeTimer.StartedAtUtc,
                        EndedAtUtc = endedAtUtc
                    }),
                    JsonSerializer.Serialize(new
                    {
                        entry.UserId,
                        entry.ProjectId,
                        entry.EntryDate,
                        entry.StartTimeUtc,
                        entry.EndTimeUtc,
                        entry.DurationMinutes,
                        entry.IsBillable,
                        entry.SourceType
                    }));
            }

            return entry.Id;
        }
    }
}
