using FamilyWall.Core.Models;

namespace FamilyWall.Services;

public interface ICalendarService
{
    // Calendar discovery
    Task<List<CalendarConfiguration>> DiscoverCalendarsAsync(string source, CancellationToken cancellationToken = default);

    // Calendar CRUD
    Task<CalendarConfiguration?> GetCalendarAsync(int id, CancellationToken cancellationToken = default);
    Task<CalendarConfiguration?> GetCalendarByCalendarIdAsync(string calendarId, CancellationToken cancellationToken = default);
    Task<List<CalendarConfiguration>> GetAllCalendarsAsync(CancellationToken cancellationToken = default);
    Task<List<CalendarConfiguration>> GetEnabledCalendarsAsync(CancellationToken cancellationToken = default);
    Task<CalendarConfiguration> AddCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken = default);
    Task<CalendarConfiguration> UpdateCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken = default);
    Task DeleteCalendarAsync(int id, CancellationToken cancellationToken = default);

    // Calendar settings
    Task SetCalendarEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default);
    Task SetCalendarColorAsync(int id, string color, CancellationToken cancellationToken = default);
    Task ReorderCalendarsAsync(List<int> orderedIds, CancellationToken cancellationToken = default);

    // Bulk operations
    Task EnableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task DisableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default);

    // Sync operations
    Task SyncCalendarAsync(int id, CancellationToken cancellationToken = default);
    Task SyncAllCalendarsAsync(CancellationToken cancellationToken = default);
}
