using AutoMapper;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.CalendarEvents;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Core.Wrappers;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.CalendarEvents.Queries.GetCalendarEvents
{
    public class GetCalendarEventsQuery : IRequest<PagedResponse<CalendarEventViewModel>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? ProjectId { get; set; }
        public string VisibilityType { get; set; }
        public string UserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GetCalendarEventsQueryHandler : IRequestHandler<GetCalendarEventsQuery, PagedResponse<CalendarEventViewModel>>
    {
        private readonly ICalendarEventRepositoryAsync _calendarRepository;
        private readonly IAuthenticatedUserService _auth;
        private readonly IMapper _mapper;

        public GetCalendarEventsQueryHandler(
            ICalendarEventRepositoryAsync calendarRepository,
            IAuthenticatedUserService auth,
            IMapper mapper)
        {
            _calendarRepository = calendarRepository;
            _auth = auth;
            _mapper = mapper;
        }

        public async Task<PagedResponse<CalendarEventViewModel>> Handle(GetCalendarEventsQuery request, CancellationToken cancellationToken)
        {
            var userId = _auth.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var role = _auth.Role ?? string.Empty;
            var filterUserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim();

            if (!string.IsNullOrEmpty(filterUserId) &&
                !string.Equals(role, Roles.Manager.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(role, Roles.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(filterUserId, userId, StringComparison.Ordinal))
                {
                    throw new ApiException("Employees may only filter by their own userId.");
                }
            }

            var criteria = new CalendarEventListCriteria
            {
                CurrentUserId = userId,
                CurrentRole = role,
                FromUtc = request.From,
                ToUtc = request.To,
                ProjectId = request.ProjectId,
                VisibilityType = string.IsNullOrWhiteSpace(request.VisibilityType) ? null : request.VisibilityType.Trim(),
                FilterUserId = filterUserId,
                Page = request.PageNumber,
                PageSize = request.PageSize
            };

            var (items, total) = await _calendarRepository.GetPagedVisibleAsync(criteria, cancellationToken);
            var vms = items.Select(e => _mapper.Map<CalendarEventViewModel>(e)).ToList();
            return new PagedResponse<CalendarEventViewModel>(vms, criteria.Page, criteria.PageSize, total);
        }
    }
}
