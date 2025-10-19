using FamilyWall.Core.Abstractions;
using FamilyWall.Core.Models;
using FamilyWall.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyWall.Services;

/// <summary>
/// Manages screen state transitions and tracks the current state.
/// </summary>
public class ScreenStateService : IScreenStateService
{
    private readonly ILogger<ScreenStateService> _logger;
    private readonly IOptions<AppSettings> _settings;
    private ScreenState _currentState;
    private readonly object _stateLock = new();

    public ScreenStateService(
        ILogger<ScreenStateService> logger,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _settings = settings;

        // Initialize to appropriate state based on time of day
        _currentState = IsNightModeSchedule() ? ScreenState.Off : ScreenState.Interactive;
    }

    public ScreenState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public event EventHandler<ScreenStateChangedEventArgs>? StateChanged;

    public Task TransitionToAsync(ScreenState newState, string? reason = null)
    {
        ScreenState previousState;

        lock (_stateLock)
        {
            if (_currentState == newState)
            {
                _logger.LogDebug("Already in state {State}, ignoring transition", newState);
                return Task.CompletedTask;
            }

            previousState = _currentState;
            _currentState = newState;
        }

        var eventArgs = new ScreenStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState,
            Reason = reason,
            TransitionedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Screen state transition: {PreviousState} -> {NewState} (Reason: {Reason})",
            previousState,
            newState,
            reason ?? "unspecified");

        // Raise event on background thread to avoid blocking
        Task.Run(() =>
        {
            try
            {
                StateChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying StateChanged subscribers");
            }
        });

        return Task.CompletedTask;
    }

    public bool IsNightModeSchedule()
    {
        var nightMode = _settings.Value.NightMode;
        if (!nightMode.Enabled)
        {
            return false;
        }

        var now = TimeOnly.FromDateTime(DateTime.Now);
        var start = nightMode.StartTime;
        var end = nightMode.EndTime;

        // Handle case where night mode crosses midnight
        if (start < end)
        {
            return now >= start && now < end;
        }
        else
        {
            return now >= start || now < end;
        }
    }
}
