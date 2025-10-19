using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using FamilyWall.Core.Settings;
using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using FamilyWall.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FamilyWall.Services;

/// <summary>
/// Background service that indexes photos from configured sources.
/// Scans NAS/OneDrive/Local folders and populates the Media table with metadata.
/// </summary>
public class PhotoIndexingService : BackgroundService
{
    private readonly ILogger<PhotoIndexingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AppSettings> _settings;
    private readonly FamilyWall.Core.Abstractions.IIndexingStatusService _indexingStatus;

    public PhotoIndexingService(
        ILogger<PhotoIndexingService> logger,
        IServiceProvider serviceProvider,
        IOptions<AppSettings> settings,
        FamilyWall.Core.Abstractions.IIndexingStatusService indexingStatus)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = settings;
        _indexingStatus = indexingStatus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("PhotoIndexingService STARTING...");
        _logger.LogInformation("========================================");

        try
        {
            // Wait a bit for app to fully start
            _logger.LogInformation("Waiting 2 seconds for app initialization...");
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            _logger.LogInformation("Starting initial photo scan...");
            // Do initial scan
            await ScanPhotosAsync(stoppingToken);
            _logger.LogInformation("Initial photo scan completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FATAL ERROR during PhotoIndexingService startup!");
            throw;
        }

        // Periodic re-scan
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                _logger.LogDebug("PhotoIndexingService: periodic scan starting...");
                await ScanPhotosAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PhotoIndexingService encountered an error during periodic scan");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("PhotoIndexingService stopped.");
    }

    private async Task ScanPhotosAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ScanPhotosAsync() called");

        var photoSettings = _settings.Value.Photos;
        _logger.LogInformation("Photo settings retrieved. Sources count: {Count}", photoSettings?.Sources?.Count ?? 0);

        if (photoSettings.Sources == null || !photoSettings.Sources.Any())
        {
            _logger.LogWarning("No photo sources configured in settings!");
            _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
            {
                IsScanning = false,
                TotalFiles = 0,
                ProcessedFiles = 0,
                IndexedPhotos = 0,
                CompletedAt = DateTime.UtcNow
            });
            return;
        }

        _logger.LogInformation("Creating service scope for database context...");
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _logger.LogInformation("Database context acquired");

        var enabledSources = photoSettings.Sources.Where(s => s.Enabled).OrderBy(s => s.Priority).ToList();
        _logger.LogInformation("Enabled sources: {Count}", enabledSources.Count);

        foreach (var source in enabledSources)
        {
            _logger.LogInformation("Processing source - Type: {Type}, Path: {Path}, Enabled: {Enabled}",
                source.Type, source.Path ?? "(null)", source.Enabled);

            if (string.IsNullOrWhiteSpace(source.Path))
            {
                _logger.LogWarning("Photo source {Type} has no path configured", source.Type);
                continue;
            }

            var pathExists = Directory.Exists(source.Path);
            _logger.LogInformation("Path exists check for '{Path}': {Exists}", source.Path, pathExists);

            if (!pathExists)
            {
                _logger.LogError("Photo source path does not exist: {Path}", source.Path);
                _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
                {
                    IsScanning = false,
                    TotalFiles = 0,
                    ProcessedFiles = 0,
                    IndexedPhotos = 0,
                    Errors = 1,
                    CurrentSource = source.Type,
                    CompletedAt = DateTime.UtcNow
                });
                continue;
            }

            _logger.LogInformation("✓ Starting scan of photo source: {Type} at {Path}", source.Type, source.Path);

            try
            {
                await ScanDirectoryAsync(dbContext, source, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning source {Type} at {Path}", source.Type, source.Path);
                _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
                {
                    IsScanning = false,
                    Errors = 1,
                    CurrentSource = source.Type,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async Task ScanDirectoryAsync(AppDbContext dbContext, PhotoSource source, CancellationToken cancellationToken)
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
        _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
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
                var existingMedia = await dbContext.Media
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
                var duplicateByHash = await dbContext.Media
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

                dbContext.Media.Add(media);
                indexed++;
                processed++;

                // Update progress and save frequently to show photos ASAP
                UpdateProgress(source.Type, files.Count, processed, indexed, skipped, errors);

                // Batch save every 10 items (reduced from 50 for faster UI updates)
                if (indexed % 10 == 0)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
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
        await dbContext.SaveChangesAsync(cancellationToken);

        // Mark as complete
        _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
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
            "Photo indexing complete for {Source}: {Indexed} indexed, {Skipped} skipped, {Errors} errors",
            source.Type, indexed, skipped, errors);
    }

    private void UpdateProgress(string source, int total, int processed, int indexed, int skipped, int errors)
    {
        _indexingStatus.UpdateStatus(new FamilyWall.Core.Abstractions.IndexingStatus
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
