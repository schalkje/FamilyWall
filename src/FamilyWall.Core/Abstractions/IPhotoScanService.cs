namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Service for scanning and indexing photos from configured sources.
/// </summary>
public interface IPhotoScanService
{
    /// <summary>
    /// Scans all enabled photo sources and indexes photos to the database.
    /// </summary>
    Task ScanAllSourcesAsync(CancellationToken cancellationToken = default);
}
