namespace FamilyWall.Core.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public required string ProviderKey { get; set; }  // Unique ID from provider
    public required string CalendarId { get; set; }   // Link to source calendar
    public required string Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsBirthday { get; set; }
    public bool IsRecurring { get; set; }             // Recurring event flag
    public string? RecurrenceRule { get; set; }       // iCalendar RRULE format
    public required string Source { get; set; }       // "Graph", "Google", "ICS"
    public string? ETag { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Organizer { get; set; }            // Event organizer
    public List<string>? Attendees { get; set; }      // List of attendees
    public EventStatus Status { get; set; } = EventStatus.Confirmed;
    public EventResponseStatus? ResponseStatus { get; set; }
    public string? OnlineMeetingUrl { get; set; }     // Teams/Zoom link
    public string? Color { get; set; }                // Event color override
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncUtc { get; set; }        // Last sync timestamp

    // Navigation
    public CalendarConfiguration? Calendar { get; set; }
}
