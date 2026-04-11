using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.CreateProjectAssignment
{
    public class CreateProjectAssignmentCommand : IRequest<Unit>
    {
        public int ProjectId { get; set; }
        public string UserId { get; set; }
    }

    public class CreateProjectAssignmentCommandHandler : IRequestHandler<CreateProjectAssignmentCommand, Unit>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IProjectAssignmentRepositoryAsync _assignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IUserRolesService _userRolesService;
        private readonly IAuditService _auditService;

        public CreateProjectAssignmentCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IProjectAssignmentRepositoryAsync assignmentRepository,
            IAuthenticatedUserService authenticatedUserService,
            IUserRolesService userRolesService,
            IAuditService auditService = null)
        {
            _projectRepository = projectRepository;
            _assignmentRepository = assignmentRepository;
            _authenticatedUserService = authenticatedUserService;
            _userRolesService = userRolesService;
            _auditService = auditService;
        }

        public async Task<Unit> Handle(CreateProjectAssignmentCommand request, CancellationToken cancellationToken)
        {
            var managerId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(managerId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ApiException("User id is required.");
            }

            var managed = await _projectRepository.IsManagedByAsync(managerId, request.ProjectId);
            if (!managed)
            {
                throw new NotFoundException("Project not found.");
            }

            var targetExists = await _userRolesService.UserExistsAsync(request.UserId);
            if (!targetExists)
            {
                throw new ApiException("User not found.");
            }

            var roles = await _userRolesService.GetRolesAsync(request.UserId);
            if (!roles.Any(r => string.Equals(r, Roles.Employee.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApiException("Only users with the Employee role can be assigned to a project.");
            }

            var duplicate = await _assignmentRepository.HasActiveAssignmentAsync(request.ProjectId, request.UserId);
            if (duplicate)
            {
                throw new ConflictException("User is already actively assigned to this project.");
            }

            var assignment = new ProjectAssignment
            {
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                AssignedAtUtc = DateTime.UtcNow,
                AssignedByUserId = managerId,
                IsActive = true
            };

            await _assignmentRepository.AddAsync(assignment);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "ProjectAssignment",
                    assignment.Id.ToString(),
                    "Create",
                    "User assigned to project.",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        assignment.ProjectId,
                        assignment.UserId,
                        assignment.AssignedByUserId
                    }));
            }

            return Unit.Value;
        }
    }
}
