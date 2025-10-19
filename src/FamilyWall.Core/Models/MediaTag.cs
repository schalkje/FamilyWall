namespace FamilyWall.Core.Models;

public class MediaTag
{
    public int Id { get; set; }
    public int MediaId { get; set; }
    public required string Tag { get; set; }

    public Media? Media { get; set; }
}
