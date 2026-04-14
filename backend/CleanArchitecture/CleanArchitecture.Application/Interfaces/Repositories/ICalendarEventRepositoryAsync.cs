using CleanArchitecture.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public class CalendarEventListCriteria
    {
        public string CurrentUserId { get; set; }
        public string CurrentRole { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int? ProjectId { get; set; }
        public string VisibilityType { get; set; }
        public string FilterUserId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public interface ICalendarEventRepositoryAsync
    {
        Task<CalendarEvent> AddAsync(CalendarEvent calendarEvent, IReadOnlyList<CalendarEventParticipant> participants, CancellationToken cancellationToken = default);

        Task<CalendarEvent> GetByIdWithParticipantsAsync(Guid id, bool tracked, CancellationToken cancellationToken = default);

        Task<CalendarEvent> GetByIdVisibleAsync(Guid id, string userId, string role, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<CalendarEvent> Items, int TotalCount)> GetPagedVisibleAsync(CalendarEventListCriteria criteria, CancellationToken cancellationToken = default);

        Task UpdateAsync(CalendarEvent calendarEvent, IReadOnlyList<CalendarEventParticipant> participants, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> AreAllUsersActiveAssigneesOnManagedProjectsAsync(string managerUserId, IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);
    }
}
