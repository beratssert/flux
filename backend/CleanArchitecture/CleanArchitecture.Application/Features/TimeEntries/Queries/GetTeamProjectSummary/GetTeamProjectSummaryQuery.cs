using AutoMapper;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary
{
    public class GetTeamProjectSummaryQuery : IRequest<List<GetTeamProjectSummaryViewModel>>
    {
        public System.DateTime? From { get; set; }
        public System.DateTime? To { get; set; }
        public string EmployeeUserId { get; set; }
    }

    public class GetTeamProjectSummaryQueryHandler : IRequestHandler<GetTeamProjectSummaryQuery, List<GetTeamProjectSummaryViewModel>>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetTeamProjectSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<List<GetTeamProjectSummaryViewModel>> Handle(GetTeamProjectSummaryQuery request, CancellationToken cancellationToken)
        {
            var summary = await _timeEntryRepository.GetProjectSummaryByManagedProjectsAsync(
                _authenticatedUserService.UserId,
                request.From,
                request.To,
                request.EmployeeUserId);

            return _mapper.Map<List<GetTeamProjectSummaryViewModel>>(summary);
        }
    }
}
