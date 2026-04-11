using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetProjectById
{
    public class GetProjectByIdQuery : IRequest<ProjectViewModel>
    {
        public int Id { get; set; }
    }

    public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectViewModel>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetProjectByIdQueryHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<ProjectViewModel> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var currentUserId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var project = await _projectRepository.GetByIdAsync(request.Id, tracked: false);
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
            else if (string.Equals(role, Roles.Employee.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                allowed = await _projectRepository.CanEmployeeViewAsync(currentUserId, request.Id);
            }

            if (!allowed)
            {
                throw new NotFoundException("Project not found.");
            }

            return _mapper.Map<ProjectViewModel>(project);
        }
    }
}
