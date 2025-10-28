using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FamilyWall.Core.Settings;
using FamilyWall.Core.Models;
using FamilyWall.Core.Abstractions;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FamilyWall.Services;

/// <summary>
/// Service for scanning and indexing photos from configured sources.
/// This is a regular service (not a hosted background service) that can be called on-demand.
/// </summary>
public class PhotoScanService : IPhotoScanService
{
    private readonly ILogger<PhotoScanService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IOptions<AppSettings> _settings;
    private readonly IIndexingStatusService _indexingStatus;

    public PhotoScanService(
        ILogger<PhotoScanService> logger,
        AppDbContext dbContext,
        IOptions<AppSettings> settings,
        IIndexingStatusService indexingStatus)
    {
        _logger = logger;
        _dbContext = dbContext;
        _settings = settings;
        _indexingStatus = indexingStatus;
    }

    public async Task ScanAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== PhotoScanService: Starting scan ===");

        var photoSettings = _settings.Value.Photos;
        if (photoSettings.Sources == null || !photoSettings.Sources.Any())
        {
            _logger.LogWarning("No photo sources configured");
            _indexingStatus.UpdateStatus(new IndexingStatus
            {
                IsScanning = false,
                TotalFiles = 0,
                ProcessedFiles = 0,
                IndexedPhotos = 0,
                CompletedAt = DateTime.UtcNow
            });
            return;
        }

        var enabledSources = photoSettings.Sources.Where(s => s.Enabled).OrderBy(s => s.Priority).ToList();
        _logger.LogInformation("Found {Count} enabled sources", enabledSources.Count);

        foreach (var source in enabledSources)
        {
            if (string.IsNullOrWhiteSpace(source.Path))
            {
                _logger.LogWarning("Source {Type} has no path", source.Type);
                continue;
            }

            if (!Directory.Exists(source.Path))
            {
                _logger.LogError("Source path does not exist: {Path}", source.Path);
                continue;
            }

            _logger.LogInformation("Scanning source: {Type} at {Path}", source.Type, source.Path);
            await ScanDirectoryAsync(source, cancellationToken);
        }

        _logger.LogInformation("=== PhotoScanService: Scan completed ===");
    }

    private async Task ScanDirectoryAsync(PhotoSource source, CancellationToken cancellationToken)
    {
        var files = Directory.EnumerateFiles(source.Path!, "*.*", SearchOption.AllDirectories)
            .Where(f => ExifHelper.IsSupportedImageFormat(f))
            .ToList();

        _logger.LogInformation("Found {Count} image files in {Path}", files.Count, source.Path);

        var indexed = 0;
        var skipped = 0;
        var errors = 0;
        var processed = 0;

        // Update status with total files
        _indexingStatus.UpdateStatus(new IndexingStatus
        {
            IsScanning = true,
            TotalFiles = files.Count,
            ProcessedFiles = 0,
            IndexedPhotos = indexed,
            SkippedPhotos = skipped,
            Errors = errors,
            CurrentSource = source.Type,
            StartedAt = DateTime.UtcNow
        });

        foreach (var filePath in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if already indexed
                var existingMedia = await _dbContext.Media
                    .FirstOrDefaultAsync(m => m.Path == filePath, cancellationToken);

                if (existingMedia != null)
                {
                    skipped++;
                    processed++;
                    UpdateProgress(source.Type, files.Count, processed, indexed, skipped, errors);
                    continue;
                }

                // Extract metadata
                var metadata = ExifHelper.ExtractMetadata(filePath);
                var sha1Hash = ExifHelper.ComputeSha1Hash(filePath);

                // Check for duplicate by hash
                var duplicateByHash = await _dbContext.Media
                    .FirstOrDefaultAsync(m => m.Sha1 == sha1Hash, cancellationToken);

                if (duplicateByHash != null)
                {
                    _logger.LogDebug("Skipping duplicate photo (by hash): {Path}", filePath);
                    skipped++;
                    processed++;
                    UpdateProgress(source.Type, files.Count, processed, indexed, skipped, errors);
                    continue;
                }

                // Create new media record
                var media = new Media
                {
                    Source = source.Type,
                    Path = filePath,
                    Sha1 = sha1Hash,
                    TakenUtc = metadata.TakenUtc,
                    Width = metadata.Width,
                    Height = metadata.Height,
                    Location = metadata.Location,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                };

                _dbContext.Media.Add(media);
                indexed++;
                processed++;

                // Update progress and save frequently
                UpdateProgress(source.Type, files.Count, processed, indexed, skipped, errors);

                // Batch save every 10 items
                if (indexed % 10 == 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Indexed {Count} photos so far...", indexed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing photo: {Path}", filePath);
                errors++;
                processed++;
                UpdateProgress(source.Type, files.Count, processed, indexed, skipped, errors);
            }
        }

        // Final save
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Mark as complete
        _indexingStatus.UpdateStatus(new IndexingStatus
        {
            IsScanning = false,
            TotalFiles = files.Count,
            ProcessedFiles = processed,
            IndexedPhotos = indexed,
            SkippedPhotos = skipped,
            Errors = errors,
            CurrentSource = source.Type,
            CompletedAt = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Scan complete for {Source}: {Indexed} indexed, {Skipped} skipped, {Errors} errors",
            source.Type, indexed, skipped, errors);
    }

    private void UpdateProgress(string source, int total, int processed, int indexed, int skipped, int errors)
    {
        _indexingStatus.UpdateStatus(new IndexingStatus
        {
            IsScanning = true,
            TotalFiles = total,
            ProcessedFiles = processed,
            IndexedPhotos = indexed,
            SkippedPhotos = skipped,
            Errors = errors,
            CurrentSource = source
        });
    }
}
