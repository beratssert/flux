using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetAllTimeEntries;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Wrappers;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamTimeEntries
{
    public class GetTeamTimeEntriesQuery : IRequest<PagedResponse<GetAllTimeEntriesViewModel>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int? ProjectId { get; set; }
        public string EmployeeUserId { get; set; }
        public System.DateTime? From { get; set; }
        public System.DateTime? To { get; set; }
        public bool? IsBillable { get; set; }
        public string SortBy { get; set; }
        public string SortDir { get; set; }
    }

    public class GetTeamTimeEntriesQueryHandler : IRequestHandler<GetTeamTimeEntriesQuery, PagedResponse<GetAllTimeEntriesViewModel>>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetTeamTimeEntriesQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<PagedResponse<GetAllTimeEntriesViewModel>> Handle(GetTeamTimeEntriesQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var isManager = string.Equals(role, Roles.Manager.ToString(), System.StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(role, Roles.Admin.ToString(), System.StringComparison.OrdinalIgnoreCase);
            if (!isManager && !isAdmin)
            {
                throw new ApiException("Only manager or admin can access team time entries.");
            }

            var entries = await _timeEntryRepository.GetPagedByManagedProjectsAsync(
                _authenticatedUserService.UserId,
                request.PageNumber,
                request.PageSize,
                request.ProjectId,
                request.EmployeeUserId,
                request.From,
                request.To,
                request.IsBillable,
                request.SortBy,
                request.SortDir);
            var totalCount = await _timeEntryRepository.CountByManagedProjectsAsync(
                _authenticatedUserService.UserId,
                request.ProjectId,
                request.EmployeeUserId,
                request.From,
                request.To,
                request.IsBillable);

            var vm = _mapper.Map<List<GetAllTimeEntriesViewModel>>(entries);
            return new PagedResponse<GetAllTimeEntriesViewModel>(vm, request.PageNumber, request.PageSize, totalCount);
        }
    }
}
