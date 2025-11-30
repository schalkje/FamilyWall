using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services;

namespace FamilyWall.Services.Tests;

public class CalendarEventServiceTests : IDisposable
{
    private readonly Mock<IDbContextFactory<AppDbContext>> _mockContextFactory;
    private readonly Mock<ILogger<CalendarEventService>> _mockLogger;
    private readonly CalendarEventService _service;
    private readonly AppDbContext _context;

    public CalendarEventServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _mockContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
        _mockContextFactory
            .Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_context);

        _mockLogger = new Mock<ILogger<CalendarEventService>>();
        _service = new CalendarEventService(_mockContextFactory.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Don't dispose _context here - IDbContextFactory manages its own disposal
        // _context.Database.EnsureDeleted();
        // _context.Dispose();
    }

    private async Task SeedTestDataAsync()
    {
        var calendar1 = new CalendarConfiguration
        {
            CalendarId = "cal1",
            Name = "Work Calendar",
            Source = "Graph",
            Color = "#FF0000",
            IsEnabled = true
        };

        var calendar2 = new CalendarConfiguration
        {
            CalendarId = "cal2",
            Name = "Personal Calendar",
            Source = "Graph",
            Color = "#00FF00",
            IsEnabled = false // Disabled calendar
        };

        _context.CalendarConfigurations.AddRange(calendar1, calendar2);

        var events = new[]
        {
            // Enabled calendar events
            new CalendarEvent
            {
                CalendarId = "cal1",
                ProviderKey = "evt1",
                Title = "Team Meeting",
                StartUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(10),
                EndUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(11),
                Source = "Graph",
                IsAllDay = false,
                Location = "Conference Room A",
                Description = "Weekly team sync"
            },
            new CalendarEvent
            {
                CalendarId = "cal1",
                ProviderKey = "evt2",
                Title = "Project Deadline",
                StartUtc = DateTime.UtcNow.Date.AddDays(7),
                EndUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(1),
                Source = "Graph",
                IsAllDay = true
            },
            new CalendarEvent
            {
                CalendarId = "cal1",
                ProviderKey = "evt3",
                Title = "John's Birthday",
                StartUtc = DateTime.UtcNow.Date.AddDays(3),
                EndUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(1),
                Source = "Graph",
                IsAllDay = true,
                IsBirthday = true
            },
            // Disabled calendar event (should be filtered out)
            new CalendarEvent
            {
                CalendarId = "cal2",
                ProviderKey = "evt4",
                Title = "Personal Event",
                StartUtc = DateTime.UtcNow.Date.AddDays(2),
                EndUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(1),
                Source = "Graph",
                IsAllDay = false
            },
            // Recurring event
            new CalendarEvent
            {
                CalendarId = "cal1",
                ProviderKey = "evt5",
                Title = "Daily Standup",
                StartUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(9),
                EndUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(9).AddMinutes(15),
                Source = "Graph",
                IsRecurring = true,
                RecurrenceRule = "FREQ=DAILY;COUNT=30"
            }
        };

        _context.CalendarEvents.AddRange(events);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsOnlyEnabledCalendarEvents()
    {
        // Arrange
        await SeedTestDataAsync();
        var start = DateTime.UtcNow.Date;
        var end = DateTime.UtcNow.Date.AddDays(30);

        // Act
        var result = await _service.GetEventsAsync(start, end);

        // Assert
        result.Should().HaveCount(4); // Only cal1 events (cal2 is disabled)
        result.Should().AllSatisfy(e => e.CalendarId.Should().Be("cal1"));
        result.Should().NotContain(e => e.Title == "Personal Event");
    }

    [Fact]
    public async Task GetEventsByDateAsync_ReturnsEventsForSpecificDate()
    {
        // Arrange
        await SeedTestDataAsync();
        var targetDate = DateTime.UtcNow.Date.AddDays(1);

        // Act
        var result = await _service.GetEventsByDateAsync(targetDate);

        // Assert
        result.Should().HaveCount(2); // Team Meeting and Daily Standup
        result.Should().Contain(e => e.Title == "Team Meeting");
        result.Should().Contain(e => e.Title == "Daily Standup");
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsNextNEvents()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetUpcomingEventsAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(e => e.StartUtc);
    }

    [Fact]
    public async Task SearchEventsAsync_FindsEventsByTitle()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.SearchEventsAsync("meeting");

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Team Meeting");
    }

    [Fact]
    public async Task GetBirthdaysAsync_ReturnsOnlyBirthdayEvents()
    {
        // Arrange
        await SeedTestDataAsync();
        var start = DateTime.UtcNow.Date;
        var end = DateTime.UtcNow.Date.AddDays(30);

        // Act
        var result = await _service.GetBirthdaysAsync(start, end);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("John's Birthday");
        result[0].IsBirthday.Should().BeTrue();
    }

    [Fact]
    public async Task GetRecurringEventsAsync_ReturnsOnlyRecurringEvents()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetRecurringEventsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Daily Standup");
        result[0].IsRecurring.Should().BeTrue();
        result[0].RecurrenceRule.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateEventAsync_AddsNewEvent()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "cal1",
            Name = "Test Calendar",
            Source = "Graph",
            IsEnabled = true
        };
        _context.CalendarConfigurations.Add(calendar);
        await _context.SaveChangesAsync();

        var newEvent = new CalendarEvent
        {
            CalendarId = "cal1",
            ProviderKey = "new-evt",
            Title = "New Event",
            StartUtc = DateTime.UtcNow.Date.AddDays(5),
            EndUtc = DateTime.UtcNow.Date.AddDays(5).AddHours(1),
            Source = "Graph"
        };

        // Act
        var created = await _service.CreateEventAsync(newEvent);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().BeGreaterThan(0);
        created.Title.Should().Be("New Event");

        var saved = await _context.CalendarEvents
            .FirstOrDefaultAsync(e => e.ProviderKey == "new-evt");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteEventAsync_RemovesEvent()
    {
        // Arrange
        await SeedTestDataAsync();
        var eventToDelete = await _context.CalendarEvents.FirstAsync(e => e.ProviderKey == "evt1");

        // Act
        await _service.DeleteEventAsync(eventToDelete.Id);

        // Assert
        var deleted = await _context.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == eventToDelete.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetEventCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await SeedTestDataAsync();
        var start = DateTime.UtcNow.Date;
        var end = DateTime.UtcNow.Date.AddDays(30);

        // Act
        var count = await _service.GetEventCountAsync(start, end);

        // Assert
        count.Should().Be(4); // Only enabled calendar events
    }

    [Fact]
    public async Task LoadsCalendarNavigationProperty()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var events = await _service.GetEventsAsync(
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(30));

        // Assert
        events.Should().NotBeEmpty();
        events.Should().AllSatisfy(e =>
        {
            e.Calendar.Should().NotBeNull();
            e.Calendar!.Name.Should().NotBeNullOrEmpty();
            e.Calendar.Color.Should().NotBeNullOrEmpty();
        });
    }
}
