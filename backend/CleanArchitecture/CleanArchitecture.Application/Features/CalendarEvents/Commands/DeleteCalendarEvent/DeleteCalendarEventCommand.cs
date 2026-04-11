using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.CalendarEvents.Commands.DeleteCalendarEvent
{
    public class DeleteCalendarEventCommand : IRequest<Unit>
    {
        public Guid Id { get; set; }
    }

    public class DeleteCalendarEventCommandHandler : IRequestHandler<DeleteCalendarEventCommand, Unit>
    {
        private readonly ICalendarEventRepositoryAsync _calendarRepository;
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _auth;

        public DeleteCalendarEventCommandHandler(
            ICalendarEventRepositoryAsync calendarRepository,
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService auth)
        {
            _calendarRepository = calendarRepository;
            _projectRepository = projectRepository;
            _auth = auth;
        }

        public async Task<Unit> Handle(DeleteCalendarEventCommand request, CancellationToken cancellationToken)
        {
            var userId = _auth.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var existing = await _calendarRepository.GetByIdWithParticipantsAsync(request.Id, false, cancellationToken);
            if (existing == null)
            {
                throw new NotFoundException("Calendar event not found.");
            }

            if (!await CanManagerMutateAsync(existing, userId, cancellationToken))
            {
                throw new NotFoundException("Calendar event not found.");
            }

            await _calendarRepository.DeleteAsync(request.Id, cancellationToken);
            return Unit.Value;
        }

        private async Task<bool> CanManagerMutateAsync(CalendarEvent e, string managerId, CancellationToken cancellationToken)
        {
            if (e.VisibilityType == CalendarVisibilityTypes.Personal)
            {
                return string.Equals(e.CreatedByUserId, managerId, StringComparison.Ordinal);
            }

            if (e.ProjectId.HasValue)
            {
                return await _projectRepository.IsManagedByAsync(managerId, e.ProjectId.Value);
            }

            return false;
        }
    }
}
