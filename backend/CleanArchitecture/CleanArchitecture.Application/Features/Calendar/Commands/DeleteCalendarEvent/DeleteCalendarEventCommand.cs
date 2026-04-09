using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Calendar.Commands.DeleteCalendarEvent
{
    public class DeleteCalendarEventCommand : IRequest<int>
    {
        public int Id { get; set; }
    }

    public class DeleteCalendarEventCommandHandler : IRequestHandler<DeleteCalendarEventCommand, int>
    {
        private readonly ICalendarEventRepositoryAsync _calendarEventRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public DeleteCalendarEventCommandHandler(
            ICalendarEventRepositoryAsync calendarEventRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _calendarEventRepository = calendarEventRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(DeleteCalendarEventCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var isManager = _authenticatedUserService.Role == "Manager" || _authenticatedUserService.Role == "Admin";

            var calendarEvent = await _calendarEventRepository.GetByIdAndUserAsync(request.Id, userId, isManager);
            if (calendarEvent == null)
            {
                throw new KeyNotFoundException($"Calendar event {request.Id} not found.");
            }

            calendarEvent.DeletedAtUtc = DateTime.UtcNow;
            await _calendarEventRepository.UpdateAsync(calendarEvent);

            return calendarEvent.Id;
        }
    }
}
