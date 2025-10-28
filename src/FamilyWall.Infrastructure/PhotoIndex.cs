using FamilyWall.Core.Abstractions;
using FamilyWall.Core.Models;
using FamilyWall.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyWall.Infrastructure;

/// <summary>
/// Photo index implementation using EF Core and SQLite.
/// </summary>
public class PhotoIndex : IPhotoIndex
{
    private readonly AppDbContext _context;

    public PhotoIndex(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Media?> GetNextPhotoAsync(CancellationToken cancellationToken = default)
    {
        // Simple logic: prefer photos from same date in prior years, then recently added, then least shown
        var today = DateTime.UtcNow.Date;
        var monthDay = new { today.Month, today.Day };

        // Try same date in prior years (±3 days)
        var sameDatePhotos = await _context.Media
            .Where(m => m.TakenUtc.HasValue &&
                        m.TakenUtc.Value.Month == monthDay.Month &&
                        Math.Abs(m.TakenUtc.Value.Day - monthDay.Day) <= 3)
            .OrderBy(m => m.ShownCount)
            .ThenBy(m => m.LastShownUtc ?? DateTime.MinValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (sameDatePhotos != null)
        {
            return sameDatePhotos;
        }

        // Fallback: least shown or never shown
        return await _context.Media
            .OrderBy(m => m.ShownCount)
            .ThenBy(m => m.LastShownUtc ?? DateTime.MinValue)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Media>> SearchPhotosAsync(DateTime? takenDate = null, string? tag = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Media.AsQueryable();

        if (takenDate.HasValue)
        {
            var date = takenDate.Value.Date;
            query = query.Where(m => m.TakenUtc.HasValue && m.TakenUtc.Value.Date == date);
        }

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(m => m.Tags.Any(t => t.Tag == tag));
        }

        return await query.OrderByDescending(m => m.TakenUtc).ToListAsync(cancellationToken);
    }

    public async Task UpdateRatingAsync(int mediaId, int rating, CancellationToken cancellationToken = default)
    {
        var media = await _context.Media.FindAsync(new object[] { mediaId }, cancellationToken);
        if (media != null)
        {
            media.Rating = rating;
            media.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ToggleFavoriteAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _context.Media.FindAsync(new object[] { mediaId }, cancellationToken);
        if (media != null)
        {
            media.Favorite = !media.Favorite;
            media.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RecordShownAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _context.Media.FindAsync(new object[] { mediaId }, cancellationToken);
        if (media != null)
        {
            media.LastShownUtc = DateTime.UtcNow;
            media.ShownCount++;
            media.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
