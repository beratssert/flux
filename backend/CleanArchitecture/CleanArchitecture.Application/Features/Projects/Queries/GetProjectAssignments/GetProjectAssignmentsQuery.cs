using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetProjectAssignments
{
    public class GetProjectAssignmentsQuery : IRequest<List<ProjectAssignmentItemViewModel>>
    {
        public int ProjectId { get; set; }
    }

    public class GetProjectAssignmentsQueryHandler : IRequestHandler<GetProjectAssignmentsQuery, List<ProjectAssignmentItemViewModel>>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IProjectAssignmentRepositoryAsync _assignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetProjectAssignmentsQueryHandler(
            IProjectRepositoryAsync projectRepository,
            IProjectAssignmentRepositoryAsync assignmentRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _projectRepository = projectRepository;
            _assignmentRepository = assignmentRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<List<ProjectAssignmentItemViewModel>> Handle(GetProjectAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var currentUserId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var project = await _projectRepository.GetByIdAsync(request.ProjectId, tracked: false);
            if (project == null)
            {
                throw new NotFoundException("Project not found.");
            }

            var allowed = false;
            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                allowed = true;
            }
            else if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                allowed = string.Equals(project.ManagerUserId, currentUserId, StringComparison.Ordinal);
            }

            if (!allowed)
            {
                throw new NotFoundException("Project not found.");
            }

            var rows = await _assignmentRepository.GetActiveByProjectIdAsync(request.ProjectId);
            return rows.Select(pa => new ProjectAssignmentItemViewModel
            {
                UserId = pa.UserId,
                AssignedAtUtc = pa.AssignedAtUtc,
                IsActive = pa.IsActive
            }).ToList();
        }
    }
}
