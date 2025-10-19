namespace FamilyWall.App.Services;

/// <summary>
/// Service for converting local file paths to data URLs that can be displayed in Blazor WebView
/// </summary>
public class PhotoUrlService
{
    private readonly Dictionary<string, string> _cache = new();
    private const int MaxCacheSize = 10; // Cache last 10 images as data URLs
    private readonly Queue<string> _cacheOrder = new();

    /// <summary>
    /// Converts a local file path to a data URL for display in Blazor
    /// </summary>
    public async Task<string> GetPhotoDataUrlAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return string.Empty;
        }

        // Check cache first
        if (_cache.TryGetValue(filePath, out var cachedUrl))
        {
            return cachedUrl;
        }

        try
        {
            // Read file and convert to base64
            var bytes = await File.ReadAllBytesAsync(filePath);
            var base64 = Convert.ToBase64String(bytes);
            
            // Determine MIME type from extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };

            var dataUrl = $"data:{mimeType};base64,{base64}";

            // Add to cache with size limit
            if (_cache.Count >= MaxCacheSize)
            {
                // Remove oldest entry
                var oldestKey = _cacheOrder.Dequeue();
                _cache.Remove(oldestKey);
            }

            _cache[filePath] = dataUrl;
            _cacheOrder.Enqueue(filePath);

            return dataUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading photo {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _cacheOrder.Clear();
    }
}
