using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Text.Json;
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
        private readonly IAuditService _auditService;

        public GetTeamPeriodSummaryQueryHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IMapper mapper,
            IAuditService auditService = null)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _mapper = mapper;
            _auditService = auditService;
        }

        public async Task<List<GetTeamPeriodSummaryViewModel>> Handle(GetTeamPeriodSummaryQuery request, CancellationToken cancellationToken)
        {
            var role = _authenticatedUserService.Role;
            var isManager = string.Equals(role, Roles.Manager.ToString(), System.StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(role, Roles.Admin.ToString(), System.StringComparison.OrdinalIgnoreCase);
            if (!isManager && !isAdmin)
            {
                throw new ApiException("Only manager or admin can access team period summary.");
            }

            var summary = await _timeEntryRepository.GetPeriodSummaryByManagedProjectsAsync(
                _authenticatedUserService.UserId,
                request.From,
                request.To,
                request.ProjectId,
                request.EmployeeUserId);

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "TeamPeriodSummary",
                    _authenticatedUserService.UserId,
                    "Read",
                    "Manager/admin accessed team period summary.",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        request.From,
                        request.To,
                        request.ProjectId,
                        request.EmployeeUserId,
                        ResultCount = summary.Count
                    }));
            }

            return _mapper.Map<List<GetTeamPeriodSummaryViewModel>>(summary);
        }
    }
}
