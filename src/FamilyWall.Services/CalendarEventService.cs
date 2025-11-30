using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Services;

public class CalendarEventService : ICalendarEventService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<CalendarEventService> _logger;

    public CalendarEventService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<CalendarEventService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<CalendarEvent?> GetEventAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetEventsAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.StartUtc >= startUtc && e.StartUtc <= endUtc)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetEventsForCalendarAsync(
        string calendarId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.CalendarId == calendarId)
            .Where(e => e.StartUtc >= startUtc && e.StartUtc <= endUtc)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetEventsByDateAsync(
        DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.StartUtc >= startOfDay && e.StartUtc < endOfDay)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetUpcomingEventsAsync(
        int count, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.StartUtc >= now)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> SearchEventsAsync(
        string query, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var lowerQuery = query.ToLower();
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.Title.ToLower().Contains(lowerQuery) ||
                       (e.Description != null && e.Description.ToLower().Contains(lowerQuery)) ||
                       (e.Location != null && e.Location.ToLower().Contains(lowerQuery)))
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetEventsByStatusAsync(
        EventStatus status, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.Status == status)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetRecurringEventsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.IsRecurring)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CalendarEvent>> GetBirthdaysAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.IsBirthday)
            .Where(e => e.StartUtc >= startUtc && e.StartUtc <= endUtc)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<CalendarEvent> CreateEventAsync(
        CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        calendarEvent.CreatedUtc = DateTime.UtcNow;
        calendarEvent.UpdatedUtc = DateTime.UtcNow;

        dbContext.CalendarEvents.Add(calendarEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created event: {Title} (ID: {Id})", calendarEvent.Title, calendarEvent.Id);
        return calendarEvent;
    }

    public async Task<CalendarEvent> UpdateEventAsync(
        CalendarEvent calendarEvent, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        calendarEvent.UpdatedUtc = DateTime.UtcNow;

        dbContext.CalendarEvents.Update(calendarEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated event: {Title} (ID: {Id})", calendarEvent.Title, calendarEvent.Id);
        return calendarEvent;
    }

    public async Task DeleteEventAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendarEvent = await dbContext.CalendarEvents.FindAsync(new object[] { id }, cancellationToken);
        if (calendarEvent != null)
        {
            dbContext.CalendarEvents.Remove(calendarEvent);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted event: {Title} (ID: {Id})", calendarEvent.Title, calendarEvent.Id);
        }
    }

    public async Task UpdateResponseStatusAsync(
        int eventId, EventResponseStatus status, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var calendarEvent = await dbContext.CalendarEvents.FindAsync(new object[] { eventId }, cancellationToken);
        if (calendarEvent != null)
        {
            calendarEvent.ResponseStatus = status;
            calendarEvent.UpdatedUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated event {Title} response status: {Status}",
                calendarEvent.Title, status);
        }
    }

    public async Task<int> GetEventCountAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.CalendarEvents
            .Where(e => e.StartUtc >= startUtc && e.StartUtc <= endUtc)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetEventCountByCalendarAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var counts = await dbContext.CalendarEvents
            .Include(e => e.Calendar)
            .Where(e => e.StartUtc >= startUtc && e.StartUtc <= endUtc)
            .Where(e => e.Calendar != null && e.Calendar.IsEnabled)
            .GroupBy(e => e.Calendar!.Name)
            .Select(g => new { CalendarName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.CalendarName, x => x.Count);
    }
}
