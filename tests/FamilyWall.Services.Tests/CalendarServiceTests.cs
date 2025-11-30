using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Integrations.Graph;
using FamilyWall.Services;

namespace FamilyWall.Services.Tests;

public class CalendarServiceTests : IDisposable
{
    private readonly Mock<IDbContextFactory<AppDbContext>> _mockContextFactory;
    private readonly Mock<IGraphClient> _mockGraphClient;
    private readonly Mock<ILogger<CalendarService>> _mockLogger;
    private readonly CalendarService _service;
    private readonly AppDbContext _context;

    public CalendarServiceTests()
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

        _mockGraphClient = new Mock<IGraphClient>();
        _mockLogger = new Mock<ILogger<CalendarService>>();
        _service = new CalendarService(_mockContextFactory.Object, _mockGraphClient.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Don't dispose _context here - IDbContextFactory manages its own disposal
        // _context.Database.EnsureDeleted();
        // _context.Dispose();
    }

    [Fact]
    public async Task DiscoverCalendarsAsync_WithGraphSource_ReturnsCalendarsWithColors()
    {
        // Arrange
        var graphCalendars = new List<GraphCalendar>
        {
            new GraphCalendar(
                Id: "cal1",
                Name: "Work Calendar",
                Owner: "user@example.com",
                CanEdit: true,
                IsDefaultCalendar: true
            ),
            new GraphCalendar(
                Id: "cal2",
                Name: "Personal Calendar",
                Owner: "user@example.com",
                CanEdit: true,
                IsDefaultCalendar: false
            )
        };

        _mockGraphClient
            .Setup(x => x.GetCalendarsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(graphCalendars);

        // Act
        var result = await _service.DiscoverCalendarsAsync("Graph");

        // Assert
        result.Should().HaveCount(2);
        result[0].CalendarId.Should().Be("cal1");
        result[0].Name.Should().Be("Work Calendar");
        result[0].Source.Should().Be("Graph");
        result[0].Color.Should().NotBeNullOrEmpty();

        result[1].CalendarId.Should().Be("cal2");
        result[1].Name.Should().Be("Personal Calendar");

        // Colors should be hex format
        result.Select(c => c.Color).Should().AllSatisfy(color =>
            color.Should().MatchRegex("^#[0-9A-F]{6}$"));
    }

    [Fact]
    public async Task AddCalendarAsync_AddsCalendarToDatabase()
    {
        // Arrange
        var calendar = new CalendarConfiguration
        {
            CalendarId = "cal1",
            Name = "Test Calendar",
            Source = "Graph",
            Color = "#FF5733",
            IsEnabled = true
        };

        // Act
        await _service.AddCalendarAsync(calendar);

        // Assert
        var saved = await _context.CalendarConfigurations
            .FirstOrDefaultAsync(c => c.CalendarId == "cal1");

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Test Calendar");
        saved.Color.Should().Be("#FF5733");
        saved.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsAllCalendars()
    {
        // Arrange
        var calendars = new[]
        {
            new CalendarConfiguration
            {
                CalendarId = "cal1",
                Name = "Calendar 1",
                Source = "Graph",
                DisplayOrder = 1,
                IsEnabled = true
            },
            new CalendarConfiguration
            {
                CalendarId = "cal2",
                Name = "Calendar 2",
                Source = "Graph",
                DisplayOrder = 2,
                IsEnabled = false
            }
        };

        _context.CalendarConfigurations.AddRange(calendars);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCalendarsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(c => c.DisplayOrder);
    }

    [Fact]
    public async Task GetEnabledCalendarsAsync_ReturnsOnlyEnabledCalendars()
    {
        // Arrange
        var calendars = new[]
        {
            new CalendarConfiguration
            {
                CalendarId = "cal1",
                Name = "Enabled Calendar",
                Source = "Graph",
                IsEnabled = true
            },
            new CalendarConfiguration
            {
                CalendarId = "cal2",
                Name = "Disabled Calendar",
                Source = "Graph",
                IsEnabled = false
            }
        };

        _context.CalendarConfigurations.AddRange(calendars);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetEnabledCalendarsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Enabled Calendar");
        result[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetCalendarByCalendarIdAsync_ReturnsCorrectCalendar()
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

        // Act
        var result = await _service.GetCalendarByCalendarIdAsync("cal1");

        // Assert
        result.Should().NotBeNull();
        result!.CalendarId.Should().Be("cal1");
        result.Name.Should().Be("Test Calendar");
    }
}
