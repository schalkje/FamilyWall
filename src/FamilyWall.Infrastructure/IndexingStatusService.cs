using FamilyWall.Core.Abstractions;

namespace FamilyWall.Infrastructure;

/// <summary>
/// Thread-safe service for tracking and broadcasting photo indexing progress.
/// </summary>
public class IndexingStatusService : IIndexingStatusService
{
    private readonly object _lock = new();
    private IndexingStatus _status = new();

    public IndexingStatus Status
    {
        get
        {
            lock (_lock)
            {
                return new IndexingStatus
                {
                    IsScanning = _status.IsScanning,
                    TotalFiles = _status.TotalFiles,
                    ProcessedFiles = _status.ProcessedFiles,
                    IndexedPhotos = _status.IndexedPhotos,
                    SkippedPhotos = _status.SkippedPhotos,
                    Errors = _status.Errors,
                    CurrentSource = _status.CurrentSource,
                    StartedAt = _status.StartedAt,
                    CompletedAt = _status.CompletedAt
                };
            }
        }
    }

    public event EventHandler<IndexingStatus>? StatusChanged;

    public void UpdateStatus(IndexingStatus status)
    {
        lock (_lock)
        {
            _status = status;
        }

        // Notify subscribers on a background thread to avoid blocking
        Task.Run(() => StatusChanged?.Invoke(this, Status));
    }
}
