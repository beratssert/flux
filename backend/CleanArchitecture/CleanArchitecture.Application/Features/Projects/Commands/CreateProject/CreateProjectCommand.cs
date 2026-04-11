using AutoMapper;
using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.Projects;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Commands.CreateProject
{
    public class CreateProjectCommand : IRequest<ProjectViewModel>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectViewModel>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public CreateProjectCommandHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<ProjectViewModel> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ApiException("Name is required.");
            }

            ValidateDateRange(request.StartDate, request.EndDate);

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var exists = await _projectRepository.CodeExistsAsync(request.Code.Trim(), null);
                if (exists)
                {
                    throw new ConflictException("A project with this code already exists.");
                }
            }

            var project = new Project
            {
                Name = request.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                ManagerUserId = userId,
                Status = ProjectStatuses.Active,
                StartDate = request.StartDate?.Date,
                EndDate = request.EndDate?.Date
            };

            await _projectRepository.AddAsync(project);
            return _mapper.Map<ProjectViewModel>(project);
        }

        private static void ValidateDateRange(DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue && end.Value < start.Value)
            {
                throw new ApiException("End date cannot be before start date.");
            }
        }
    }
}
