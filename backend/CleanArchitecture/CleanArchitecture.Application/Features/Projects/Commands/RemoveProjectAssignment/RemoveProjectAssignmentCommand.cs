using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.RemoveProjectAssignment
{
    public class RemoveProjectAssignmentCommand : IRequest<Unit>
    {
        public int ProjectId { get; set; }
        public string UserId { get; set; }
    }

    public class RemoveProjectAssignmentCommandHandler : IRequestHandler<RemoveProjectAssignmentCommand, Unit>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IProjectAssignmentRepositoryAsync _assignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IAuditService _auditService;

        public RemoveProjectAssignmentCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IProjectAssignmentRepositoryAsync assignmentRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _projectRepository = projectRepository;
            _assignmentRepository = assignmentRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
        }

        public async Task<Unit> Handle(RemoveProjectAssignmentCommand request, CancellationToken cancellationToken)
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

            var assignment = await _assignmentRepository.GetActiveByProjectAndUserAsync(request.ProjectId, request.UserId);
            if (assignment == null)
            {
                throw new NotFoundException("Assignment not found.");
            }

            assignment.IsActive = false;
            assignment.UnassignedAtUtc = DateTime.UtcNow;
            await _assignmentRepository.UpdateAsync(assignment);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "ProjectAssignment",
                    assignment.Id.ToString(),
                    "Unassign",
                    "User unassigned from project.",
                    JsonSerializer.Serialize(new { assignment.UserId, wasActive = true }),
                    JsonSerializer.Serialize(new { assignment.IsActive, assignment.UnassignedAtUtc }));
            }

            return Unit.Value;
        }
    }
}
