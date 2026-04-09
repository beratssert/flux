using CleanArchitecture.Core.Enums;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Calendar.Queries.GetCalendarItems
{
    public class GetCalendarItemsQuery : IRequest<List<CalendarItemViewModel>>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int? ProjectId { get; set; }
        public bool IncludeTimeEntries { get; set; } = true;
    }

    public class GetCalendarItemsQueryHandler : IRequestHandler<GetCalendarItemsQuery, List<CalendarItemViewModel>>
    {
        private readonly ICalendarEventRepositoryAsync _calendarEventRepository;
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetCalendarItemsQueryHandler(
            ICalendarEventRepositoryAsync calendarEventRepository,
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _calendarEventRepository = calendarEventRepository;
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<List<CalendarItemViewModel>> Handle(GetCalendarItemsQuery request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUserService.UserId;
            var role = _authenticatedUserService.Role;
            var isManager = role == Roles.Manager.ToString() || role == Roles.Admin.ToString();

            var from = request.From.Date;
            var to = request.To.Date.AddDays(1).AddTicks(-1);

            var result = new List<CalendarItemViewModel>();

            // Calendar events
            var events = await _calendarEventRepository.GetForUserAsync(userId, isManager, from, to, request.ProjectId);
            foreach (var ev in events)
            {
                result.Add(new CalendarItemViewModel
                {
                    ItemType = "Event",
                    Id = ev.Id,
                    Title = ev.Title,
                    Description = ev.Description,
                    StartUtc = ev.StartUtc,
                    EndUtc = ev.EndUtc,
                    AllDay = ev.AllDay,
                    ProjectId = ev.ProjectId,
                    Visibility = ev.Visibility,
                });
            }

            // Time entries as calendar items
            if (request.IncludeTimeEntries)
            {
                var entries = await _timeEntryRepository.GetPagedByUserIdAsync(
                    userId,
                    pageNumber: 1,
                    pageSize: 1000,
                    projectId: request.ProjectId,
                    from: from,
                    to: to);

                foreach (var te in entries)
                {
                    var startUtc = te.StartTimeUtc ?? te.EntryDate.Date.ToUniversalTime();
                    var endUtc = te.EndTimeUtc ?? startUtc.AddMinutes(te.DurationMinutes);

                    result.Add(new CalendarItemViewModel
                    {
                        ItemType = "TimeEntry",
                        Id = te.Id,
                        Title = string.IsNullOrWhiteSpace(te.Description) ? "Time Entry" : te.Description,
                        Description = te.Description,
                        StartUtc = startUtc,
                        EndUtc = endUtc,
                        AllDay = false,
                        ProjectId = te.ProjectId,
                        DurationMinutes = te.DurationMinutes,
                        IsBillable = te.IsBillable,
                    });
                }
            }

            return result.OrderBy(x => x.StartUtc).ToList();
        }
    }
}
