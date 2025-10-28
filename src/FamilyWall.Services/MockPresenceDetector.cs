using FamilyWall.Core.Abstractions;
using FamilyWall.Core.Models;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Services;

/// <summary>
/// Mock presence detector that simulates motion events for testing.
/// This will be replaced with actual camera-based detection in a future milestone.
/// </summary>
public class MockPresenceDetector : IPresenceDetector
{
    private readonly ILogger<MockPresenceDetector> _logger;
    private CancellationTokenSource? _cts;
    private Task? _detectionTask;
    private readonly Random _random = new();

    public MockPresenceDetector(ILogger<MockPresenceDetector> logger)
    {
        _logger = logger;
    }

    public event EventHandler<PresenceEvent>? PresenceDetected;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("MockPresenceDetector is already running");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting MockPresenceDetector (simulated motion events)");
        _cts = new CancellationTokenSource();

        _detectionTask = Task.Run(async () =>
        {
            await DetectionLoopAsync(_cts.Token);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping MockPresenceDetector");
        _cts?.Cancel();

        if (_detectionTask != null)
        {
            await _detectionTask;
        }

        _cts?.Dispose();
        _cts = null;
        _detectionTask = null;
    }

    private async Task DetectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Simulate random motion detection every 15-45 seconds
                var delaySeconds = _random.Next(15, 45);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                // Simulate motion detection with varying confidence
                var confidence = 0.6 + (_random.NextDouble() * 0.4); // 0.6 to 1.0

                var presenceEvent = new PresenceEvent
                {
                    DetectedAt = DateTime.UtcNow,
                    Source = PresenceSource.Camera,
                    Confidence = confidence,
                    Details = $"Mock motion detected (confidence: {confidence:P0})"
                };

                _logger.LogInformation("Mock motion detected: {Details}", presenceEvent.Details);
                PresenceDetected?.Invoke(this, presenceEvent);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mock presence detection loop");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        _logger.LogInformation("MockPresenceDetector loop stopped");
    }
}
