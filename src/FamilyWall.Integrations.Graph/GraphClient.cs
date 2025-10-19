using System.Net.Http.Json;

namespace FamilyWall.Integrations.Graph;

/// <summary>
/// Stub client for Microsoft Graph API (photos, calendar, contacts).
/// </summary>
public interface IGraphClient
{
    Task<List<GraphPhoto>> GetRecentPhotosAsync(int count, CancellationToken cancellationToken = default);
    Task<List<GraphEvent>> GetCalendarEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}

public class GraphClient : IGraphClient
{
    private readonly HttpClient _httpClient;

    public GraphClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GraphPhoto>> GetRecentPhotosAsync(int count, CancellationToken cancellationToken = default)
    {
        // TODO: Implement using Microsoft.Graph SDK
        // 1. Call /me/drive/root:/Photos:/children with $top=count, $orderby=lastModifiedDateTime desc
        // 2. Map DriveItem to GraphPhoto
        await Task.CompletedTask;
        return new List<GraphPhoto>();
    }

    public async Task<List<GraphEvent>> GetCalendarEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        // TODO: Implement using Microsoft.Graph SDK
        // 1. Call /me/calendar/calendarView with startDateTime/endDateTime
        // 2. Map Event to GraphEvent
        // 3. Check for birthdays from /me/people or generated Birthday calendar
        await Task.CompletedTask;
        return new List<GraphEvent>();
    }
}

public record GraphPhoto(string Id, string Name, string? ThumbnailUrl, DateTime? TakenDateTime);
public record GraphEvent(string Id, string Subject, DateTime Start, DateTime End, bool IsAllDay, bool IsBirthday);
