using FamilyWall.Core.Models;

namespace FamilyWall.Services;

public interface ICalendarEventService
{
    // Event retrieval
    Task<CalendarEvent?> GetEventAsync(int id, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsForCalendarAsync(string calendarId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetUpcomingEventsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> SearchEventsAsync(string query, CancellationToken cancellationToken = default);

    // Event filtering
    Task<List<CalendarEvent>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetRecurringEventsAsync(CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetBirthdaysAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    // Event CRUD (for local/editable events)
    Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    Task<CalendarEvent> UpdateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(int id, CancellationToken cancellationToken = default);

    // Event response
    Task UpdateResponseStatusAsync(int eventId, EventResponseStatus status, CancellationToken cancellationToken = default);

    // Statistics
    Task<int> GetEventCountAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetEventCountByCalendarAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
