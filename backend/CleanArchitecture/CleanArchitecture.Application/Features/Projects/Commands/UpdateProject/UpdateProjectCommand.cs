using AutoMapper;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectCommand : IRequest<ProjectViewModel>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectViewModel>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public UpdateProjectCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<ProjectViewModel> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
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

            if (!string.IsNullOrEmpty(request.Name))
            {
                project.Name = request.Name.Trim();
            }

            if (request.Code != null)
            {
                var trimmed = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
                if (trimmed != null)
                {
                    var exists = await _projectRepository.CodeExistsAsync(trimmed, request.Id);
                    if (exists)
                    {
                        throw new ConflictException("A project with this code already exists.");
                    }
                }

                project.Code = trimmed;
            }

            if (request.Description != null)
            {
                project.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            if (request.StartDate.HasValue)
            {
                project.StartDate = request.StartDate.Value.Date;
            }

            if (request.EndDate.HasValue)
            {
                project.EndDate = request.EndDate.Value.Date;
            }

            var start = project.StartDate;
            var end = project.EndDate;
            if (start.HasValue && end.HasValue && end.Value < start.Value)
            {
                throw new ApiException("End date cannot be before start date.");
            }

            await _projectRepository.UpdateAsync(project);
            var refreshed = await _projectRepository.GetByIdAsync(request.Id, tracked: false);
            return _mapper.Map<ProjectViewModel>(refreshed);
        }
    }
}
