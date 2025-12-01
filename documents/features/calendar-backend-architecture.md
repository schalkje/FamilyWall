# Calendar Backend Architecture & UI Feature Specification

## Overview

This document outlines the comprehensive backend architecture for the FamilyWall calendar feature, including multi-calendar support, event management, synchronization, and UI/UX specifications inspired by industry-leading calendar applications (Outlook and Google Calendar).

## Table of Contents

1. [Backend Architecture](#backend-architecture)
2. [Database Schema](#database-schema)
3. [Calendar Management](#calendar-management)
4. [Event Management](#event-management)
5. [Synchronization Engine](#synchronization-engine)
6. [UI/UX Specifications](#uiux-specifications)
7. [API Endpoints](#api-endpoints)
8. [Performance & Optimization](#performance--optimization)
9. [Security Considerations](#security-considerations)

---

## Backend Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ Calendar UI  │  │  Settings UI │  │  Event Details   │  │
│  │  (Blazor)    │  │   (Blazor)   │  │     (Blazor)     │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         CalendarSyncService (Background)             │   │
│  │  - Periodic sync from external calendars             │   │
│  │  - Cache management with TTL                         │   │
│  │  - Event deduplication & merging                     │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         CalendarService (Business Logic)             │   │
│  │  - CRUD operations for calendar events               │   │
│  │  - Calendar configuration management                 │   │
│  │  - Event filtering & querying                        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                  Integration Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │   Graph      │  │   Google     │  │   ICS/iCal       │  │
│  │  Integration │  │  Integration │  │   Integration    │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                   Data Access Layer                          │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              AppDbContext (EF Core)                  │   │
│  │  - CalendarEvents                                    │   │
│  │  - CalendarConfigurations                            │   │
│  │  - CalendarSubscriptions                             │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                     SQLite Database                          │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

- **FamilyWall.Core**: Domain models, settings, interfaces
- **FamilyWall.Services**: Business logic and background services
- **FamilyWall.Infrastructure**: Data access, DbContext, repositories
- **FamilyWall.Integrations.Graph**: Microsoft Graph API integration
- **FamilyWall.Integrations.Google**: Google Calendar API integration (future)
- **FamilyWall.App**: Blazor UI components

---

## Database Schema

### Enhanced Calendar Models

#### CalendarEvent (Existing - Enhanced)

```csharp
public class CalendarEvent
{
    public int Id { get; set; }
    public required string ProviderKey { get; set; }  // Unique ID from provider
    public required string CalendarId { get; set; }   // NEW: Link to source calendar
    public required string Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public bool IsBirthday { get; set; }
    public bool IsRecurring { get; set; }             // NEW: Recurring event flag
    public string? RecurrenceRule { get; set; }       // NEW: iCalendar RRULE format
    public required string Source { get; set; }       // "Graph", "Google", "ICS"
    public string? ETag { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Organizer { get; set; }            // NEW: Event organizer
    public List<string>? Attendees { get; set; }      // NEW: List of attendees
    public EventStatus Status { get; set; }           // NEW: Confirmed, Tentative, Cancelled
    public EventResponseStatus? ResponseStatus { get; set; } // NEW: User's response
    public string? OnlineMeetingUrl { get; set; }     // NEW: Teams/Zoom link
    public string? Color { get; set; }                // NEW: Event color override
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? LastSyncUtc { get; set; }        // NEW: Last sync timestamp

    // Navigation
    public CalendarConfiguration Calendar { get; set; } = null!;
}

public enum EventStatus
{
    Confirmed,
    Tentative,
    Cancelled
}

public enum EventResponseStatus
{
    NotResponded,
    Accepted,
    Declined,
    Tentative
}
```

#### CalendarConfiguration (NEW)

Represents a configured calendar source with display settings.

```csharp
public class CalendarConfiguration
{
    public int Id { get; set; }
    public required string CalendarId { get; set; }    // Unique ID from provider
    public required string Name { get; set; }          // Display name
    public required string Source { get; set; }        // "Graph", "Google", "ICS"
    public string? Owner { get; set; }                 // Owner email/name
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;        // Show/hide calendar
    public bool IsDefault { get; set; }                // Default calendar flag
    public bool CanEdit { get; set; }                  // User has write permissions
    public bool CanShare { get; set; }                 // User can share calendar
    public string Color { get; set; } = "#3788D8";     // Display color (hex)
    public int DisplayOrder { get; set; }              // Sort order in UI
    public bool ShowInAgenda { get; set; } = true;     // Show in agenda view
    public bool ShowInMonthView { get; set; } = true;  // Show in month view
    public bool ShowInWeekView { get; set; } = true;   // Show in week view
    public CalendarVisibility Visibility { get; set; } = CalendarVisibility.Private;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime? LastSyncUtc { get; set; }

    // Sync settings
    public int SyncIntervalMinutes { get; set; } = 15;
    public bool SyncPastEvents { get; set; } = false;
    public int FutureDaysToSync { get; set; } = 90;

    // Navigation
    public List<CalendarEvent> Events { get; set; } = new();
}

public enum CalendarVisibility
{
    Private,      // Only owner can see
    Shared,       // Shared with specific users
    Public        // Anyone with link can view
}
```

#### CalendarSubscription (NEW)

Manages external calendar subscriptions (ICS feeds, etc.).

```csharp
public class CalendarSubscription
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }           // ICS/iCal feed URL
    public required string Source { get; set; }        // "ICS", "Webcal"
    public bool IsEnabled { get; set; } = true;
    public string Color { get; set; } = "#3788D8";
    public int RefreshIntervalMinutes { get; set; } = 60;
    public DateTime? LastFetchUtc { get; set; }
    public string? LastETag { get; set; }
    public bool IsValid { get; set; } = true;          // Feed is accessible
    public string? LastErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
```

### Database Indexes

```csharp
// AppDbContext.OnModelCreating

// CalendarEvent indexes
entity.HasIndex(e => e.CalendarId);
entity.HasIndex(e => new { e.StartUtc, e.EndUtc });
entity.HasIndex(e => new { e.Source, e.CalendarId });
entity.HasIndex(e => e.IsRecurring);
entity.HasIndex(e => e.LastSyncUtc);

// CalendarConfiguration indexes
entity.HasIndex(e => new { e.Source, e.CalendarId }).IsUnique();
entity.HasIndex(e => e.IsEnabled);
entity.HasIndex(e => e.DisplayOrder);

// CalendarSubscription indexes
entity.HasIndex(e => e.Url).IsUnique();
entity.HasIndex(e => e.IsEnabled);
```

---

## Calendar Management

### CalendarService

Handles all calendar-related business logic.

```csharp
public interface ICalendarService
{
    // Calendar discovery
    Task<List<CalendarConfiguration>> DiscoverCalendarsAsync(string source, CancellationToken cancellationToken = default);

    // Calendar CRUD
    Task<CalendarConfiguration> GetCalendarAsync(int id, CancellationToken cancellationToken = default);
    Task<List<CalendarConfiguration>> GetAllCalendarsAsync(CancellationToken cancellationToken = default);
    Task<List<CalendarConfiguration>> GetEnabledCalendarsAsync(CancellationToken cancellationToken = default);
    Task<CalendarConfiguration> AddCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken = default);
    Task<CalendarConfiguration> UpdateCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken = default);
    Task DeleteCalendarAsync(int id, CancellationToken cancellationToken = default);

    // Calendar settings
    Task SetCalendarEnabledAsync(int id, bool enabled, CancellationToken cancellationToken = default);
    Task SetCalendarColorAsync(int id, string color, CancellationToken cancellationToken = default);
    Task ReorderCalendarsAsync(List<int> orderedIds, CancellationToken cancellationToken = default);

    // Bulk operations
    Task EnableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default);
    Task DisableMultipleCalendarsAsync(List<int> ids, CancellationToken cancellationToken = default);

    // Sync operations
    Task SyncCalendarAsync(int id, CancellationToken cancellationToken = default);
    Task SyncAllCalendarsAsync(CancellationToken cancellationToken = default);
}
```

### Implementation Example

```csharp
public class CalendarService : ICalendarService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IGraphClient _graphClient;
    private readonly ILogger<CalendarService> _logger;

    public async Task<List<CalendarConfiguration>> DiscoverCalendarsAsync(
        string source, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering calendars from source: {Source}", source);

        if (source == "Graph")
        {
            var graphCalendars = await _graphClient.GetCalendarsAsync(cancellationToken);

            return graphCalendars.Select(gc => new CalendarConfiguration
            {
                CalendarId = gc.Id,
                Name = gc.Name,
                Source = "Graph",
                Owner = gc.Owner,
                IsDefault = gc.IsDefaultCalendar,
                CanEdit = gc.CanEdit,
                Color = AssignColor(gc.Name),
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            }).ToList();
        }

        throw new NotSupportedException($"Source '{source}' is not supported");
    }

    public async Task<List<CalendarConfiguration>> GetEnabledCalendarsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.CalendarConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    private static string AssignColor(string calendarName)
    {
        // Assign colors based on calendar type/name (like Outlook/Google)
        var colorPalette = new[]
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

        var hash = Math.Abs(calendarName.GetHashCode());
        return colorPalette[hash % colorPalette.Length];
    }
}
```

---

## Event Management

### CalendarEventService

Manages calendar events with advanced querying and filtering.

```csharp
public interface ICalendarEventService
{
    // Event retrieval
    Task<CalendarEvent?> GetEventAsync(int id, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsForCalendarAsync(int calendarId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetEventsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetUpcomingEventsAsync(int count, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> SearchEventsAsync(string query, CancellationToken cancellationToken = default);

    // Event filtering
    Task<List<CalendarEvent>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetRecurringEventsAsync(CancellationToken cancellationToken = default);
    Task<List<CalendarEvent>> GetBirthdaysAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    // Event CRUD (for local/editable events)
    Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    Task<CalendarEvent> UpdateEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(int id, CancellationToken cancellationToken = default);

    // Event response
    Task UpdateResponseStatusAsync(int eventId, EventResponseStatus status, CancellationToken cancellationToken = default);

    // Statistics
    Task<int> GetEventCountAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetEventCountByCalendarAsync(DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
```

---

## Synchronization Engine

### Enhanced CalendarSyncService

```csharp
public class CalendarSyncService : BackgroundService
{
    private readonly ICalendarService _calendarService;
    private readonly ICalendarEventService _eventService;
    private readonly IGraphClient _graphClient;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly CalendarSettings _settings;
    private readonly ILogger<CalendarSyncService> _logger;

    // Sync strategies
    private readonly Dictionary<string, ISyncStrategy> _syncStrategies = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllCalendarsAsync(stoppingToken);

                // Dynamic interval based on settings
                var minInterval = await GetMinimumSyncIntervalAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(minInterval), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in calendar sync loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task SyncAllCalendarsAsync(CancellationToken cancellationToken)
    {
        var enabledCalendars = await _calendarService.GetEnabledCalendarsAsync(cancellationToken);

        _logger.LogInformation("Syncing {Count} enabled calendars", enabledCalendars.Count);

        // Sync calendars in parallel for better performance
        var syncTasks = enabledCalendars.Select(calendar =>
            SyncCalendarAsync(calendar, cancellationToken));

        await Task.WhenAll(syncTasks);
    }

    private async Task SyncCalendarAsync(CalendarConfiguration calendar, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Syncing calendar: {Name} ({Source})", calendar.Name, calendar.Source);

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
                // Update if changed (check ETag)
                if (existingEvent.ETag != graphEvent.ETag)
                {
                    UpdateEventFromSource(existingEvent, graphEvent);
                    updatedCount++;
                }
            }
            else
            {
                // Add new event
                var newEvent = CreateEventFromSource(calendar, graphEvent);
                dbContext.CalendarEvents.Add(newEvent);
                addedCount++;
            }
        }

        // Remove events that no longer exist
        var providerKeys = events.Select(e => e.Id).ToHashSet();
        var eventsToRemove = existingEvents.Values
            .Where(e => !providerKeys.Contains(e.ProviderKey))
            .ToList();

        if (eventsToRemove.Any())
        {
            dbContext.CalendarEvents.RemoveRange(eventsToRemove);
            _logger.LogDebug("Removed {Count} deleted events", eventsToRemove.Count);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Upserted events - Added: {Added}, Updated: {Updated}, Removed: {Removed}",
            addedCount, updatedCount, eventsToRemove.Count);
    }
}
```

### Sync Strategies (Strategy Pattern)

```csharp
public interface ISyncStrategy
{
    string Source { get; }
    Task<List<GraphEvent>> FetchEventsAsync(string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken);
}

public class GraphSyncStrategy : ISyncStrategy
{
    public string Source => "Graph";
    private readonly IGraphClient _graphClient;

    public async Task<List<GraphEvent>> FetchEventsAsync(
        string calendarId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        return await _graphClient.GetCalendarEventsAsync(calendarId, start, end, cancellationToken);
    }
}

// Future: GoogleSyncStrategy, IcsSyncStrategy, etc.
```

---

## UI/UX Specifications

### Design Principles (Inspired by Outlook & Google Calendar)

1. **Clean & Intuitive**: Minimal clutter, focus on events
2. **Color-Coded**: Each calendar has a distinct color
3. **Multi-View Support**: Month, Week, Agenda views
4. **Quick Actions**: Fast access to common operations
5. **Responsive**: Adapts to different screen sizes
6. **Accessible**: Keyboard navigation, screen reader support

### View Modes

#### 1. Month View (Default)

**Layout:**
```
┌────────────────────────────────────────────────────────────┐
│  ◀  January 2025  ▶                          Today  Week   │
├────────────────────────────────────────────────────────────┤
│ Sun │ Mon │ Tue │ Wed │ Thu │ Fri │ Sat                    │
├─────┼─────┼─────┼─────┼─────┼─────┼─────────────────────────┤
│     │     │  1  │  2  │  3  │  4  │  5                     │
│     │     │ 🔵  │     │ 🟢  │     │                        │
├─────┼─────┼─────┼─────┼─────┼─────┼─────────────────────────┤
│  6  │  7  │  8  │  9  │ 10  │ 11  │ 12                     │
│ 🔵🟢│     │ 🔴  │     │     │ 🔵  │                        │
├─────┼─────┼─────┼─────┼─────┼─────┼─────────────────────────┤
│ ... │ ... │ ... │ ... │ ... │ ... │ ...                    │
└────────────────────────────────────────────────────────────┘

Sidebar:
┌──────────────────────────┐
│ My Calendars             │
│ ☑ 🔵 Work Calendar       │
│ ☑ 🟢 Personal            │
│ ☑ 🔴 Family              │
│ ☐ 🟣 Birthdays           │
│                          │
│ [+ Add Calendar]         │
└──────────────────────────┘
```

**Features:**
- Color-coded event dots (max 3 per day)
- Click day to see all events
- Hover shows event preview
- Current day highlighted
- Weekend styling (lighter background)
- Today button to jump to current date

#### 2. Week View

**Layout:**
```
┌────────────────────────────────────────────────────────────┐
│  ◀  Week of Jan 6-12, 2025  ▶                Month  Agenda │
├────┬────────┬────────┬────────┬────────┬────────┬────────┤
│All │        │        │        │        │        │        │
│Day │        │        │        │        │        │        │
├────┼────────┼────────┼────────┼────────┼────────┼────────┤
│8AM │        │┌──────┐│        │        │        │        │
│    │        ││Team  ││        │        │        │        │
├────┤        ││Meet  ││        │┌──────┐│        │        │
│9AM │        │└──────┘│        ││Dentist│        │        │
│    │        │        │        ││       │        │        │
├────┼────────┼────────┼────────┼┴──────┤        │        │
│... │        │        │        │        │        │        │
└────────────────────────────────────────────────────────────┘
```

**Features:**
- Hourly time slots (7 AM - 11 PM default)
- Event blocks sized by duration
- Overlapping events shown side-by-side
- All-day events section at top
- Scroll to current time on load
- Drag to create new events (future)

#### 3. Agenda View

**Layout:**
```
┌────────────────────────────────────────────────────────────┐
│  Agenda                                      Month  Week    │
├────────────────────────────────────────────────────────────┤
│ Today - Friday, January 10, 2025                           │
├────────────────────────────────────────────────────────────┤
│ 🔵 9:00 AM - 10:00 AM   Team Standup                       │
│    Work Calendar        Conference Room A                  │
│                         5 attendees                        │
├────────────────────────────────────────────────────────────┤
│ 🟢 2:00 PM - 3:00 PM    Doctor Appointment                 │
│    Personal             Dr. Smith's Office                 │
│                         123 Main St                        │
├────────────────────────────────────────────────────────────┤
│ Tomorrow - Saturday, January 11, 2025                      │
├────────────────────────────────────────────────────────────┤
│ 🔴 11:00 AM - 1:00 PM   Family Brunch                      │
│    Family Calendar      Mom's House                        │
│                         All day event                      │
├────────────────────────────────────────────────────────────┤
│ Sunday, January 12, 2025                                   │
├────────────────────────────────────────────────────────────┤
│ No events scheduled                                        │
└────────────────────────────────────────────────────────────┘
```

**Features:**
- Chronological list of upcoming events
- Grouped by day
- Shows event details inline
- Color-coded by calendar
- Quick actions (edit, delete, respond)
- Infinite scroll for future dates

### Calendar Management UI

#### Calendar Settings Panel

```
┌────────────────────────────────────────────────────────────┐
│ Calendar Settings                                      [X] │
├────────────────────────────────────────────────────────────┤
│ Connected Calendars                                        │
│                                                            │
│ Microsoft (name@outlook.com)                    [Connected]│
│ ┌──────────────────────────────────────────────────────┐   │
│ │ ☑ 🔵 Work Calendar         Last synced: 2 min ago    │   │
│ │    owner@company.com       45 events                 │   │
│ │    [🎨] [⚙] [🗑]                                      │   │
│ │                                                      │   │
│ │ ☑ 🟢 Personal              Last synced: 2 min ago    │   │
│ │    Default calendar        12 events                 │   │
│ │    [🎨] [⚙] [🗑]                                      │   │
│ │                                                      │   │
│ │ ☐ 🔴 Team Calendar         Last synced: 2 min ago    │   │
│ │    team@company.com        28 events (disabled)      │   │
│ │    [🎨] [⚙] [🗑]                                      │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                            │
│ [+ Discover More Calendars]  [Sync All Now]                │
│                                                            │
├────────────────────────────────────────────────────────────┤
│ Calendar Subscriptions                                     │
│                                                            │
│ ☑ 🟣 US Holidays           Last fetched: 1 hour ago        │
│    https://calendar.google.com/...                         │
│    [🎨] [⚙] [🗑]                                            │
│                                                            │
│ [+ Add Subscription (ICS/iCal URL)]                        │
│                                                            │
├────────────────────────────────────────────────────────────┤
│ Sync Settings                                              │
│                                                            │
│ Sync Interval:  [15 minutes ▼]                             │
│ Sync Past Events: [☐] (Last 30 days)                       │
│ Sync Future Events: [☑] (Next [90▼] days)                  │
│                                                            │
│ [Save Settings]                                            │
└────────────────────────────────────────────────────────────┘
```

**Interactive Elements:**
- **Checkboxes**: Enable/disable calendar visibility
- **Color picker** (🎨): Change calendar color
- **Settings** (⚙): Advanced calendar settings
- **Delete** (🗑): Remove calendar from sync
- **Drag to reorder**: Change display order

#### Calendar Color Picker

```
┌────────────────────────────┐
│ Choose Calendar Color      │
├────────────────────────────┤
│ Preset Colors:             │
│ 🔵 🔴 🟢 🟡 🟣 🟠 🔶 🟤       │
│                            │
│ Custom Color:              │
│ [   #3788D8   ] [Picker]   │
│                            │
│ Preview:                   │
│ ┌────────────────────────┐ │
│ │ 🔵 Sample Event        │ │
│ │    9:00 AM - 10:00 AM  │ │
│ └────────────────────────┘ │
│                            │
│ [Cancel]  [Apply]          │
└────────────────────────────┘
```

### Event Details Panel

```
┌────────────────────────────────────────────────────────────┐
│ Event Details                                          [X] │
├────────────────────────────────────────────────────────────┤
│ Team Standup                                               │
│ 🔵 Work Calendar                                           │
│                                                            │
│ 📅 Friday, January 10, 2025                                │
│ ⏰ 9:00 AM - 10:00 AM (1 hour)                             │
│ 📍 Conference Room A                                       │
│ 🔗 Join Teams Meeting                                      │
│                                                            │
│ 👤 Organizer: manager@company.com                          │
│                                                            │
│ 👥 Attendees (5):                                          │
│    ✓ You (Accepted)                                        │
│    ✓ colleague1@company.com (Accepted)                     │
│    ? colleague2@company.com (No response)                  │
│    ✓ colleague3@company.com (Accepted)                     │
│    ✗ colleague4@company.com (Declined)                     │
│                                                            │
│ 📝 Description:                                            │
│    Daily team standup meeting to discuss progress          │
│    and blockers.                                           │
│                                                            │
│ Your Response:                                             │
│ [Accept] [Tentative] [Decline]                             │
│                                                            │
│ [Edit Event] [Delete Event] [Share]                        │
└────────────────────────────────────────────────────────────┘
```

### Best Practices Implemented

#### From Outlook Calendar
- **Categorization**: Color-coded calendars
- **Meeting Details**: Attendees, response status, online meeting links
- **Quick Actions**: Inline actions for common tasks
- **Sidebar**: Calendar list with checkboxes

#### From Google Calendar
- **Material Design**: Clean, modern UI
- **Event Density**: Multiple events per day view
- **Agenda View**: List-based upcoming events
- **Color Palette**: Wide range of preset colors
- **Search**: Quick event search functionality

#### Additional Features
- **Keyboard Shortcuts**: Navigate between dates, views
- **Drag & Drop**: Reorder calendars, move events (future)
- **Smart Suggestions**: Auto-complete for locations, attendees
- **Notifications**: Upcoming event reminders
- **Offline Support**: Cached events available offline

---

## API Endpoints

### RESTful API Design (Future Enhancement)

```csharp
// Calendar Management
GET    /api/calendars                  // Get all calendars
GET    /api/calendars/{id}             // Get specific calendar
POST   /api/calendars                  // Add new calendar
PUT    /api/calendars/{id}             // Update calendar
DELETE /api/calendars/{id}             // Delete calendar
POST   /api/calendars/discover         // Discover available calendars
POST   /api/calendars/{id}/sync        // Sync specific calendar

// Event Management
GET    /api/events                     // Get events (with filters)
GET    /api/events/{id}                // Get specific event
POST   /api/events                     // Create event
PUT    /api/events/{id}                // Update event
DELETE /api/events/{id}                // Delete event
GET    /api/events/upcoming            // Get upcoming events
GET    /api/events/search?q={query}    // Search events

// Event Response
POST   /api/events/{id}/respond        // Respond to event (Accept/Decline)

// Subscriptions
GET    /api/subscriptions              // Get all subscriptions
POST   /api/subscriptions              // Add subscription
DELETE /api/subscriptions/{id}         // Remove subscription
```

---

## Performance & Optimization

### Caching Strategy

```csharp
public class CalendarCacheService
{
    private readonly IMemoryCache _cache;
    private readonly CalendarSettings _settings;

    public async Task<List<CalendarEvent>> GetCachedEventsAsync(
        DateTime start, DateTime end,
        Func<Task<List<CalendarEvent>>> fetchFunc)
    {
        var cacheKey = $"events_{start:yyyyMMdd}_{end:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out List<CalendarEvent>? cachedEvents))
        {
            return cachedEvents!;
        }

        var events = await fetchFunc();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(_settings.CacheTtlMinutes));

        _cache.Set(cacheKey, events, cacheOptions);

        return events;
    }
}
```

### Database Optimization

1. **Indexes**: Proper indexes on frequently queried columns
2. **Pagination**: Limit result sets for large date ranges
3. **Projection**: Select only needed columns
4. **Batching**: Batch insert/update operations
5. **Connection Pooling**: Reuse database connections

### UI Optimization

1. **Virtualization**: Only render visible events
2. **Lazy Loading**: Load events as user scrolls
3. **Debouncing**: Delay search queries
4. **Memoization**: Cache computed values
5. **Web Workers**: Offload heavy computations

---

## Security Considerations

### Authentication & Authorization

```csharp
public class CalendarAuthorizationService
{
    public async Task<bool> CanEditEventAsync(int eventId, string userId)
    {
        var calendarEvent = await _eventService.GetEventAsync(eventId);
        if (calendarEvent == null) return false;

        var calendar = await _calendarService.GetCalendarAsync(calendarEvent.CalendarId);

        return calendar.CanEdit && calendar.Owner == userId;
    }

    public async Task<bool> CanViewCalendarAsync(int calendarId, string userId)
    {
        var calendar = await _calendarService.GetCalendarAsync(calendarId);

        return calendar.Visibility == CalendarVisibility.Public ||
               calendar.Owner == userId ||
               await IsCalendarSharedWithUserAsync(calendarId, userId);
    }
}
```

### Data Protection

1. **Encryption**: Encrypt sensitive calendar data at rest
2. **HTTPS**: All API calls over secure connections
3. **Token Storage**: Securely store OAuth tokens
4. **Rate Limiting**: Prevent API abuse
5. **Input Validation**: Sanitize all user inputs

### Privacy

1. **Data Minimization**: Only sync necessary event fields
2. **Consent**: User approval for calendar access
3. **Deletion**: Permanent removal of calendar data on request
4. **Audit Logs**: Track calendar access and modifications

---

## Implementation Roadmap

### Implementation Status

**Last Updated:** 2025-01-30

**Phase 2 & Data Synchronization Complete!** 🎉

The backend foundation for multi-calendar support is now fully implemented and tested. All services build successfully, and the architecture is ready for UI implementation.

**Key Achievements:**
- 3 new domain models with comprehensive properties
- 2 service layers with 30+ methods for calendar and event management
- Strategy pattern for extensible sync providers
- Complete database schema with optimized indexes
- All projects building without errors

### Phase 1: Core Backend (Current)
- ✅ Basic calendar event model
- ✅ Microsoft Graph integration
- ✅ Calendar sync service
- ✅ Multi-calendar discovery

### Phase 2: Enhanced Backend (MVP Foundation) ✅ COMPLETED
- ✅ CalendarConfiguration model
- ✅ CalendarSubscription model
- ✅ CalendarService implementation
- ✅ CalendarEventService implementation
- ✅ Enhanced sync strategies for Microsoft Graph (GraphSyncStrategy)
- ✅ Event CRUD operations (read-only from Graph)
- ✅ Database schema updates (EnsureCreated handles migrations)

### Phase 3: Core UI Implementation (MVP) ✅ COMPLETED
- ✅ Month view component
- ✅ Week view component
- ✅ Agenda view component
- ✅ Calendar management panel (Outlook calendars only)
- ✅ Event details panel (read-only view)
- ✅ Calendar settings UI (basic)

### Phase 4: Event Creation & Editing (CURRENT PRIORITY)
- [ ] Event details page with full editing capabilities
- [ ] Click event → Navigate to event details
- [ ] Edit event: title, start, end, duration, description
- [ ] Family member selector (who is it for)
- [ ] Day view page with hourly time slots
- [ ] Click day label → Navigate to day view
- [ ] Create event via "New Event" button
- [ ] Create event by clicking time slot in day view
- [ ] Event deletion with confirmation
- [ ] Save changes to Microsoft Graph API

### Phase 5: Essential Features
- [ ] Recurring events support
- [ ] Event filtering and sorting
- [ ] Multiple view persistence (remember user's preferred view)
- [ ] Event notifications
- [ ] Event conflict detection
- [ ] Time zone support

### Phase 6: Polish & Optimization
- [ ] Performance optimization
- [ ] Caching improvements
- [ ] Offline support with sync queue
- [ ] Keyboard shortcuts
- [ ] Accessibility improvements (ARIA, screen readers)
- [ ] Mobile responsiveness
- [ ] Drag & drop event rescheduling
- [ ] Event duplication

### Phase 7: Advanced Features (Future)
- [ ] Event response tracking (Accept/Decline/Tentative)
- [ ] Calendar subscriptions (ICS/iCal feeds)
- [ ] Google Calendar integration
- [ ] Advanced event search
- [ ] Attendee management
- [ ] Calendar sharing with family
- [ ] Advanced recurrence patterns
- [ ] Natural language event creation
- [ ] Event templates
- [ ] Print calendar views

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task GetEnabledCalendars_ReturnsOnlyEnabledCalendars()
{
    // Arrange
    var calendars = new List<CalendarConfiguration>
    {
        new() { Name = "Work", IsEnabled = true },
        new() { Name = "Personal", IsEnabled = true },
        new() { Name = "Disabled", IsEnabled = false }
    };

    // Act
    var result = await _calendarService.GetEnabledCalendarsAsync();

    // Assert
    Assert.Equal(2, result.Count);
    Assert.All(result, c => Assert.True(c.IsEnabled));
}
```

### Integration Tests

```csharp
[Fact]
public async Task SyncCalendar_UpdatesLocalEvents()
{
    // Arrange
    var calendar = CreateTestCalendar();
    var mockEvents = CreateMockGraphEvents(10);

    // Act
    await _syncService.SyncCalendarAsync(calendar);

    // Assert
    var events = await _eventService.GetEventsForCalendarAsync(calendar.Id);
    Assert.Equal(10, events.Count);
}
```

### UI Tests

```csharp
[Fact]
public async Task MonthView_DisplaysCorrectEventCount()
{
    // Arrange
    var ctx = new TestContext();
    var component = ctx.RenderComponent<MonthView>();

    // Act
    var eventDots = component.FindAll(".event-dot");

    // Assert
    Assert.Equal(15, eventDots.Count);
}
```

---

## Monitoring & Logging

### Key Metrics

1. **Sync Performance**: Time to sync each calendar
2. **API Latency**: Response times for Graph API calls
3. **Event Count**: Number of events per calendar
4. **Error Rates**: Sync failures, API errors
5. **Cache Hit Rate**: Effectiveness of caching

### Logging Strategy

```csharp
_logger.LogInformation("Syncing calendar {CalendarName} with {EventCount} events",
    calendar.Name, events.Count);

_logger.LogWarning("Calendar sync took longer than expected: {Duration}ms",
    syncDuration.TotalMilliseconds);

_logger.LogError(ex, "Failed to sync calendar {CalendarId}", calendar.Id);
```

---

## Conclusion

This comprehensive backend architecture provides a solid foundation for a fully-featured calendar application. The design follows industry best practices from Outlook and Google Calendar while maintaining flexibility for future enhancements.

### Key Benefits

✅ **Scalable Architecture**: Clean separation of concerns
✅ **Multi-Calendar Support**: Unlimited calendars from multiple sources
✅ **Efficient Syncing**: Smart sync strategies with caching
✅ **Modern UI/UX**: Intuitive interface inspired by market leaders
✅ **Extensible**: Easy to add new calendar providers
✅ **Performance**: Optimized database queries and caching
✅ **Secure**: Proper authentication and authorization

### Next Steps (MVP Focus)

**Phase 2 - Enhanced Backend:** ✅ COMPLETED
1. ✅ Implemented CalendarConfiguration and related models
2. ✅ Created CalendarService with full CRUD operations
3. ✅ Enhanced sync service for multiple Outlook calendars
4. ✅ Added database schema updates (auto-applied via EnsureCreated)

**Implemented Files (Phase 2):**
- `FamilyWall.Core/Models/CalendarConfiguration.cs` - Calendar source configuration
- `FamilyWall.Core/Models/CalendarSubscription.cs` - ICS/iCal subscription support
- `FamilyWall.Core/Models/CalendarEnums.cs` - Visibility, status, and response enums
- `FamilyWall.Core/Models/CalendarEvent.cs` - Enhanced with new properties
- `FamilyWall.Services/ICalendarService.cs` - Calendar management interface
- `FamilyWall.Services/CalendarService.cs` - Full implementation with 15+ methods
- `FamilyWall.Services/ICalendarEventService.cs` - Event management interface
- `FamilyWall.Services/CalendarEventService.cs` - Full implementation with 15+ methods
- `FamilyWall.Services/ISyncStrategy.cs` - Strategy pattern interface
- `FamilyWall.Services/GraphSyncStrategy.cs` - Microsoft Graph sync implementation
- `FamilyWall.Services/CalendarSyncService.cs` - Updated for multi-calendar architecture
- `FamilyWall.Infrastructure/Data/AppDbContext.cs` - Enhanced with new DbSets and indexes
- `FamilyWall.App/MauiProgram.cs` - Registered all new services in DI

**Phase 3 - Core UI:** ✅ COMPLETED
5. ✅ Built Month, Week, and Agenda view components
6. ✅ Created calendar management panel for Outlook calendars
7. ✅ Implemented event details panel (read-only)
8. ✅ Added calendar settings UI

**Implemented Files (Phase 3):**
- `FamilyWall.App/ViewModels/CalendarViewModel.cs` - View state management
- `FamilyWall.App/Components/Calendar/MonthViewSimple.razor` - Month calendar grid
- `FamilyWall.App/Components/Calendar/WeekView.razor` - Week view with hourly slots
- `FamilyWall.App/Components/Calendar/AgendaView.razor` - Agenda list view
- `FamilyWall.App/Components/Calendar/EventDetailsPanel.razor` - Event details side panel
- `FamilyWall.App/Components/Calendar/CalendarManagementPanel.razor` - Calendar settings panel
- `FamilyWall.App/Components/Pages/Calendar.razor` - Updated with view switching and panels

**Phase 4 - Event Creation & Editing:** ⏳ NEXT PRIORITY
9. Implement event details page with full editing
10. Add day view with hourly time slots
11. Enable event creation (button + click-to-create)
12. Implement family member assignment
13. Add event deletion functionality
14. Integrate with Microsoft Graph API for write operations

**Planning Documents:**
- See `documents/features/PHASE_4_EVENT_CRUD_PLAN.md` for detailed implementation plan
- Includes UI mockups, component architecture, and task breakdown
- Estimated timeline: 8-12 hours of development

**Phase 5 - Essential Features:**
15. Support recurring events display
16. Add event filtering and sorting
17. Implement view preferences persistence
18. Add event conflict detection

This architecture ensures FamilyWall has a professional calendar system with full CRUD capabilities for calendar events, family member assignment, and intuitive UI for day-to-day calendar management. The system is designed for extensibility with a clear path for future enhancements like advanced features, offline support, and third-party integrations.
