using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Calendar.Commands.UpdateCalendarEvent
{
    public class UpdateCalendarEventCommand : IRequest<int>
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public bool AllDay { get; set; }
        public string Visibility { get; set; }
    }

    public class UpdateCalendarEventCommandHandler : IRequestHandler<UpdateCalendarEventCommand, int>
    {
        private readonly ICalendarEventRepositoryAsync _calendarEventRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public UpdateCalendarEventCommandHandler(
            ICalendarEventRepositoryAsync calendarEventRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _calendarEventRepository = calendarEventRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(UpdateCalendarEventCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var isManager = _authenticatedUserService.Role == "Manager" || _authenticatedUserService.Role == "Admin";

            var calendarEvent = await _calendarEventRepository.GetByIdAndUserAsync(request.Id, userId, isManager);
            if (calendarEvent == null)
            {
                throw new KeyNotFoundException($"Calendar event {request.Id} not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ApiException("Title is required.");
            }

            if (!request.AllDay && request.EndUtc <= request.StartUtc)
            {
                throw new ApiException("End time must be after start time.");
            }

            calendarEvent.ProjectId = request.ProjectId;
            calendarEvent.Title = request.Title.Trim();
            calendarEvent.Description = request.Description;
            calendarEvent.StartUtc = request.StartUtc;
            calendarEvent.EndUtc = request.EndUtc;
            calendarEvent.AllDay = request.AllDay;
            calendarEvent.Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? calendarEvent.Visibility : request.Visibility;

            await _calendarEventRepository.UpdateAsync(calendarEvent);

            return calendarEvent.Id;
        }
    }
}
