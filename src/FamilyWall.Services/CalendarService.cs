using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Integrations.Graph;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Services;

public class CalendarService : ICalendarService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IGraphClient _graphClient;
    private readonly ILogger<CalendarService> _logger;

    private static readonly string[] ColorPalette = new[]
    {
        "#3788D8", // Blue
        "#D83737", // Red
        "#37D875", // Green
        "#D87537", // Orange
        "#8B37D8", // Purple
        "#37D8D8", // Cyan
        "#D8D837", // Yellow
        "#D837A7"  // Pink
    };

    public CalendarService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IGraphClient graphClient,
        ILogger<CalendarService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _graphClient = graphClient;
        _logger = logger;
    }

    public async Task<List<CalendarConfiguration>> DiscoverCalendarsAsync(
        string source, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering calendars from source: {Source}", source);

        if (source == "Graph")
        {
            var graphCalendars = await _graphClient.GetCalendarsAsync(cancellationToken);

            var colorIndex = 0;
            return graphCalendars.Select(gc => new CalendarConfiguration
            {
                CalendarId = gc.Id,
                Name = gc.Name,
                Source = "Graph",
                Owner = gc.Owner,
                IsDefault = gc.IsDefaultCalendar,
                CanEdit = gc.CanEdit,
                Color = ColorPalette[colorIndex++ % ColorPalette.Length],
                DisplayOrder = colorIndex,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            }).ToList();
        }

        throw new NotSupportedException($"Source '{source}' is not supported");
    }

    public async Task<CalendarConfiguration?> GetCalendarAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarConfigurations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<CalendarConfiguration?> GetCalendarByCalendarIdAsync(
        string calendarId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarConfigurations
            .FirstOrDefaultAsync(c => c.CalendarId == calendarId, cancellationToken);
    }

    public async Task<List<CalendarConfiguration>> GetAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarConfigurations
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarConfiguration>> GetEnabledCalendarsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<CalendarConfiguration> AddCalendarAsync(
        CalendarConfiguration calendar, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        calendar.CreatedUtc = DateTime.UtcNow;
        calendar.UpdatedUtc = DateTime.UtcNow;

        dbContext.CalendarConfigurations.Add(calendar);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added calendar: {Name} (ID: {Id})", calendar.Name, calendar.Id);
        return calendar;
    }

    public async Task<CalendarConfiguration> UpdateCalendarAsync(
        CalendarConfiguration calendar, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        calendar.UpdatedUtc = DateTime.UtcNow;

        dbContext.CalendarConfigurations.Update(calendar);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated calendar: {Name} (ID: {Id})", calendar.Name, calendar.Id);
        return calendar;
    }

    public async Task DeleteCalendarAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendar = await dbContext.CalendarConfigurations.FindAsync(new object[] { id }, cancellationToken);
        if (calendar != null)
        {
            dbContext.CalendarConfigurations.Remove(calendar);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted calendar: {Name} (ID: {Id})", calendar.Name, calendar.Id);
        }
    }

    public async Task SetCalendarEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendar = await dbContext.CalendarConfigurations.FindAsync(new object[] { id }, cancellationToken);
        if (calendar != null)
        {
            calendar.IsEnabled = enabled;
            calendar.UpdatedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Set calendar {Name} enabled: {Enabled}", calendar.Name, enabled);
        }
    }

    public async Task SetCalendarColorAsync(int id, string color, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendar = await dbContext.CalendarConfigurations.FindAsync(new object[] { id }, cancellationToken);
        if (calendar != null)
        {
            calendar.Color = color;
            calendar.UpdatedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Set calendar {Name} color: {Color}", calendar.Name, color);
        }
    }

    public async Task ReorderCalendarsAsync(List<int> orderedIds, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var calendar = await dbContext.CalendarConfigurations.FindAsync(new object[] { orderedIds[i] }, cancellationToken);
            if (calendar != null)
            {
                calendar.DisplayOrder = i;
                calendar.UpdatedUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Reordered {Count} calendars", orderedIds.Count);
    }

    public async Task EnableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendars = await dbContext.CalendarConfigurations
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var calendar in calendars)
        {
            calendar.IsEnabled = true;
            calendar.UpdatedUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Enabled {Count} calendars", calendars.Count);
    }

    public async Task DisableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendars = await dbContext.CalendarConfigurations
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var calendar in calendars)
        {
            calendar.IsEnabled = false;
            calendar.UpdatedUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Disabled {Count} calendars", calendars.Count);
    }

    public async Task SyncCalendarAsync(int id, CancellationToken cancellationToken = default)
    {
        var calendar = await GetCalendarAsync(id, cancellationToken);
        if (calendar == null)
        {
            _logger.LogWarning("Calendar with ID {Id} not found", id);
            return;
        }

        _logger.LogInformation("Manual sync requested for calendar: {Name}", calendar.Name);
        // Sync logic will be handled by CalendarSyncService
    }

    public async Task SyncAllCalendarsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual sync requested for all calendars");
        // Sync logic will be handled by CalendarSyncService
        await Task.CompletedTask;
    }
}
