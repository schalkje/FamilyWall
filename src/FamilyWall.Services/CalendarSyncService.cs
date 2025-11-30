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
/// Background service that syncs calendar events from multiple calendars with local cache.
/// Implements configurable sync strategies for different calendar sources.
/// </summary>
public class CalendarSyncService : BackgroundService
{
    private readonly ICalendarService _calendarService;
    private readonly IGraphClient _graphClient;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly CalendarSettings _settings;
    private readonly ILogger<CalendarSyncService> _logger;
    private readonly Dictionary<string, ISyncStrategy> _syncStrategies = new();
    private DateTime _lastSyncTime = DateTime.MinValue;

    public CalendarSyncService(
        ICalendarService calendarService,
        IGraphClient graphClient,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IOptions<AppSettings> appSettings,
        ILogger<CalendarSyncService> logger,
        IEnumerable<ISyncStrategy> syncStrategies)
    {
        _calendarService = calendarService;
        _graphClient = graphClient;
        _dbContextFactory = dbContextFactory;
        _settings = appSettings.Value.Calendar;
        _logger = logger;

        // Register sync strategies
        foreach (var strategy in syncStrategies)
        {
            _syncStrategies[strategy.Source] = strategy;
        }

        _logger.LogInformation("CalendarSyncService constructed with {StrategyCount} sync strategies",
            _syncStrategies.Count);
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
                    await SyncAllCalendarsAsync(stoppingToken);

                    // Use the minimum sync interval from all calendars or default
                    var minInterval = await GetMinimumSyncIntervalAsync(stoppingToken);
                    _logger.LogDebug("Next calendar sync in {Minutes} minutes", minInterval);
                    await Task.Delay(TimeSpan.FromMinutes(minInterval), stoppingToken);
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

    private async Task<int> GetMinimumSyncIntervalAsync(CancellationToken cancellationToken)
    {
        var calendars = await _calendarService.GetEnabledCalendarsAsync(cancellationToken);
        if (calendars.Count == 0)
        {
            return _settings.CacheTtlMinutes;
        }

        var minInterval = calendars.Min(c => c.SyncIntervalMinutes);
        return Math.Max(minInterval, 5); // Minimum 5 minutes
    }

    private async Task SyncAllCalendarsAsync(CancellationToken cancellationToken)
    {
        var enabledCalendars = await _calendarService.GetEnabledCalendarsAsync(cancellationToken);

        if (enabledCalendars.Count == 0)
        {
            _logger.LogInformation("No enabled calendars to sync");
            return;
        }

        _logger.LogInformation("Syncing {Count} enabled calendars", enabledCalendars.Count);

        // Sync calendars in parallel for better performance
        var syncTasks = enabledCalendars.Select(calendar =>
            SyncCalendarAsync(calendar, cancellationToken));

        await Task.WhenAll(syncTasks);

        _lastSyncTime = DateTime.UtcNow;
    }

    private async Task SyncCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Syncing calendar: {Name} ({Source})", calendar.Name, calendar.Source);

            // Check if authenticated for Graph calendars
            if (calendar.Source == "Graph")
            {
                var isAuthenticated = await _graphClient.IsAuthenticatedAsync(cancellationToken);
                if (!isAuthenticated)
                {
                    _logger.LogWarning("Not authenticated with Microsoft Graph, skipping calendar {Name}", calendar.Name);
                    return;
                }
            }

            var strategy = _syncStrategies.GetValueOrDefault(calendar.Source);
            if (strategy == null)
            {
                _logger.LogWarning("No sync strategy found for source: {Source}", calendar.Source);
                return;
            }

            var start = calendar.SyncPastEvents ? DateTime.Today.AddDays(-30) : DateTime.Today;
            var end = DateTime.Today.AddDays(calendar.FutureDaysToSync);

            var events = await strategy.FetchEventsAsync(calendar.CalendarId, start, end, cancellationToken);

            await UpsertEventsAsync(calendar, events, cancellationToken);

            // Update last sync time
            calendar.LastSyncUtc = DateTime.UtcNow;
            await _calendarService.UpdateCalendarAsync(calendar, cancellationToken);

            _logger.LogInformation("Synced {Count} events for calendar: {Name}", events.Count, calendar.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync calendar: {Name}", calendar.Name);
        }
    }

    private async Task UpsertEventsAsync(
        CalendarConfiguration calendar,
        List<GraphEvent> events,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Get existing events for this calendar
        var existingEvents = await dbContext.CalendarEvents
            .Where(e => e.CalendarId == calendar.CalendarId)
            .ToDictionaryAsync(e => e.ProviderKey, cancellationToken);

        var updatedCount = 0;
        var addedCount = 0;

        foreach (var graphEvent in events)
        {
            if (existingEvents.TryGetValue(graphEvent.Id, out var existingEvent))
            {
                // Update existing event
                existingEvent.Title = graphEvent.Subject;
                existingEvent.StartUtc = graphEvent.Start;
                existingEvent.EndUtc = graphEvent.End;
                existingEvent.IsAllDay = graphEvent.IsAllDay;
                existingEvent.IsBirthday = graphEvent.IsBirthday;
                existingEvent.UpdatedUtc = DateTime.UtcNow;
                existingEvent.LastSyncUtc = DateTime.UtcNow;
                updatedCount++;
            }
            else
            {
                // Add new event
                var newEvent = new CalendarEvent
                {
                    ProviderKey = graphEvent.Id,
                    CalendarId = calendar.CalendarId,
                    Title = graphEvent.Subject,
                    StartUtc = graphEvent.Start,
                    EndUtc = graphEvent.End,
                    IsAllDay = graphEvent.IsAllDay,
                    IsBirthday = graphEvent.IsBirthday,
                    Source = calendar.Source,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    LastSyncUtc = DateTime.UtcNow
                };
                dbContext.CalendarEvents.Add(newEvent);
                addedCount++;
            }
        }

        // Remove events that no longer exist in the source
        var providerKeys = events.Select(e => e.Id).ToHashSet();
        var eventsToRemove = existingEvents.Values
            .Where(e => !providerKeys.Contains(e.ProviderKey))
            .ToList();

        if (eventsToRemove.Count > 0)
        {
            dbContext.CalendarEvents.RemoveRange(eventsToRemove);
            _logger.LogDebug("Removed {Count} deleted events from calendar {Name}",
                eventsToRemove.Count, calendar.Name);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Upserted events for {CalendarName} - Added: {Added}, Updated: {Updated}, Removed: {Removed}",
            calendar.Name, addedCount, updatedCount, eventsToRemove.Count);
    }

    /// <summary>
    /// Manually trigger a sync (useful for testing or immediate refresh)
    /// </summary>
    public async Task TriggerSyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual calendar sync triggered");
        await SyncAllCalendarsAsync(cancellationToken);
    }

    /// <summary>
    /// Get the last sync time
    /// </summary>
    public DateTime GetLastSyncTime() => _lastSyncTime;
}
