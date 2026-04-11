using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetMyProjectAssignments
{
    public class GetMyProjectAssignmentsQuery : IRequest<List<MyProjectAssignmentViewModel>>
    {
    }

    public class GetMyProjectAssignmentsQueryHandler : IRequestHandler<GetMyProjectAssignmentsQuery, List<MyProjectAssignmentViewModel>>
    {
        private readonly IProjectAssignmentRepositoryAsync _assignmentRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetMyProjectAssignmentsQueryHandler(
            IProjectAssignmentRepositoryAsync assignmentRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _assignmentRepository = assignmentRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<List<MyProjectAssignmentViewModel>> Handle(GetMyProjectAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var rows = await _assignmentRepository.GetActiveRowsForUserAsync(userId);
            return rows.Select(r => new MyProjectAssignmentViewModel
            {
                ProjectId = r.ProjectId,
                ProjectName = r.ProjectName,
                ProjectCode = r.ProjectCode,
                ProjectStatus = r.ProjectStatus,
                AssignedAtUtc = r.AssignedAtUtc
            }).ToList();
        }
    }
}
