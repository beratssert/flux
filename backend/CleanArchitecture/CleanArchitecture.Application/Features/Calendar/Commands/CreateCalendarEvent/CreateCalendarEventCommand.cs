using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Calendar.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommand : IRequest<int>
    {
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public bool AllDay { get; set; }
        /// <summary>Personal, Project, or Team</summary>
        public string Visibility { get; set; }
    }

    public class CreateCalendarEventCommandHandler : IRequestHandler<CreateCalendarEventCommand, int>
    {
        private readonly ICalendarEventRepositoryAsync _calendarEventRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public CreateCalendarEventCommandHandler(
            ICalendarEventRepositoryAsync calendarEventRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _calendarEventRepository = calendarEventRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(CreateCalendarEventCommand request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new ApiException("Title is required.");
            }

            if (!request.AllDay && request.EndUtc <= request.StartUtc)
            {
                throw new ApiException("End time must be after start time.");
            }

            var calendarEvent = new CalendarEvent
            {
                CreatedByUserId = userId,
                ProjectId = request.ProjectId,
                Title = request.Title.Trim(),
                Description = request.Description,
                StartUtc = request.StartUtc,
                EndUtc = request.EndUtc,
                AllDay = request.AllDay,
                Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? "Project" : request.Visibility,
            };

            await _calendarEventRepository.AddAsync(calendarEvent);

            return calendarEvent.Id;
        }
    }
}
