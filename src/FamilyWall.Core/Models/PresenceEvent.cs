namespace FamilyWall.Core.Models;

/// <summary>
/// Represents a detected presence/motion event.
/// </summary>
public class PresenceEvent
{
    public DateTime DetectedAt { get; set; }
    public PresenceSource Source { get; set; }
    public double Confidence { get; set; } // 0.0 to 1.0
    public string? Details { get; set; }
}

/// <summary>
/// Source of the presence detection.
/// </summary>
public enum PresenceSource
{
    Camera,
    PIR,
    Manual,
    Scheduled
}
