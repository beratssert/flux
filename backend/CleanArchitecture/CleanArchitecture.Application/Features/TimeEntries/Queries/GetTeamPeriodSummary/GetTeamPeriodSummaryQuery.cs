using AutoMapper;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamPeriodSummary
{
    public class GetTeamPeriodSummaryQuery : IRequest<List<GetTeamPeriodSummaryViewModel>>
    {
        public System.DateTime? From { get; set; }
        public System.DateTime? To { get; set; }
        public int? ProjectId { get; set; }
        public string EmployeeUserId { get; set; }
    }

    public class GetTeamPeriodSummaryQueryHandler : IRequestHandler<GetTeamPeriodSummaryQuery, List<GetTeamPeriodSummaryViewModel>>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IMapper _mapper;

        public GetTeamPeriodSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
        }

        public async Task<List<GetTeamPeriodSummaryViewModel>> Handle(GetTeamPeriodSummaryQuery request, CancellationToken cancellationToken)
        {
            var summary = await _timeEntryRepository.GetPeriodSummaryByManagedProjectsAsync(
                _authenticatedUserService.UserId,
                request.From,
                request.To,
                request.ProjectId,
                request.EmployeeUserId);

            return _mapper.Map<List<GetTeamPeriodSummaryViewModel>>(summary);
        }
    }
}
