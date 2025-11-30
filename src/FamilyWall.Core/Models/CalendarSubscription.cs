namespace FamilyWall.Core.Models;

public class CalendarSubscription
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }           // ICS/iCal feed URL
    public required string Source { get; set; }        // "ICS", "Webcal"
    public bool IsEnabled { get; set; } = true;
    public string Color { get; set; } = "#3788D8";
    public int RefreshIntervalMinutes { get; set; } = 60;
    public DateTime? LastFetchUtc { get; set; }
    public string? LastETag { get; set; }
    public bool IsValid { get; set; } = true;          // Feed is accessible
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
