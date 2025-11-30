using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;

namespace FamilyWall.Services.Tests;

/// <summary>
/// Integration tests to verify database schema is correct and all columns exist.
/// This addresses the SQLite error: 'no such column: c.Attendees'
/// </summary>
public class DatabaseSchemaTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public DatabaseSchemaTests()
    {
        // Use SQLite in-memory database to test actual schema
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CalendarEvent_WithAllNewProperties_CanBeSavedAndRetrieved()
    {
        // Arrange - Create calendar first
        var calendar = new CalendarConfiguration
        {
            CalendarId = "test-cal",
            Name = "Test Calendar",
            Source = "Graph",
            IsEnabled = true
        };
        _context.CalendarConfigurations.Add(calendar);
        await _context.SaveChangesAsync();

        // Arrange - Create event with all new properties that were causing the SQLite error
        var testEvent = new CalendarEvent
        {
            CalendarId = "test-cal",
            ProviderKey = "test-evt-1",
            Title = "Test Event",
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddHours(1),
            Source = "Graph",
            IsAllDay = false,
            IsBirthday = false,
            Description = "Test Description",
            Location = "Test Location",
            IsRecurring = false,
            RecurrenceRule = "FREQ=DAILY",
            Organizer = "organizer@test.com",
            Attendees = new List<string> { "attendee1@test.com", "attendee2@test.com" }, // THIS WAS THE COLUMN CAUSING THE ERROR
            Status = EventStatus.Confirmed,
            ResponseStatus = EventResponseStatus.Accepted,
            OnlineMeetingUrl = "https://teams.microsoft.com/test",
            LastSyncUtc = DateTime.UtcNow
        };

        // Act
        _context.CalendarEvents.Add(testEvent);
        var saveAction = async () => await _context.SaveChangesAsync();

        // Assert - Should not throw "no such column" error
        await saveAction.Should().NotThrowAsync();

        // Verify all properties were saved
        var saved = await _context.CalendarEvents
            .Include(e => e.Calendar)
            .FirstOrDefaultAsync(e => e.ProviderKey == "test-evt-1");

        saved.Should().NotBeNull();
        saved!.Attendees.Should().NotBeNull();
        saved.Attendees.Should().HaveCount(2);
        saved.Organizer.Should().Be("organizer@test.com");
        saved.OnlineMeetingUrl.Should().Be("https://teams.microsoft.com/test");
        saved.LastSyncUtc.Should().NotBeNull();
        saved.Calendar.Should().NotBeNull();
        saved.Calendar!.Name.Should().Be("Test Calendar");
    }

    [Fact]
    public async Task CalendarConfiguration_WithAllProperties_CanBeSavedAndRetrieved()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "test-cal-2",
            Name = "Test Calendar 2",
            Source = "Graph",
            Owner = "owner@test.com",
            Color = "#FF0000",
            IsEnabled = true,
            CanEdit = true,
            Visibility = CalendarVisibility.Private,
            SyncIntervalMinutes = 15,
            FutureDaysToSync = 90,
            DisplayOrder = 1,
            LastSyncUtc = DateTime.UtcNow
        };

        // Act
        _context.CalendarConfigurations.Add(calendar);
        var saveAction = async () => await _context.SaveChangesAsync();

        // Assert - Should not throw
        await saveAction.Should().NotThrowAsync();

        var saved = await _context.CalendarConfigurations
            .FirstOrDefaultAsync(c => c.CalendarId == "test-cal-2");

        saved.Should().NotBeNull();
        saved!.Owner.Should().Be("owner@test.com");
        saved.Color.Should().Be("#FF0000");
        saved.SyncIntervalMinutes.Should().Be(15);
        saved.FutureDaysToSync.Should().Be(90);
    }

    [Fact]
    public async Task CalendarEvent_AttendeesColumn_SupportsJsonSerialization()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "test-cal-3",
            Name = "Test Calendar 3",
            Source = "Graph",
            IsEnabled = true
        };
        _context.CalendarConfigurations.Add(calendar);
        await _context.SaveChangesAsync();

        var attendees = new List<string>
        {
            "alice@example.com",
            "bob@example.com",
            "charlie@example.com"
        };

        var calendarEvent = new CalendarEvent
        {
            CalendarId = "test-cal-3",
            ProviderKey = "evt-attendees-test",
            Title = "Meeting with Attendees",
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddHours(1),
            Source = "Graph",
            Attendees = attendees
        };

        // Act
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Clear tracking to force a fresh read
        _context.ChangeTracker.Clear();

        // Assert
        var saved = await _context.CalendarEvents
            .FirstOrDefaultAsync(e => e.ProviderKey == "evt-attendees-test");

        saved.Should().NotBeNull();
        saved!.Attendees.Should().NotBeNull();
        saved.Attendees.Should().HaveCount(3);
        saved.Attendees.Should().ContainInOrder("alice@example.com", "bob@example.com", "charlie@example.com");
    }

    [Fact]
    public async Task CalendarEvent_NavigationProperty_WorksCorrectly()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "cal-nav-test",
            Name = "Navigation Test Calendar",
            Source = "Graph",
            Color = "#00FF00",
            IsEnabled = true
        };

        var calendarEvent = new CalendarEvent
        {
            CalendarId = "cal-nav-test",
            ProviderKey = "evt-nav-test",
            Title = "Event with Navigation",
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddHours(1),
            Source = "Graph"
        };

        _context.CalendarConfigurations.Add(calendar);
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Act
        var savedEvent = await _context.CalendarEvents
            .Include(e => e.Calendar)
            .FirstOrDefaultAsync(e => e.ProviderKey == "evt-nav-test");

        // Assert
        savedEvent.Should().NotBeNull();
        savedEvent!.Calendar.Should().NotBeNull();
        savedEvent.Calendar!.Name.Should().Be("Navigation Test Calendar");
        savedEvent.Calendar.Color.Should().Be("#00FF00");
    }

    [Fact]
    public async Task CalendarConfiguration_CascadeDelete_DeletesEvents()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "cal-cascade-test",
            Name = "Cascade Test Calendar",
            Source = "Graph",
            IsEnabled = true
        };

        var events = new[]
        {
            new CalendarEvent
            {
                CalendarId = "cal-cascade-test",
                ProviderKey = "evt-cascade-1",
                Title = "Event 1",
                StartUtc = DateTime.UtcNow,
                EndUtc = DateTime.UtcNow.AddHours(1),
                Source = "Graph"
            },
            new CalendarEvent
            {
                CalendarId = "cal-cascade-test",
                ProviderKey = "evt-cascade-2",
                Title = "Event 2",
                StartUtc = DateTime.UtcNow.AddDays(1),
                EndUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
                Source = "Graph"
            }
        };

        _context.CalendarConfigurations.Add(calendar);
        _context.CalendarEvents.AddRange(events);
        await _context.SaveChangesAsync();

        // Act - Delete calendar
        _context.CalendarConfigurations.Remove(calendar);
        await _context.SaveChangesAsync();

        // Assert - Events should be deleted too (cascade)
        var remainingEvents = await _context.CalendarEvents
            .Where(e => e.CalendarId == "cal-cascade-test")
            .ToListAsync();

        remainingEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumProperties_AreStoredCorrectly()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "test-cal-enum",
            Name = "Enum Test Calendar",
            Source = "Graph",
            IsEnabled = true
        };
        _context.CalendarConfigurations.Add(calendar);
        await _context.SaveChangesAsync();

        var calendarEvent = new CalendarEvent
        {
            CalendarId = "test-cal-enum",
            ProviderKey = "evt-enum-test",
            Title = "Enum Test Event",
            StartUtc = DateTime.UtcNow,
            EndUtc = DateTime.UtcNow.AddHours(1),
            Source = "Graph",
            Status = EventStatus.Tentative,
            ResponseStatus = EventResponseStatus.Declined
        };

        // Act
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        var saved = await _context.CalendarEvents
            .FirstOrDefaultAsync(e => e.ProviderKey == "evt-enum-test");

        // Assert
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(EventStatus.Tentative);
        saved.ResponseStatus.Should().Be(EventResponseStatus.Declined);
    }
}
