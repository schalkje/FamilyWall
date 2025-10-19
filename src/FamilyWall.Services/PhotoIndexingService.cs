using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyWall.Services;

/// <summary>
/// Background service that indexes photos from configured sources.
/// Stub implementation - will scan NAS/OneDrive and populate the Media table.
/// </summary>
public class PhotoIndexingService : BackgroundService
{
    private readonly ILogger<PhotoIndexingService> _logger;

    public PhotoIndexingService(ILogger<PhotoIndexingService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PhotoIndexingService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("PhotoIndexingService: checking for new photos...");

                // TODO: Implement photo scanning logic:
                // 1. Read PhotoSettings from IOptions
                // 2. Scan each enabled source (NAS/OneDrive/Local)
                // 3. Extract EXIF metadata using MetadataExtractor
                // 4. Compute SHA1 hash for deduplication
                // 5. Insert/update Media records in AppDbContext
                // 6. Persist last scanned cursor for incremental indexing

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PhotoIndexingService encountered an error");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("PhotoIndexingService stopped.");
    }
}
