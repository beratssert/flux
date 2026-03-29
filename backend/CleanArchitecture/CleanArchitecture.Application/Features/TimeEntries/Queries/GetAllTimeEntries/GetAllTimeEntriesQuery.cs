using AutoMapper;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Wrappers;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetAllTimeEntries
{
    public class GetAllTimeEntriesQuery : IRequest<PagedResponse<GetAllTimeEntriesViewModel>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int? ProjectId { get; set; }
        public System.DateTime? From { get; set; }
        public System.DateTime? To { get; set; }
        public bool? IsBillable { get; set; }
        public string SortBy { get; set; }
        public string SortDir { get; set; }
    }

    public class GetAllTimeEntriesQueryHandler : IRequestHandler<GetAllTimeEntriesQuery, PagedResponse<GetAllTimeEntriesViewModel>>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetAllTimeEntriesQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<PagedResponse<GetAllTimeEntriesViewModel>> Handle(GetAllTimeEntriesQuery request, CancellationToken cancellationToken)
        {
            var validFilter = _mapper.Map<GetAllTimeEntriesParameter>(request);
            var timeEntries = await _timeEntryRepository.GetPagedByUserIdAsync(
                _authenticatedUserService.UserId,
                validFilter.PageNumber,
                validFilter.PageSize,
                validFilter.ProjectId,
                validFilter.From,
                validFilter.To,
                validFilter.IsBillable,
                validFilter.SortBy,
                validFilter.SortDir);
            var totalCount = await _timeEntryRepository.CountByUserIdAsync(
                _authenticatedUserService.UserId,
                validFilter.ProjectId,
                validFilter.From,
                validFilter.To,
                validFilter.IsBillable);
            var vm = _mapper.Map<List<GetAllTimeEntriesViewModel>>(timeEntries);
            return new PagedResponse<GetAllTimeEntriesViewModel>(vm, validFilter.PageNumber, validFilter.PageSize, totalCount);
        }
    }
}
