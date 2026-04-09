using CleanArchitecture.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface ICalendarEventRepositoryAsync : IGenericRepositoryAsync<CalendarEvent>
    {
        Task<CalendarEvent> GetByIdAndUserAsync(int id, string userId, bool isManager);
        Task<IReadOnlyList<CalendarEvent>> GetForUserAsync(
            string userId,
            bool isManager,
            DateTime from,
            DateTime to,
            int? projectId = null);
    }
}
