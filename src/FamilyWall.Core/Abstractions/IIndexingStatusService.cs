namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Service for tracking and broadcasting photo indexing progress.
/// </summary>
public interface IIndexingStatusService
{
    /// <summary>
    /// Current indexing status.
    /// </summary>
    IndexingStatus Status { get; }

    /// <summary>
    /// Event raised when indexing status changes.
    /// </summary>
    event EventHandler<IndexingStatus>? StatusChanged;

    /// <summary>
    /// Updates the indexing status.
    /// </summary>
    void UpdateStatus(IndexingStatus status);
}

/// <summary>
/// Current state of photo indexing.
/// </summary>
public class IndexingStatus
{
    public bool IsScanning { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int IndexedPhotos { get; set; }
    public int SkippedPhotos { get; set; }
    public int Errors { get; set; }
    public string? CurrentSource { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int PercentComplete => TotalFiles > 0 ? (ProcessedFiles * 100) / TotalFiles : 0;
    public bool HasPhotos => IndexedPhotos > 0;
}
