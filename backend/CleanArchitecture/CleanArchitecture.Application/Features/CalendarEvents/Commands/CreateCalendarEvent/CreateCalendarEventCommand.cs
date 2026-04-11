using AutoMapper;
using CleanArchitecture.Core.Constants;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Features.CalendarEvents;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.CalendarEvents.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommand : IRequest<CalendarEventViewModel>
    {
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public string VisibilityType { get; set; }
        public bool IsAllDay { get; set; }
        public List<string> ParticipantUserIds { get; set; }
    }

    public class CreateCalendarEventCommandHandler : IRequestHandler<CreateCalendarEventCommand, CalendarEventViewModel>
    {
        private readonly ICalendarEventRepositoryAsync _calendarRepository;
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _auth;
        private readonly IMapper _mapper;

        public CreateCalendarEventCommandHandler(
            ICalendarEventRepositoryAsync calendarRepository,
            IProjectRepositoryAsync projectRepository,
            IAuthenticatedUserService auth,
            IMapper mapper)
        {
            _calendarRepository = calendarRepository;
            _projectRepository = projectRepository;
            _auth = auth;
            _mapper = mapper;
        }

        public async Task<CalendarEventViewModel> Handle(CreateCalendarEventCommand request, CancellationToken cancellationToken)
        {
            var userId = _auth.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ApiException("Authenticated user not found.");
            }

            var visibility = NormalizeVisibility(request.VisibilityType);
            if (visibility == null)
            {
                throw new ApiException("Invalid visibilityType. Use Personal, Project, or Team.");
            }

            if (request.EndAtUtc <= request.StartAtUtc)
            {
                throw new ApiException("endAtUtc must be after startAtUtc.");
            }

            var title = request.Title?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                throw new ApiException("Title is required.");
            }

            var participantIds = DistinctParticipants(request.ParticipantUserIds);

            if (visibility == CalendarVisibilityTypes.Project || visibility == CalendarVisibilityTypes.Team)
            {
                if (!request.ProjectId.HasValue)
                {
                    throw new ApiException("projectId is required for Project and Team visibility.");
                }

                var managed = await _projectRepository.IsManagedByAsync(userId, request.ProjectId.Value);
                if (!managed)
                {
                    throw new NotFoundException("Project not found.");
                }
            }
            else if (visibility == CalendarVisibilityTypes.Personal)
            {
                if (request.ProjectId.HasValue)
                {
                    throw new ApiException("projectId must be null for Personal events.");
                }

                if (participantIds.Count == 0)
                {
                    throw new ApiException("At least one participant is required for Personal events.");
                }

                var ok = await _calendarRepository.AreAllUsersActiveAssigneesOnManagedProjectsAsync(userId, participantIds, cancellationToken);
                if (!ok)
                {
                    throw new ApiException("All participants must have an active assignment on a project you manage.");
                }
            }

            var participants = participantIds
                .Select(uid => new CalendarEventParticipant
                {
                    UserId = uid,
                    ParticipationType = CalendarParticipationTypes.Optional
                })
                .ToList();

            var entity = new CalendarEvent
            {
                Title = title,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                VisibilityType = visibility,
                IsAllDay = request.IsAllDay,
                ProjectId = visibility == CalendarVisibilityTypes.Personal ? null : request.ProjectId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _calendarRepository.AddAsync(entity, participants, cancellationToken);
            var withParts = await _calendarRepository.GetByIdWithParticipantsAsync(entity.Id, false, cancellationToken);
            return _mapper.Map<CalendarEventViewModel>(withParts);
        }

        private static string NormalizeVisibility(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var v = value.Trim();
            if (string.Equals(v, CalendarVisibilityTypes.Personal, StringComparison.OrdinalIgnoreCase))
            {
                return CalendarVisibilityTypes.Personal;
            }

            if (string.Equals(v, CalendarVisibilityTypes.Project, StringComparison.OrdinalIgnoreCase))
            {
                return CalendarVisibilityTypes.Project;
            }

            if (string.Equals(v, CalendarVisibilityTypes.Team, StringComparison.OrdinalIgnoreCase))
            {
                return CalendarVisibilityTypes.Team;
            }

            return null;
        }

        private static List<string> DistinctParticipants(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return new List<string>();
            }

            return ids
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }
    }
}
