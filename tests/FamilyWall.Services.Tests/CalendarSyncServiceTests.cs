using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FamilyWall.Core.Models;
using FamilyWall.Core.Settings;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services;
using FamilyWall.Integrations.Graph;

namespace FamilyWall.Services.Tests;

/// <summary>
/// Basic tests for CalendarSyncService to verify it can be instantiated
/// and has the correct dependencies. Full integration testing would require
/// running the actual background service.
/// </summary>
public class CalendarSyncServiceTests : IDisposable
{
    private readonly Mock<IDbContextFactory<AppDbContext>> _mockContextFactory;
    private readonly Mock<ICalendarService> _mockCalendarService;
    private readonly Mock<IGraphClient> _mockGraphClient;
    private readonly Mock<ILogger<CalendarSyncService>> _mockLogger;
    private readonly Mock<ISyncStrategy> _mockSyncStrategy;
    private readonly Mock<IOptions<AppSettings>> _mockAppSettings;
    private readonly AppDbContext _context;

    public CalendarSyncServiceTests()
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

        _mockCalendarService = new Mock<ICalendarService>();
        _mockGraphClient = new Mock<IGraphClient>();
        _mockLogger = new Mock<ILogger<CalendarSyncService>>();
        _mockSyncStrategy = new Mock<ISyncStrategy>();

        // Setup app settings
        var appSettings = new AppSettings
        {
            Calendar = new CalendarSettings
            {
                CacheTtlMinutes = 15,
                ShowBirthdays = true
            }
        };
        _mockAppSettings = new Mock<IOptions<AppSettings>>();
        _mockAppSettings.Setup(x => x.Value).Returns(appSettings);

        _mockSyncStrategy.Setup(x => x.Source).Returns("Graph");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void CalendarSyncService_CanBeInstantiated()
    {
        // Act
        var service = new CalendarSyncService(
            _mockCalendarService.Object,
            _mockGraphClient.Object,
            _mockContextFactory.Object,
            _mockAppSettings.Object,
            _mockLogger.Object,
            new[] { _mockSyncStrategy.Object });

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task CalendarSyncService_CanStart()
    {
        // Arrange
        var service = new CalendarSyncService(
            _mockCalendarService.Object,
            _mockGraphClient.Object,
            _mockContextFactory.Object,
            _mockAppSettings.Object,
            _mockLogger.Object,
            new[] { _mockSyncStrategy.Object });

        // Mock calendar service to return empty list
        _mockCalendarService
            .Setup(x => x.GetEnabledCalendarsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarConfiguration>());

        // Act & Assert - should not throw
        var action = async () => await service.StartAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CalendarSyncService_CanStop()
    {
        // Arrange
        var service = new CalendarSyncService(
            _mockCalendarService.Object,
            _mockGraphClient.Object,
            _mockContextFactory.Object,
            _mockAppSettings.Object,
            _mockLogger.Object,
            new[] { _mockSyncStrategy.Object });

        // Act & Assert - should not throw
        var action = async () => await service.StopAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();
    }
}
