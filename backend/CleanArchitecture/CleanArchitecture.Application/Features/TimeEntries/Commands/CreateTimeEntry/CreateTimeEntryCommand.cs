using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Commands.CreateTimeEntry
{
    public class CreateTimeEntryCommand : IRequest<int>
    {
        public int ProjectId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public int? DurationMinutes { get; set; }
        public string Description { get; set; }
        public bool IsBillable { get; set; }
    }

    public class CreateTimeEntryCommandHandler : IRequestHandler<CreateTimeEntryCommand, int>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IProjectAssignmentRepositoryAsync _projectAssignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public CreateTimeEntryCommandHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IProjectAssignmentRepositoryAsync projectAssignmentRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _timeEntryRepository = timeEntryRepository;
            _projectAssignmentRepository = projectAssignmentRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var isAssigned = await _projectAssignmentRepository.IsUserAssignedToProjectAsync(userId, request.ProjectId);
            if (!isAssigned)
            {
                throw new ApiException("User is not assigned to this project.");
            }

            var durationMinutes = ResolveDuration(request.StartTimeUtc, request.EndTimeUtc, request.DurationMinutes);

            if (request.StartTimeUtc.HasValue && request.EndTimeUtc.HasValue)
            {
                var hasOverlap = await _timeEntryRepository.HasOverlappingEntryAsync(userId, request.StartTimeUtc.Value, request.EndTimeUtc.Value);
                if (hasOverlap)
                {
                    throw new ApiException("Time entry overlaps with another record.");
                }
            }

            var entry = new TimeEntry
            {
                UserId = userId,
                ProjectId = request.ProjectId,
                EntryDate = request.EntryDate.Date,
                StartTimeUtc = request.StartTimeUtc,
                EndTimeUtc = request.EndTimeUtc,
                DurationMinutes = durationMinutes,
                Description = request.Description,
                IsBillable = request.IsBillable,
                SourceType = "Manual",
                IsLocked = false
            };

            await _timeEntryRepository.AddAsync(entry);
            return entry.Id;
        }

        private static int ResolveDuration(DateTime? startTimeUtc, DateTime? endTimeUtc, int? durationMinutes)
        {
            if (startTimeUtc.HasValue || endTimeUtc.HasValue)
            {
                if (!startTimeUtc.HasValue || !endTimeUtc.HasValue)
                {
                    throw new ApiException("Start and end times must be provided together.");
                }

                if (endTimeUtc.Value <= startTimeUtc.Value)
                {
                    throw new ApiException("End time must be after start time.");
                }

                var computed = (int)(endTimeUtc.Value - startTimeUtc.Value).TotalMinutes;
                if (computed <= 0)
                {
                    throw new ApiException("Duration must be greater than zero.");
                }

                return computed;
            }

            if (!durationMinutes.HasValue || durationMinutes.Value <= 0)
            {
                throw new ApiException("Duration must be greater than zero.");
            }

            return durationMinutes.Value;
        }
    }
}
