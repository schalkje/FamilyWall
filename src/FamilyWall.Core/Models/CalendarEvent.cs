namespace FamilyWall.Core.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public required string ProviderKey { get; set; } // Compound key from provider (e.g., "graph:event123")
    public required string Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsBirthday { get; set; }
    public required string Source { get; set; } // "Graph", "Google", "ICS"
    public string? ETag { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
