# Calendar Feature - Quick Start Guide

## 🚀 Getting Started with the Calendar Services

This guide shows you how to use the newly implemented calendar services in your Blazor UI components.

---

## 📋 Table of Contents

1. [Service Injection](#service-injection)
2. [Discovering Calendars](#discovering-calendars)
3. [Managing Calendars](#managing-calendars)
4. [Querying Events](#querying-events)
5. [Filtering Events](#filtering-events)
6. [Common Patterns](#common-patterns)
7. [Background Sync](#background-sync)

---

## Service Injection

### In Blazor Components (.razor)

```csharp
@inject ICalendarService CalendarService
@inject ICalendarEventService EventService
```

### In Code-Behind (.razor.cs)

```csharp
public class CalendarPage : ComponentBase
{
    [Inject]
    private ICalendarService CalendarService { get; set; } = null!;

    [Inject]
    private ICalendarEventService EventService { get; set; } = null!;
}
```

---

## Discovering Calendars

### 1. Discover All Microsoft Graph Calendars

```csharp
private async Task DiscoverAndAddCalendarsAsync()
{
    try
    {
        // Discover all calendars from Microsoft Graph
        var discoveredCalendars = await CalendarService.DiscoverCalendarsAsync("Graph");

        // Get existing calendars to avoid duplicates
        var existingCalendars = await CalendarService.GetAllCalendarsAsync();
        var existingIds = existingCalendars.Select(c => c.CalendarId).ToHashSet();

        // Add only new calendars
        foreach (var calendar in discoveredCalendars)
        {
            if (!existingIds.Contains(calendar.CalendarId))
            {
                await CalendarService.AddCalendarAsync(calendar);
            }
        }

        // Refresh UI
        await LoadCalendarsAsync();
    }
    catch (Exception ex)
    {
        // Handle error (show toast, log, etc.)
        Console.WriteLine($"Error discovering calendars: {ex.Message}");
    }
}
```

### 2. Check Current Calendars

```csharp
private async Task<List<CalendarConfiguration>> LoadCalendarsAsync()
{
    // Get all calendars (ordered by DisplayOrder)
    return await CalendarService.GetAllCalendarsAsync();
}

private async Task<List<CalendarConfiguration>> LoadEnabledCalendarsAsync()
{
    // Get only enabled calendars
    return await CalendarService.GetEnabledCalendarsAsync();
}
```

---

## Managing Calendars

### Enable/Disable Calendars

```csharp
private async Task ToggleCalendarAsync(int calendarId, bool isEnabled)
{
    await CalendarService.SetCalendarEnabledAsync(calendarId, isEnabled);
}
```

### Change Calendar Color

```csharp
private async Task ChangeCalendarColorAsync(int calendarId, string hexColor)
{
    await CalendarService.SetCalendarColorAsync(calendarId, hexColor);
}
```

### Reorder Calendars (Drag & Drop)

```csharp
private async Task ReorderCalendarsAsync(List<CalendarConfiguration> calendars)
{
    // Extract IDs in new order
    var orderedIds = calendars.Select(c => c.Id).ToList();

    // Update display order in database
    await CalendarService.ReorderCalendarsAsync(orderedIds);
}
```

### Delete Calendar

```csharp
private async Task DeleteCalendarAsync(int calendarId)
{
    // This will cascade delete all events from this calendar
    await CalendarService.DeleteCalendarAsync(calendarId);
}
```

---

## Querying Events

### Get Events for Month View

```csharp
private async Task<List<CalendarEvent>> GetEventsForMonthAsync(DateTime month)
{
    var startOfMonth = new DateTime(month.Year, month.Month, 1);
    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

    // Gets all events from enabled calendars only
    return await EventService.GetEventsAsync(startOfMonth, endOfMonth);
}
```

### Get Events for Week View

```csharp
private async Task<List<CalendarEvent>> GetEventsForWeekAsync(DateTime weekStart)
{
    var endOfWeek = weekStart.AddDays(7);

    return await EventService.GetEventsAsync(weekStart, endOfWeek);
}
```

### Get Events for Specific Day

```csharp
private async Task<List<CalendarEvent>> GetEventsForDayAsync(DateTime date)
{
    // Gets all events on this day from enabled calendars
    return await EventService.GetEventsByDateAsync(date);
}
```

### Get Upcoming Events (Agenda View)

```csharp
private async Task<List<CalendarEvent>> GetUpcomingEventsAsync(int count = 50)
{
    // Gets next N events starting from now
    return await EventService.GetUpcomingEventsAsync(count);
}
```

### Get Single Event Details

```csharp
private async Task<CalendarEvent?> GetEventDetailsAsync(int eventId)
{
    // Includes navigation to Calendar
    return await EventService.GetEventAsync(eventId);
}
```

---

## Filtering Events

### Search Events

```csharp
private async Task<List<CalendarEvent>> SearchEventsAsync(string query)
{
    // Searches title, description, and location
    return await EventService.SearchEventsAsync(query);
}
```

### Get Birthdays

```csharp
private async Task<List<CalendarEvent>> GetBirthdaysThisMonthAsync()
{
    var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

    return await EventService.GetBirthdaysAsync(startOfMonth, endOfMonth);
}
```

### Get Recurring Events

```csharp
private async Task<List<CalendarEvent>> GetRecurringEventsAsync()
{
    return await EventService.GetRecurringEventsAsync();
}
```

### Get Events by Status

```csharp
private async Task<List<CalendarEvent>> GetConfirmedEventsAsync()
{
    return await EventService.GetEventsByStatusAsync(EventStatus.Confirmed);
}
```

---

## Common Patterns

### Calendar Sidebar Component

```razor
@inject ICalendarService CalendarService

<div class="calendar-sidebar">
    <h3>My Calendars</h3>

    @foreach (var calendar in _calendars)
    {
        <div class="calendar-item">
            <input type="checkbox"
                   checked="@calendar.IsEnabled"
                   @onchange="e => ToggleCalendar(calendar.Id, (bool)e.Value!)" />
            <span class="color-dot" style="background-color: @calendar.Color"></span>
            <span>@calendar.Name</span>
            <span class="event-count">(@calendar.Events.Count)</span>
        </div>
    }

    <button @onclick="DiscoverCalendars">+ Discover More</button>
</div>

@code {
    private List<CalendarConfiguration> _calendars = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadCalendarsAsync();
    }

    private async Task LoadCalendarsAsync()
    {
        _calendars = await CalendarService.GetAllCalendarsAsync();
    }

    private async Task ToggleCalendar(int id, bool enabled)
    {
        await CalendarService.SetCalendarEnabledAsync(id, enabled);
        await LoadCalendarsAsync();

        // Notify parent component to refresh events
        StateHasChanged();
    }

    private async Task DiscoverCalendars()
    {
        var discovered = await CalendarService.DiscoverCalendarsAsync("Graph");

        foreach (var calendar in discovered)
        {
            await CalendarService.AddCalendarAsync(calendar);
        }

        await LoadCalendarsAsync();
    }
}
```

### Month View Component

```razor
@inject ICalendarEventService EventService

<div class="month-view">
    <div class="month-header">
        <button @onclick="PreviousMonth">◀</button>
        <h2>@_currentMonth.ToString("MMMM yyyy")</h2>
        <button @onclick="NextMonth">▶</button>
    </div>

    <div class="calendar-grid">
        @foreach (var day in GetDaysInMonth())
        {
            <div class="calendar-day">
                <div class="day-number">@day.Day</div>
                @foreach (var evt in GetEventsForDay(day))
                {
                    <div class="event-dot"
                         style="background-color: @evt.Calendar?.Color"
                         @onclick="() => ShowEventDetails(evt)">
                    </div>
                }
            </div>
        }
    </div>
</div>

@code {
    private DateTime _currentMonth = DateTime.Today;
    private List<CalendarEvent> _events = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        var startOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        _events = await EventService.GetEventsAsync(startOfMonth, endOfMonth);
    }

    private List<DateTime> GetDaysInMonth()
    {
        var days = new List<DateTime>();
        var startOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);

        for (int i = 0; i < daysInMonth; i++)
        {
            days.Add(startOfMonth.AddDays(i));
        }

        return days;
    }

    private List<CalendarEvent> GetEventsForDay(DateTime day)
    {
        return _events.Where(e => e.StartUtc.Date == day.Date).ToList();
    }

    private async Task PreviousMonth()
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        await LoadEventsAsync();
    }

    private async Task NextMonth()
    {
        _currentMonth = _currentMonth.AddMonths(1);
        await LoadEventsAsync();
    }

    private void ShowEventDetails(CalendarEvent evt)
    {
        // Navigate to event details or show modal
    }
}
```

### Event Statistics Dashboard

```razor
@inject ICalendarEventService EventService

<div class="stats-dashboard">
    <h3>Calendar Statistics</h3>

    <div class="stat-card">
        <h4>Events This Month</h4>
        <p class="stat-number">@_eventCount</p>
    </div>

    <div class="stat-breakdown">
        <h4>Events by Calendar</h4>
        @foreach (var (calendar, count) in _eventsByCalendar)
        {
            <div class="stat-row">
                <span>@calendar</span>
                <span class="count">@count</span>
            </div>
        }
    </div>
</div>

@code {
    private int _eventCount;
    private Dictionary<string, int> _eventsByCalendar = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        _eventCount = await EventService.GetEventCountAsync(startOfMonth, endOfMonth);
        _eventsByCalendar = await EventService.GetEventCountByCalendarAsync(startOfMonth, endOfMonth);
    }
}
```

---

## Background Sync

### Manual Sync Trigger

```csharp
@inject CalendarSyncService SyncService

private async Task TriggerSyncAsync()
{
    await SyncService.TriggerSyncAsync();

    // Show notification
    await ShowToast("Syncing calendars...");

    // Reload events after sync
    await Task.Delay(2000); // Wait for sync to complete
    await LoadEventsAsync();
}
```

### Check Last Sync Time

```csharp
@inject CalendarSyncService SyncService

private DateTime GetLastSyncTime()
{
    return SyncService.GetLastSyncTime();
}
```

### Display Sync Status

```razor
<div class="sync-status">
    <span>Last synced: @FormatSyncTime(SyncService.GetLastSyncTime())</span>
    <button @onclick="TriggerSyncAsync">Sync Now</button>
</div>

@code {
    private string FormatSyncTime(DateTime lastSync)
    {
        if (lastSync == DateTime.MinValue)
            return "Never";

        var timeAgo = DateTime.UtcNow - lastSync;

        if (timeAgo.TotalMinutes < 1)
            return "Just now";
        else if (timeAgo.TotalMinutes < 60)
            return $"{(int)timeAgo.TotalMinutes} minutes ago";
        else if (timeAgo.TotalHours < 24)
            return $"{(int)timeAgo.TotalHours} hours ago";
        else
            return lastSync.ToLocalTime().ToString("g");
    }
}
```

---

## 🎨 Working with Calendar Colors

### Display Events with Calendar Colors

```razor
@foreach (var evt in _events)
{
    <div class="event-item"
         style="border-left: 4px solid @evt.Calendar?.Color">
        <h4>@evt.Title</h4>
        <p>@evt.StartUtc.ToLocalTime().ToString("g")</p>
        <span class="calendar-badge"
              style="background-color: @evt.Calendar?.Color">
            @evt.Calendar?.Name
        </span>
    </div>
}
```

### Color Picker Component

```razor
<div class="color-picker">
    @foreach (var color in PresetColors)
    {
        <button class="color-swatch"
                style="background-color: @color"
                @onclick="() => SelectColor(color)">
        </button>
    }
</div>

@code {
    private static readonly string[] PresetColors = new[]
    {
        "#3788D8", "#D83737", "#37D875", "#D87537",
        "#8B37D8", "#37D8D8", "#D8D837", "#D837A7"
    };

    [Parameter]
    public EventCallback<string> OnColorSelected { get; set; }

    private async Task SelectColor(string color)
    {
        await OnColorSelected.InvokeAsync(color);
    }
}
```

---

## 💡 Tips & Best Practices

### 1. Always Filter by Enabled Calendars
Most `EventService` methods automatically filter by enabled calendars. No need to do it manually.

### 2. Use Navigation Properties
When you fetch events with `GetEventAsync()` or `GetEventsAsync()`, the `Calendar` navigation property is automatically loaded via `.Include()`.

```csharp
var evt = await EventService.GetEventAsync(eventId);
var calendarColor = evt.Calendar?.Color; // Already loaded!
```

### 3. Handle Null Calendars Gracefully
Some events might not have a calendar (if it was deleted):

```csharp
var color = evt.Calendar?.Color ?? "#808080"; // Fallback to gray
```

### 4. Optimize Date Range Queries
Don't load more data than needed:

```csharp
// Good: Only load visible month
var events = await EventService.GetEventsAsync(startOfMonth, endOfMonth);

// Bad: Loading entire year
var events = await EventService.GetEventsAsync(startOfYear, endOfYear);
```

### 5. Cache Calendar List
Calendar list changes infrequently, so cache it:

```csharp
private List<CalendarConfiguration>? _cachedCalendars;

private async Task<List<CalendarConfiguration>> GetCalendarsAsync()
{
    if (_cachedCalendars == null)
    {
        _cachedCalendars = await CalendarService.GetAllCalendarsAsync();
    }
    return _cachedCalendars;
}

private void InvalidateCalendarCache()
{
    _cachedCalendars = null;
}
```

---

## 🐛 Troubleshooting

### Events Not Showing

1. **Check if calendar is enabled:**
   ```csharp
   var calendar = await CalendarService.GetCalendarAsync(calendarId);
   if (!calendar.IsEnabled)
   {
       await CalendarService.SetCalendarEnabledAsync(calendarId, true);
   }
   ```

2. **Verify date range:**
   ```csharp
   var events = await EventService.GetEventsAsync(startDate, endDate);
   Console.WriteLine($"Found {events.Count} events between {startDate:d} and {endDate:d}");
   ```

3. **Check authentication:**
   ```csharp
   var isAuthenticated = await GraphClient.IsAuthenticatedAsync();
   if (!isAuthenticated)
   {
       // Prompt user to sign in
   }
   ```

### Calendar Discovery Returns Empty List

- User might not have any calendars in Microsoft Graph
- Authentication might have failed
- Check Graph API permissions in app settings

### Sync Not Working

1. **Verify calendar exists in database:**
   ```csharp
   var calendars = await CalendarService.GetAllCalendarsAsync();
   if (calendars.Count == 0)
   {
       // Run discovery first
       await DiscoverCalendarsAsync();
   }
   ```

2. **Check sync service logs** in console output

---

## 📚 Next Steps

1. Build Month View component
2. Build Week View component
3. Build Agenda View component
4. Build Calendar Settings panel
5. Build Event Details modal
6. Add color picker for calendars
7. Add search functionality
8. Add filters (birthdays, recurring, etc.)

---

**Happy Coding! 🎉**
