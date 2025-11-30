namespace FamilyWall.Core.Models;

public class CalendarConfiguration
{
    public int Id { get; set; }
    public required string CalendarId { get; set; }    // Unique ID from provider
    public required string Name { get; set; }          // Display name
    public required string Source { get; set; }        // "Graph", "Google", "ICS"
    public string? Owner { get; set; }                 // Owner email/name
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;        // Show/hide calendar
    public bool IsDefault { get; set; }                // Default calendar flag
    public bool CanEdit { get; set; }                  // User has write permissions
    public bool CanShare { get; set; }                 // User can share calendar
    public string Color { get; set; } = "#3788D8";     // Display color (hex)
    public int DisplayOrder { get; set; }              // Sort order in UI
    public bool ShowInAgenda { get; set; } = true;     // Show in agenda view
    public bool ShowInMonthView { get; set; } = true;  // Show in month view
    public bool ShowInWeekView { get; set; } = true;   // Show in week view
    public CalendarVisibility Visibility { get; set; } = CalendarVisibility.Private;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncUtc { get; set; }

    // Sync settings
    public int SyncIntervalMinutes { get; set; } = 15;
    public bool SyncPastEvents { get; set; } = false;
    public int FutureDaysToSync { get; set; } = 90;

    // Navigation
    public List<CalendarEvent> Events { get; set; } = new();
}
