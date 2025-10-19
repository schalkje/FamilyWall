using FamilyWall.Core.Models;

namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Interface for presence detection sources (camera, PIR, etc.).
/// </summary>
public interface IPresenceDetector
{
    /// <summary>
    /// Event raised when presence/motion is detected.
    /// </summary>
    event EventHandler<PresenceEvent>? PresenceDetected;

    /// <summary>
    /// Starts the presence detector.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the presence detector.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the detector is currently running.
    /// </summary>
    bool IsRunning { get; }
}
