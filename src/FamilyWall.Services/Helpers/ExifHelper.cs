using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Security.Cryptography;

namespace FamilyWall.Services.Helpers;

/// <summary>
/// Helper for extracting EXIF metadata from photos and computing file hashes.
/// </summary>
public static class ExifHelper
{
    /// <summary>
    /// Extracts metadata from a photo file.
    /// </summary>
    public static PhotoMetadata ExtractMetadata(string filePath)
    {
        var metadata = new PhotoMetadata
        {
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length
        };

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            // Extract date taken
            var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd != null)
            {
                if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTime))
                {
                    metadata.TakenUtc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
                else if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTime, out dateTime))
                {
                    metadata.TakenUtc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }

                // Extract image dimensions
                if (exifSubIfd.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var width))
                {
                    metadata.Width = width;
                }
                if (exifSubIfd.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var height))
                {
                    metadata.Height = height;
                }
            }

            // Extract GPS location
            var gpsDirectory = directories.OfType<MetadataExtractor.Formats.Exif.GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null && gpsDirectory.TagCount > 0)
            {
                try
                {
                    // Try to get latitude and longitude
                    if (gpsDirectory.TryGetDouble(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude, out var latitude) &&
                        gpsDirectory.TryGetDouble(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude, out var longitude))
                    {
                        // Get the reference (N/S, E/W)
                        var latRef = gpsDirectory.GetString(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitudeRef);
                        var lonRef = gpsDirectory.GetString(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitudeRef);

                        if (latRef == "S") latitude = -latitude;
                        if (lonRef == "W") longitude = -longitude;

                        metadata.Location = $"{latitude:F6},{longitude:F6}";
                    }
                }
                catch
                {
                    // GPS extraction failed, continue without location
                }
            }

            // Fallback: try to get dimensions from any directory
            if (!metadata.Width.HasValue || !metadata.Height.HasValue)
            {
                foreach (var dir in directories)
                {
                    if (!metadata.Width.HasValue && dir.TryGetInt32(ExifDirectoryBase.TagImageWidth, out var w))
                    {
                        metadata.Width = w;
                    }
                    if (!metadata.Height.HasValue && dir.TryGetInt32(ExifDirectoryBase.TagImageHeight, out var h))
                    {
                        metadata.Height = h;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - we can still index the photo without metadata
            metadata.Error = ex.Message;
        }

        return metadata;
    }

    /// <summary>
    /// Computes SHA1 hash of a file for deduplication.
    /// </summary>
    public static string ComputeSha1Hash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a file is a supported image format.
    /// </summary>
    public static bool IsSupportedImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".heic" or ".heif" or ".webp";
    }
}

/// <summary>
/// Container for extracted photo metadata.
/// </summary>
public class PhotoMetadata
{
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public DateTime? TakenUtc { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Location { get; set; }
    public string? Error { get; set; }
}
