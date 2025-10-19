using FamilyWall.Core.Abstractions;
using FamilyWall.Core.Models;
using FamilyWall.Core.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyWall.Services;

/// <summary>
/// Background service that coordinates presence detection and screen state transitions.
/// Uses a mock detector for MVP; will integrate with camera and optional PIR sensors later.
/// </summary>
public class PresenceService : BackgroundService
{
    private readonly ILogger<PresenceService> _logger;
    private readonly IScreenStateService _screenStateService;
    private readonly IPresenceDetector _presenceDetector;
    private readonly IOptions<AppSettings> _settings;
    private DateTime? _lastMotionDetectedAt;
    private Timer? _inactivityTimer;

    public PresenceService(
        ILogger<PresenceService> logger,
        IScreenStateService screenStateService,
        IPresenceDetector presenceDetector,
        IOptions<AppSettings> settings)
    {
        _logger = logger;
        _screenStateService = screenStateService;
        _presenceDetector = presenceDetector;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PresenceService starting...");

        // Subscribe to presence events
        _presenceDetector.PresenceDetected += OnPresenceDetected;

        // Start the presence detector
        await _presenceDetector.StartAsync(stoppingToken);

        // Monitor state transitions based on schedule
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateStateBasedOnScheduleAsync();
                await CheckInactivityTimeoutAsync();

                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PresenceService encountered an error");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        // Cleanup
        _presenceDetector.PresenceDetected -= OnPresenceDetected;
        await _presenceDetector.StopAsync(stoppingToken);
        _inactivityTimer?.Dispose();

        _logger.LogInformation("PresenceService stopped.");
    }

    private void OnPresenceDetected(object? sender, PresenceEvent presenceEvent)
    {
        _logger.LogInformation(
            "Presence detected from {Source} at {DetectedAt} (confidence: {Confidence:P0})",
            presenceEvent.Source,
            presenceEvent.DetectedAt,
            presenceEvent.Confidence);

        _lastMotionDetectedAt = presenceEvent.DetectedAt;

        // Determine target state based on current schedule
        var isNightMode = _screenStateService.IsNightModeSchedule();
        var targetState = isNightMode ? ScreenState.NightLive : ScreenState.Interactive;

        // Only transition if we're not already in the target state
        if (_screenStateService.CurrentState != targetState)
        {
            _screenStateService.TransitionToAsync(
                targetState,
                $"Motion detected from {presenceEvent.Source}").Wait();
        }

        // Reset inactivity timer
        ResetInactivityTimer();
    }

    private async Task UpdateStateBasedOnScheduleAsync()
    {
        var isNightMode = _screenStateService.IsNightModeSchedule();
        var currentState = _screenStateService.CurrentState;

        // Transition to Off when entering Night Mode schedule (if not already showing live view)
        if (isNightMode && currentState == ScreenState.Interactive)
        {
            await _screenStateService.TransitionToAsync(ScreenState.Off, "Night Mode schedule started");
            _lastMotionDetectedAt = null;
        }
        // Transition to Interactive when leaving Night Mode schedule
        else if (!isNightMode && (currentState == ScreenState.Off || currentState == ScreenState.NightLive))
        {
            await _screenStateService.TransitionToAsync(ScreenState.Interactive, "Day Mode schedule started");
            _lastMotionDetectedAt = null;
        }
    }

    private async Task CheckInactivityTimeoutAsync()
    {
        if (!_lastMotionDetectedAt.HasValue)
        {
            return;
        }

        var inactivityThreshold = TimeSpan.FromSeconds(_settings.Value.NightMode.InactivityTimeoutSeconds);
        var timeSinceLastMotion = DateTime.UtcNow - _lastMotionDetectedAt.Value;

        // If in NightLive mode and inactivity timeout exceeded, return to Off
        if (_screenStateService.CurrentState == ScreenState.NightLive &&
            timeSinceLastMotion > inactivityThreshold)
        {
            _logger.LogInformation(
                "Inactivity timeout exceeded ({Seconds}s), returning to Off state",
                timeSinceLastMotion.TotalSeconds);

            await _screenStateService.TransitionToAsync(ScreenState.Off, "Inactivity timeout");
            _lastMotionDetectedAt = null;
        }
    }

    private void ResetInactivityTimer()
    {
        _inactivityTimer?.Dispose();

        var timeout = TimeSpan.FromSeconds(_settings.Value.NightMode.InactivityTimeoutSeconds);
        _inactivityTimer = new Timer(
            _ => Task.Run(() => CheckInactivityTimeoutAsync()),
            null,
            timeout,
            Timeout.InfiniteTimeSpan);
    }
}
