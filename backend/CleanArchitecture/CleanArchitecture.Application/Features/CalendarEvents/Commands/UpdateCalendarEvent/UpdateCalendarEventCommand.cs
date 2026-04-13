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

namespace CleanArchitecture.Core.Features.CalendarEvents.Commands.UpdateCalendarEvent
{
    public class UpdateCalendarEventCommand : IRequest<CalendarEventViewModel>
    {
        public Guid Id { get; set; }
        public int? ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public string VisibilityType { get; set; }
        public bool? IsAllDay { get; set; }
        public List<string> ParticipantUserIds { get; set; }
    }

    public class UpdateCalendarEventCommandHandler : IRequestHandler<UpdateCalendarEventCommand, CalendarEventViewModel>
    {
        private readonly ICalendarEventRepositoryAsync _calendarRepository;
        private readonly IProjectRepositoryAsync _projectRepository;
        private readonly IAuthenticatedUserService _auth;
        private readonly IMapper _mapper;

        public UpdateCalendarEventCommandHandler(
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

        public async Task<CalendarEventViewModel> Handle(UpdateCalendarEventCommand request, CancellationToken cancellationToken)
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

            var visibility = existing.VisibilityType;
            if (!string.IsNullOrWhiteSpace(request.VisibilityType))
            {
                var n = NormalizeVisibility(request.VisibilityType);
                if (n == null)
                {
                    throw new ApiException("Invalid visibilityType.");
                }

                visibility = n;
            }

            var projectId = request.ProjectId ?? existing.ProjectId;
            if (visibility == CalendarVisibilityTypes.Personal)
            {
                projectId = null;
            }

            if (visibility == CalendarVisibilityTypes.Project || visibility == CalendarVisibilityTypes.Team)
            {
                if (!projectId.HasValue)
                {
                    throw new ApiException("projectId is required for Project and Team visibility.");
                }

                var managed = await _projectRepository.IsManagedByAsync(userId, projectId.Value);
                if (!managed)
                {
                    throw new NotFoundException("Project not found.");
                }
            }

            var title = existing.Title;
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                title = request.Title.Trim();
            }

            var description = existing.Description;
            if (request.Description != null)
            {
                description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            var start = request.StartAtUtc ?? existing.StartAtUtc;
            var end = request.EndAtUtc ?? existing.EndAtUtc;
            if (end <= start)
            {
                throw new ApiException("endAtUtc must be after startAtUtc.");
            }

            var isAllDay = request.IsAllDay ?? existing.IsAllDay;

            List<CalendarEventParticipant> participants;
            if (request.ParticipantUserIds != null)
            {
                var ids = DistinctParticipants(request.ParticipantUserIds);
                if (visibility == CalendarVisibilityTypes.Personal && ids.Count == 0)
                {
                    throw new ApiException("At least one participant is required for Personal events.");
                }

                if (visibility == CalendarVisibilityTypes.Personal)
                {
                    var ok = await _calendarRepository.AreAllUsersActiveAssigneesOnManagedProjectsAsync(userId, ids, cancellationToken);
                    if (!ok)
                    {
                        throw new ApiException("All participants must have an active assignment on a project you manage.");
                    }
                }

                participants = ids.Select(uid => new CalendarEventParticipant
                {
                    UserId = uid,
                    ParticipationType = CalendarParticipationTypes.Optional
                }).ToList();
            }
            else
            {
                participants = existing.Participants
                    .Select(p => new CalendarEventParticipant { UserId = p.UserId, ParticipationType = p.ParticipationType })
                    .ToList();
            }

            var updated = new CalendarEvent
            {
                Id = existing.Id,
                Title = title,
                Description = description,
                StartAtUtc = start,
                EndAtUtc = end,
                VisibilityType = visibility,
                IsAllDay = isAllDay,
                ProjectId = projectId,
                CreatedByUserId = existing.CreatedByUserId,
                CreatedAtUtc = existing.CreatedAtUtc
            };

            await _calendarRepository.UpdateAsync(updated, participants, cancellationToken);
            var result = await _calendarRepository.GetByIdWithParticipantsAsync(request.Id, false, cancellationToken);
            return _mapper.Map<CalendarEventViewModel>(result);
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
