using AutoMapper;
using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.UpdateProjectStatus
{
    /// <summary>Request body for <c>PATCH /projects/{id}/status</c>.</summary>
    public class UpdateProjectStatusCommand : IRequest<ProjectViewModel>
    {
        /// <summary>Set from route.</summary>
        public int Id { get; set; }
        /// <summary>Active, Archived, or Closed.</summary>
        public string Status { get; set; }
    }

    public class UpdateProjectStatusCommandHandler : IRequestHandler<UpdateProjectStatusCommand, ProjectViewModel>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public UpdateProjectStatusCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<ProjectViewModel> Handle(UpdateProjectStatusCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Status) || !ProjectStatuses.IsValid(request.Status))
            {
                throw new ApiException("Invalid project status.");
            }

            var project = await _projectRepository.GetByIdAsync(request.Id, tracked: true);
            if (project == null)
            {
                throw new NotFoundException("Project not found.");
            }

            if (!string.Equals(project.ManagerUserId, userId, StringComparison.Ordinal))
            {
                throw new NotFoundException("Project not found.");
            }

            project.Status = ProjectStatuses.Normalize(request.Status);
            await _projectRepository.UpdateAsync(project);
            var refreshed = await _projectRepository.GetByIdAsync(request.Id, tracked: false);
            return _mapper.Map<ProjectViewModel>(refreshed);
        }
    }
}
