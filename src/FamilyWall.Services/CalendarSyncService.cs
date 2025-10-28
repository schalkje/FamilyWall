using FamilyWall.Core.Models;
using FamilyWall.Core.Settings;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Integrations.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyWall.Services;

/// <summary>
/// Background service that syncs calendar events from Microsoft Graph with local cache.
/// Implements short-lived cache with TTL to prefer live reads while supporting brief offline continuity.
/// </summary>
public class CalendarSyncService : BackgroundService
{
    private readonly IGraphClient _graphClient;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly CalendarSettings _settings;
    private readonly ILogger<CalendarSyncService> _logger;
    private DateTime _lastSyncTime = DateTime.MinValue;

    public CalendarSyncService(
        IGraphClient graphClient,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IOptions<AppSettings> appSettings,
        ILogger<CalendarSyncService> logger)
    {
        _graphClient = graphClient;
        _dbContextFactory = dbContextFactory;
        _settings = appSettings.Value.Calendar;
        _logger = logger;

        _logger.LogInformation("CalendarSyncService constructed with CacheTtlMinutes={Minutes}", _settings.CacheTtlMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("CalendarSyncService starting");

            // Wait a bit for the app to fully start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            _logger.LogInformation("CalendarSyncService: Initial delay complete, starting sync loop");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncCalendarEventsAsync(stoppingToken);

                // Wait for the configured TTL before next sync
                var syncInterval = TimeSpan.FromMinutes(_settings.CacheTtlMinutes);
                _logger.LogDebug("Next calendar sync in {Minutes} minutes", _settings.CacheTtlMinutes);
                await Task.Delay(syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in calendar sync loop, retrying in 5 minutes");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("CalendarSyncService stopped");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CalendarSyncService failed to start or encountered critical error");
            throw;
        }
    }

    private async Task SyncCalendarEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if authenticated
            var isAuthenticated = await _graphClient.IsAuthenticatedAsync(cancellationToken);
            if (!isAuthenticated)
            {
                _logger.LogWarning("Not authenticated with Microsoft Graph, skipping calendar sync");
                return;
            }

            // Fetch events for the next 90 days
            var start = DateTime.Today;
            var end = start.AddDays(90);

            _logger.LogInformation("Syncing calendar events from {Start:d} to {End:d}", start, end);

            var graphEvents = await _graphClient.GetCalendarEventsAsync(start, end, cancellationToken);

            _logger.LogInformation("Fetched {Count} events from Microsoft Graph", graphEvents.Count);

            if (graphEvents.Count == 0)
            {
                _logger.LogInformation("No calendar events found in the date range");

                // Still update the database to clear old events
                await using var emptyDbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var allOldEvents = await emptyDbContext.CalendarEvents
                    .Where(e => e.Source == "Graph")
                    .ToListAsync(cancellationToken);

                if (allOldEvents.Count > 0)
                {
                    emptyDbContext.CalendarEvents.RemoveRange(allOldEvents);
                    await emptyDbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Cleared {Count} old calendar events", allOldEvents.Count);
                }

                _lastSyncTime = DateTime.UtcNow;
                return;
            }

            // Store events in database
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // Remove old cached events (beyond the current sync range)
            var oldEvents = await dbContext.CalendarEvents
                .Where(e => e.Source == "Graph" && (e.StartUtc < start || e.StartUtc > end))
                .ToListAsync(cancellationToken);

            if (oldEvents.Count > 0)
            {
                dbContext.CalendarEvents.RemoveRange(oldEvents);
                _logger.LogDebug("Removed {Count} old calendar events", oldEvents.Count);
            }

            // Upsert new events
            foreach (var graphEvent in graphEvents)
            {
                var existingEvent = await dbContext.CalendarEvents
                    .FirstOrDefaultAsync(e => e.ProviderKey == graphEvent.Id && e.Source == "Graph", cancellationToken);

                if (existingEvent != null)
                {
                    // Update existing event
                    existingEvent.Title = graphEvent.Subject;
                    existingEvent.StartUtc = graphEvent.Start;
                    existingEvent.EndUtc = graphEvent.End;
                    existingEvent.IsAllDay = graphEvent.IsAllDay;
                    existingEvent.IsBirthday = graphEvent.IsBirthday;
                }
                else
                {
                    // Add new event
                    dbContext.CalendarEvents.Add(new CalendarEvent
                    {
                        ProviderKey = graphEvent.Id,
                        Title = graphEvent.Subject,
                        StartUtc = graphEvent.Start,
                        EndUtc = graphEvent.End,
                        IsAllDay = graphEvent.IsAllDay,
                        IsBirthday = graphEvent.IsBirthday,
                        Source = "Graph",
                        ETag = null
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            _lastSyncTime = DateTime.UtcNow;
            _logger.LogInformation("Successfully synced {Count} calendar events", graphEvents.Count);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("authentication required"))
        {
            _logger.LogWarning("Authentication required for calendar sync. Please authenticate with Microsoft Graph.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendar events");
            throw;
        }
    }

    /// <summary>
    /// Manually trigger a sync (useful for testing or immediate refresh)
    /// </summary>
    public async Task TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual calendar sync triggered");
        await SyncCalendarEventsAsync(cancellationToken);
    }

    /// <summary>
    /// Get the last sync time
    /// </summary>
    public DateTime GetLastSyncTime() => _lastSyncTime;
}
