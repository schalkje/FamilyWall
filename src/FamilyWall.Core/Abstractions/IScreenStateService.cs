using FamilyWall.Core.Models;

namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Manages the screen state and transitions between states.
/// </summary>
public interface IScreenStateService
{
    /// <summary>
    /// Gets the current screen state.
    /// </summary>
    ScreenState CurrentState { get; }

    /// <summary>
    /// Event raised when the screen state changes.
    /// </summary>
    event EventHandler<ScreenStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Requests a transition to the specified state.
    /// </summary>
    Task TransitionToAsync(ScreenState newState, string? reason = null);

    /// <summary>
    /// Gets whether the system is currently in Night Mode schedule.
    /// </summary>
    bool IsNightModeSchedule();
}

/// <summary>
/// Event arguments for screen state changes.
/// </summary>
public class ScreenStateChangedEventArgs : EventArgs
{
    public ScreenState PreviousState { get; init; }
    public ScreenState NewState { get; init; }
    public string? Reason { get; init; }
    public DateTime TransitionedAt { get; init; }
}
