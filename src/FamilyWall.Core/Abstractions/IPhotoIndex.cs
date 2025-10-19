using FamilyWall.Core.Models;

namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Photo indexing and retrieval service.
/// </summary>
public interface IPhotoIndex
{
    Task<Media?> GetNextPhotoAsync(CancellationToken cancellationToken = default);
    Task<List<Media>> SearchPhotosAsync(DateTime? takenDate = null, string? tag = null, CancellationToken cancellationToken = default);
    Task UpdateRatingAsync(int mediaId, int rating, CancellationToken cancellationToken = default);
    Task ToggleFavoriteAsync(int mediaId, CancellationToken cancellationToken = default);
    Task RecordShownAsync(int mediaId, CancellationToken cancellationToken = default);
}
