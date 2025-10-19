namespace FamilyWall.Core.Models;

public class Media
{
    public int Id { get; set; }
    public required string Source { get; set; } // "NAS", "OneDrive", "Local"
    public required string Path { get; set; }
    public string? Sha1 { get; set; }
    public DateTime? TakenUtc { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Location { get; set; } // GPS/EXIF location
    public int Rating { get; set; } = 0;
    public bool Favorite { get; set; } = false;
    public DateTime? LastShownUtc { get; set; }
    public int ShownCount { get; set; } = 0;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<MediaTag> Tags { get; set; } = new List<MediaTag>();
}
