using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.ReassignProjectManager
{
    public class ReassignProjectManagerCommand : IRequest<ProjectViewModel>
    {
        public int Id { get; set; }
        public string ManagerUserId { get; set; }
    }

    public class ReassignProjectManagerCommandHandler : IRequestHandler<ReassignProjectManagerCommand, ProjectViewModel>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IUserRolesService _userRolesService;
        private readonly IAuditService _auditService;
        private readonly IMapper _mapper;

        public ReassignProjectManagerCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IUserRolesService userRolesService,
            IMapper mapper,
            IAuditService auditService = null)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _userRolesService = userRolesService;
            _mapper = mapper;
            _auditService = auditService;
        }

        public async Task<ProjectViewModel> Handle(ReassignProjectManagerCommand request, CancellationToken cancellationToken)
        {
            var actorId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            if (string.IsNullOrWhiteSpace(request.ManagerUserId))
            {
                throw new ApiException("Manager user id is required.");
            }

            var exists = await _userRolesService.UserExistsAsync(request.ManagerUserId);
            if (!exists)
            {
                throw new ApiException("User not found.");
            }

            var roles = await _userRolesService.GetRolesAsync(request.ManagerUserId);
            if (!roles.Any(r => string.Equals(r, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApiException("New manager must have the Manager role.");
            }

            var project = await _projectRepository.GetByIdAsync(request.Id, tracked: true);
            if (project == null)
            {
                throw new NotFoundException("Project not found.");
            }

            var oldManager = project.ManagerUserId;
            project.ManagerUserId = request.ManagerUserId;
            await _projectRepository.UpdateAsync(project);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "Project",
                    project.Id.ToString(),
                    "ReassignManager",
                    "Project manager reassigned.",
                    JsonSerializer.Serialize(new { managerUserId = oldManager }),
                    JsonSerializer.Serialize(new { managerUserId = project.ManagerUserId, actorUserId = actorId }));
            }

            var refreshed = await _projectRepository.GetByIdAsync(request.Id, tracked: false);
            return _mapper.Map<ProjectViewModel>(refreshed);
        }
    }
}
