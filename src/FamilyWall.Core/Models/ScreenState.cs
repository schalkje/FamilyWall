namespace FamilyWall.Core.Models;

/// <summary>
/// Represents the current state of the display panel.
/// </summary>
public enum ScreenState
{
    /// <summary>
    /// Screen is off (Night Mode, no motion detected)
    /// </summary>
    Off,

    /// <summary>
    /// Ambient mode: low-glare clock or minimal display
    /// </summary>
    Ambient,

    /// <summary>
    /// Interactive mode: full slideshow and calendar (day mode)
    /// </summary>
    Interactive,

    /// <summary>
    /// Night Mode with live camera preview (motion detected)
    /// </summary>
    NightLive
}
