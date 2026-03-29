using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Commands.UpdateTimeEntry
{
    public class UpdateTimeEntryCommand : IRequest<int>
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public int? DurationMinutes { get; set; }
        public string Description { get; set; }
        public bool IsBillable { get; set; }
    }

    public class UpdateTimeEntryCommandHandler : IRequestHandler<UpdateTimeEntryCommand, int>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IProjectAssignmentRepositoryAsync _projectAssignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public UpdateTimeEntryCommandHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IProjectAssignmentRepositoryAsync projectAssignmentRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _timeEntryRepository = timeEntryRepository;
            _projectAssignmentRepository = projectAssignmentRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(UpdateTimeEntryCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(request.Id, userId);
            if (entry == null)
            {
                throw new ApiException("Time entry not found.");
            }

            if (entry.IsLocked)
            {
                throw new ApiException("Locked entries cannot be updated.");
            }

            var isAssigned = await _projectAssignmentRepository.IsUserAssignedToProjectAsync(userId, request.ProjectId);
            if (!isAssigned)
            {
                throw new ApiException("User is not assigned to this project.");
            }

            var durationMinutes = ResolveDuration(request.StartTimeUtc, request.EndTimeUtc, request.DurationMinutes);

            if (request.StartTimeUtc.HasValue && request.EndTimeUtc.HasValue)
            {
                var hasOverlap = await _timeEntryRepository.HasOverlappingEntryAsync(userId, request.StartTimeUtc.Value, request.EndTimeUtc.Value, request.Id);
                if (hasOverlap)
                {
                    throw new ApiException("Time entry overlaps with another record.");
                }
            }

            entry.ProjectId = request.ProjectId;
            entry.EntryDate = request.EntryDate.Date;
            entry.StartTimeUtc = request.StartTimeUtc;
            entry.EndTimeUtc = request.EndTimeUtc;
            entry.DurationMinutes = durationMinutes;
            entry.Description = request.Description;
            entry.IsBillable = request.IsBillable;

            await _timeEntryRepository.UpdateAsync(entry);
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
