using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Timers.Commands.StartTimer
{
    public class StartTimerCommand : IRequest<int>
    {
        public int ProjectId { get; set; }
        public string Description { get; set; }
        public bool IsBillable { get; set; }
    }

    public class StartTimerCommandHandler : IRequestHandler<StartTimerCommand, int>
    {
        private readonly IRunningTimerRepositoryAsync _runningTimerRepository;
        private readonly IProjectAssignmentRepositoryAsync _projectAssignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public StartTimerCommandHandler(
            IRunningTimerRepositoryAsync runningTimerRepository,
            IProjectAssignmentRepositoryAsync projectAssignmentRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _runningTimerRepository = runningTimerRepository;
            _projectAssignmentRepository = projectAssignmentRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(StartTimerCommand request, CancellationToken cancellationToken)
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

            var activeTimer = await _runningTimerRepository.GetActiveByUserIdAsync(userId);
            if (activeTimer != null)
            {
                throw new ApiException("An active timer already exists.");
            }

            var timer = new RunningTimer
            {
                UserId = userId,
                ProjectId = request.ProjectId,
                StartedAtUtc = DateTime.UtcNow,
                Description = request.Description,
                IsBillable = request.IsBillable
            };

            await _runningTimerRepository.AddAsync(timer);
            return timer.Id;
        }
    }
}
