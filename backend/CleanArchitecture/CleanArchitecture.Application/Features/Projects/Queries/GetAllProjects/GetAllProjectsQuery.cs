using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Wrappers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Projects.Queries.GetAllProjects
{
    public class GetAllProjectsQuery : IRequest<PagedResponse<ProjectViewModel>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string Status { get; set; }
        public string ManagerUserId { get; set; }
        public string Q { get; set; }
    }

    public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, PagedResponse<ProjectViewModel>>
    {
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetAllProjectsQueryHandler(
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<PagedResponse<ProjectViewModel>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var currentUserId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var page = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

            IReadOnlyList<Project> items;
            int total;

            if (string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                (items, total) = await _projectRepository.GetPagedForAdminAsync(
                    page,
                    pageSize,
                    request.Status,
                    request.ManagerUserId,
                    request.Q);
            }
            else if (string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                (items, total) = await _projectRepository.GetPagedForManagerAsync(
                    currentUserId,
                    page,
                    pageSize,
                    request.Status,
                    request.ManagerUserId,
                    request.Q);
            }
            else
            {
                (items, total) = await _projectRepository.GetPagedForEmployeeAsync(
                    currentUserId,
                    page,
                    pageSize,
                    request.Status,
                    request.ManagerUserId,
                    request.Q);
            }

            var vm = items.Select(p => _mapper.Map<ProjectViewModel>(p)).ToList();
            return new PagedResponse<ProjectViewModel>(vm, page, pageSize, total);
        }
    }
}
