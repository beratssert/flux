using AutoMapper;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.CalendarEvents;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.CalendarEvents.Queries.GetCalendarEventById
{
    public class GetCalendarEventByIdQuery : IRequest<CalendarEventViewModel>
    {
        public Guid Id { get; set; }
    }

    public class GetCalendarEventByIdQueryHandler : IRequestHandler<GetCalendarEventByIdQuery, CalendarEventViewModel>
    {
        private readonly ICalendarEventRepositoryAsync _calendarRepository;
        private readonly IAuthenticatedUserService _auth;
        private readonly IMapper _mapper;

        public GetCalendarEventByIdQueryHandler(
            ICalendarEventRepositoryAsync calendarRepository,
            IAuthenticatedUserService auth,
            IMapper mapper)
        {
            _calendarRepository = calendarRepository;
            _auth = auth;
            _mapper = mapper;
        }

        public async Task<CalendarEventViewModel> Handle(GetCalendarEventByIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _auth.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var role = _auth.Role ?? string.Empty;
            var entity = await _calendarRepository.GetByIdVisibleAsync(request.Id, userId, role, cancellationToken);
            if (entity == null)
            {
                throw new NotFoundException("Calendar event not found.");
            }

            return _mapper.Map<CalendarEventViewModel>(entity);
        }
    }
}
