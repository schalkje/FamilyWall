# Photo Provider Integration - Technical Plan

## Overview

This document details the technical architecture, implementation plan, and database design for the extensible photo provider system. It focuses on the OneDrive implementation while maintaining an architecture that supports future providers (Google Photos, iCloud, etc.).

---

## Architecture Design

### 1. Core Abstractions

#### IPhotoProvider (Core Interface)

```csharp
// FamilyWall.Core/Abstractions/IPhotoProvider.cs
public interface IPhotoProvider
{
    // Provider identification
    string ProviderType { get; } // "OneDrive", "GooglePhotos", "LocalDrive", "NAS"
    string ProviderId { get; } // Unique instance ID
    string DisplayName { get; set; } // User-friendly name

    // Authentication (for cloud providers)
    Task<bool> IsAuthenticatedAsync();
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationOptions options);
    Task SignOutAsync();

    // Folder/Album browsing
    Task<List<PhotoContainer>> GetContainersAsync(); // Folders or Albums
    Task<List<PhotoContainer>> GetSubContainersAsync(string containerId);

    // Photo enumeration
    Task<List<PhotoMetadata>> GetPhotosInContainerAsync(
        string containerId,
        bool recursive = false);

    // Incremental sync (optional - not all providers support this)
    Task<DeltaResult?> GetPhotosDeltaAsync(
        string containerId,
        string? deltaToken = null);

    // Photo operations
    Task<Stream> DownloadPhotoAsync(string photoId, PhotoSize size = PhotoSize.Full);
    Task<Stream> DownloadThumbnailAsync(string photoId, ThumbnailSize size = ThumbnailSize.Medium);

    // Metadata operations
    Task<PhotoMetadata> GetPhotoMetadataAsync(string photoId);
    Task UpdatePhotoMetadataAsync(string photoId, PhotoMetadataUpdate update);

    // Favorite support (optional)
    Task<bool> SupportsFavoritesAsync();
    Task<bool> GetFavoriteStatusAsync(string photoId);
    Task SetFavoriteStatusAsync(string photoId, bool isFavorite);

    // Provider capabilities
    PhotoProviderCapabilities GetCapabilities();
}
```

#### PhotoProviderCapabilities

```csharp
// FamilyWall.Core/Models/PhotoProviderCapabilities.cs
public class PhotoProviderCapabilities
{
    public bool SupportsAuthentication { get; set; } // True for cloud, false for local
    public bool SupportsDeltaSync { get; set; } // OneDrive/Google = true, Local = false
    public bool SupportsFavorites { get; set; } // OneDrive = true, Local = false
    public bool SupportsMetadataWrite { get; set; } // Can write metadata back
    public bool SupportsAlbums { get; set; } // Google Photos albums
    public bool SupportsFolders { get; set; } // OneDrive/Local folders
    public bool RequiresNetwork { get; set; } // True for cloud, false for local
    public long MaxFileSize { get; set; } // Maximum photo file size supported
    public List<string> SupportedFormats { get; set; } // Image formats
}
```

#### PhotoContainer (Generic Folder/Album)

```csharp
// FamilyWall.Core/Models/PhotoContainer.cs
public class PhotoContainer
{
    public string Id { get; set; } // Provider-specific ID
    public string Name { get; set; } // Display name
    public string Path { get; set; } // Full path or hierarchical name
    public string? ParentId { get; set; } // For hierarchical containers
    public ContainerType Type { get; set; } // Folder, Album, Collection
    public int? PhotoCount { get; set; } // If available
    public DateTime? LastModifiedUtc { get; set; }
    public bool IsShared { get; set; } // For cloud providers
    public string? Owner { get; set; } // For shared containers
}

public enum ContainerType
{
    Folder,      // File system folder (OneDrive, Local, NAS)
    Album,       // Google Photos album
    Collection,  // Custom grouping
    Root         // Root container
}
```

#### PhotoMetadata (Unified Metadata Model)

```csharp
// FamilyWall.Core/Models/PhotoMetadata.cs
public class PhotoMetadata
{
    // Universal properties
    public string ProviderId { get; set; } // e.g., "onedrive-account-001"
    public string ProviderType { get; set; } // e.g., "OneDrive"
    public string PhotoId { get; set; } // Provider-specific photo ID
    public string FileName { get; set; }
    public string ContainerId { get; set; } // Folder or Album ID
    public string ContainerPath { get; set; } // Display path

    // File information
    public long FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }

    // EXIF metadata
    public DateTime? TakenUtc { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? CameraModel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LocationName { get; set; }

    // Provider-specific metadata
    public bool IsFavorite { get; set; }
    public string? ETag { get; set; } // For change tracking
    public string? Sha1Hash { get; set; } // For deduplication

    // Download URLs (cloud providers)
    public string? ThumbnailUrl { get; set; }
    public string? DownloadUrl { get; set; }

    // Provider-specific extended data
    public Dictionary<string, object>? ExtendedMetadata { get; set; }
}
```

### 2. Configuration Models

#### PhotoSourceSettings

```csharp
// FamilyWall.Core/Settings/PhotoSourceSettings.cs
public class PhotoSourceSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProviderType { get; set; } // "OneDrive", "GooglePhotos", "LocalDrive", "NAS"
    public string DisplayName { get; set; } = "Photo Source";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 0; // For ordering multiple sources

    // Cloud provider settings (OneDrive, Google Photos)
    public CloudProviderSettings? CloudSettings { get; set; }

    // Local/NAS provider settings
    public LocalProviderSettings? LocalSettings { get; set; }

    // Selected containers (folders/albums)
    public List<ContainerSelection> SelectedContainers { get; set; } = new();
}
```

#### CloudProviderSettings

```csharp
// FamilyWall.Core/Settings/CloudProviderSettings.cs
public class CloudProviderSettings
{
    public string TenantId { get; set; } = "consumers"; // For Microsoft/Azure
    public string ClientId { get; set; } = ""; // OAuth client ID
    public List<string> Scopes { get; set; } = new(); // Required OAuth scopes
    public string? RefreshToken { get; set; } // Encrypted refresh token
    public DateTime? TokenExpiryUtc { get; set; }
}
```

#### LocalProviderSettings

```csharp
// FamilyWall.Core/Settings/LocalProviderSettings.cs
public class LocalProviderSettings
{
    public string RootPath { get; set; } = ""; // e.g., "C:\\Pictures" or "\\\\nas\\photos"
    public bool IsNetworkPath { get; set; } // True for NAS/SMB paths
    public string? Username { get; set; } // For network authentication
    public string? EncryptedPassword { get; set; } // For network authentication
    public bool WatchForChanges { get; set; } = true; // File system watcher
    public bool FollowSymlinks { get; set; } = true;
}
```

#### ContainerSelection

```csharp
// FamilyWall.Core/Settings/ContainerSelection.cs
public class ContainerSelection
{
    public string ContainerId { get; set; } = "";
    public string ContainerPath { get; set; } = "";
    public ContainerType Type { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Recursive { get; set; } = true; // Include subfolders/sub-albums
    public DateTime? LastSyncUtc { get; set; }
    public string? DeltaToken { get; set; } // For incremental sync
}
```

#### SmartFilterSettings

```csharp
// FamilyWall.Core/Settings/SmartFilterSettings.cs
public class SmartFilterSettings
{
    // Algorithm selection
    public SlideshowAlgorithm SelectedAlgorithm { get; set; } = SlideshowAlgorithm.Smart;

    // Smart algorithm filters (only used when SelectedAlgorithm = Smart)
    public bool EnableSameDayInHistory { get; set; } = true;
    public int SameDayWindowDays { get; set; } = 3; // ±3 days

    public bool EnableRecentPhotos { get; set; } = true;
    public int RecentPhotosDays { get; set; } = 30;

    public bool EnableHighRatings { get; set; } = true;
    public int MinimumHighRating { get; set; } = 4; // 4-5 stars

    public bool EnableFavorites { get; set; } = true;

    // Weight multipliers for Smart algorithm
    public double SameDayWeight { get; set; } = 3.0;
    public double RecentWeight { get; set; } = 2.0;
    public double HighRatingWeight { get; set; } = 2.5;
    public double FavoriteWeight { get; set; } = 4.0;

    // Star-based algorithm probabilities (only used when SelectedAlgorithm = StarBased)
    public int FiveStarProbability { get; set; } = 80;  // 80%
    public int FourStarProbability { get; set; } = 60;  // 60%
    public int ThreeStarProbability { get; set; } = 40; // 40%
    public int TwoStarProbability { get; set; } = 20;   // 20%
    public int OneStarProbability { get; set; } = 10;   // 10%
    public int UnratedProbability { get; set; } = 40;   // 40%
}

public enum SlideshowAlgorithm
{
    Smart,        // Weighted scoring with multiple filters
    Favorites,    // Favorites first, then 4-5 star photos
    StarBased,    // Probability-based selection by star rating
    HighLow,      // Alternate between high-quality (fav/5/4 star) and others
    Unmarked,     // Only show photos with no favorite and no rating
    Random        // Completely random selection (no filtering)
}
```

#### PhotoCacheSettings

```csharp
// FamilyWall.Core/Settings/PhotoCacheSettings.cs
public class PhotoCacheSettings
{
    // Maximum cache size in MB (0 = unlimited)
    public long MaxCacheSizeMB { get; set; } = 5120; // 5GB default

    // Retention period in days (0 = keep forever)
    public int RetentionDays { get; set; } = 30;

    // Minimum number of photos to always keep (even if exceeding size/age limits)
    public int MinimumPhotoCount { get; set; } = 500;

    // Always protect these from cleanup
    public bool AlwaysKeepFavorites { get; set; } = true;
    public bool AlwaysKeepHighRated { get; set; } = true; // 4-5 stars
    public int HighRatedThreshold { get; set; } = 4;

    // Cache cleanup trigger threshold (% of max size)
    public int CleanupTriggerPercent { get; set; } = 90; // Start cleanup at 90% full

    // Cache cleanup target (% of max size to free up to)
    public int CleanupTargetPercent { get; set; } = 70; // Clean down to 70%

    // Automatic cleanup enabled
    public bool AutoCleanupEnabled { get; set; } = true;

    // Statistics tracking
    public DateTime? LastCleanupUtc { get; set; }
    public long LastCleanupFreedMB { get; set; }
    public int LastCleanupDeletedCount { get; set; }
}
```

#### PhotoSettings (Top-level)

```csharp
// FamilyWall.Core/Settings/AppSettings.cs - PhotoSettings
public class PhotoSettings
{
    // Unified photo sources
    public List<PhotoSourceSettings> PhotoSources { get; set; } = new();

    // Smart filtering
    public SmartFilterSettings SmartFiltering { get; set; } = new();

    // Cache management
    public PhotoCacheSettings CacheManagement { get; set; } = new();

    // Slideshow settings
    public int SlideshowIntervalSeconds { get; set; } = 5;
    public bool ShowMetadataOverlay { get; set; } = true;
}
```

### 3. Domain Model

#### Media Entity

```csharp
// FamilyWall.Core/Models/Media.cs
public class Media
{
    public int Id { get; set; }

    // Provider information
    public string ProviderId { get; set; } // e.g., "onedrive-account-001", "local-drive-001"
    public string ProviderType { get; set; } // e.g., "OneDrive", "LocalDrive", "GooglePhotos"
    public string ProviderPhotoId { get; set; } // Provider-specific photo ID

    // File information
    public string Path { get; set; } // Local cache path or file system path
    public string FileName { get; set; }
    public string? Sha1 { get; set; }
    public long FileSizeBytes { get; set; }

    // Photo metadata
    public DateTime? TakenUtc { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Location { get; set; }
    public string? CameraModel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Container (folder/album) information
    public string ContainerId { get; set; }
    public string ContainerPath { get; set; } // Display path
    public ContainerType ContainerType { get; set; }

    // User ratings and favorites
    public int Rating { get; set; }
    public bool Favorite { get; set; } // Local favorite
    public bool ProviderFavorite { get; set; } // Provider's favorite status (OneDrive, Google)

    // Photo marking
    public bool NeverShowAgain { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionUtc { get; set; }
    public string? MarkedForDeletionReason { get; set; }

    // View tracking
    public DateTime? LastShownUtc { get; set; }
    public int ShownCount { get; set; }

    // Cache management
    public string? LocalCachePath { get; set; }
    public long? LocalCacheFileSizeBytes { get; set; }
    public DateTime? LocalCacheDownloadedUtc { get; set; }
    public DateTime? LocalCacheLastAccessUtc { get; set; }
    public bool IsCachePinned { get; set; }

    // Sync metadata
    public DateTime? AddedToIndexUtc { get; set; }
    public DateTime? LastSyncUtc { get; set; }
    public string? ETag { get; set; }

    // Timestamps
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
```

### 4. Service Interfaces

#### IPhotoProviderFactory

```csharp
// FamilyWall.Core/Abstractions/IPhotoProviderFactory.cs
public interface IPhotoProviderFactory
{
    IPhotoProvider CreateProvider(PhotoSourceSettings settings);
    List<string> GetSupportedProviderTypes();
    PhotoProviderInfo GetProviderInfo(string providerType);
}

public class PhotoProviderInfo
{
    public string ProviderType { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string IconName { get; set; } // For UI
    public bool RequiresAuthentication { get; set; }
    public bool IsCloudProvider { get; set; }
}
```

#### IPhotoIndex (Extended)

```csharp
// FamilyWall.Core/Abstractions/IPhotoIndex.cs
public interface IPhotoIndex
{
    // Existing methods...

    // Smart filtering queries
    Task<Media?> GetNextPhotoAsync(SmartFilterSettings settings);
    Task<List<Media>> GetSameDayInHistoryAsync(DateTime referenceDate, int windowDays);
    Task<List<Media>> GetRecentPhotosAsync(int days, int limit = 100);
    Task<List<Media>> GetHighRatedPhotosAsync(int minRating, int limit = 100);
    Task<List<Media>> GetFavoritePhotosAsync(int limit = 100);

    // Provider-specific queries
    Task<Media?> GetMediaByProviderPhotoIdAsync(string providerId, string photoId);
    Task<List<Media>> GetMediaByProviderAsync(string providerId);
    Task<bool> HasMediaInContainerAsync(string providerId, string containerId);

    // Metadata updates
    Task UpdatePhotoMetadataAsync(int mediaId, PhotoMetadata metadata);
    Task SyncFavoriteToProviderAsync(int mediaId, bool isFavorite);
    Task AddPhotoAsync(PhotoMetadata metadata);
}
```

#### IPhotoSyncService

```csharp
// FamilyWall.Core/Abstractions/IPhotoSyncService.cs
public interface IPhotoSyncService
{
    // Full sync (initial or manual)
    Task SyncAllSourcesAsync(CancellationToken cancellationToken = default);
    Task SyncSourceAsync(string sourceId, CancellationToken cancellationToken = default);
    Task SyncContainerAsync(string sourceId, string containerId, CancellationToken cancellationToken = default);

    // Incremental sync (delta)
    Task SyncDeltaForAllSourcesAsync(CancellationToken cancellationToken = default);

    // Favorite sync back to provider
    Task SyncLocalFavoritesToProvidersAsync(CancellationToken cancellationToken = default);

    // Status
    PhotoSyncStatus GetSyncStatus();
}
```

#### IPhotoCacheManager

```csharp
// FamilyWall.Core/Abstractions/IPhotoCacheManager.cs
public interface IPhotoCacheManager
{
    // Cache download management
    Task<string> DownloadAndCachePhotoAsync(Media media, bool pinToCache = false);
    Task<bool> IsPhotoCachedAsync(int mediaId);
    Task<string?> GetCachedPhotoPathAsync(int mediaId);

    // Cache statistics
    Task<CacheStatistics> GetCacheStatisticsAsync();

    // Cache cleanup operations
    Task<CacheCleanupResult> CleanupCacheAsync(bool force = false);
    Task<CacheCleanupResult> CleanupCacheIfNeededAsync(); // Respects trigger threshold
    Task PinPhotoToCacheAsync(int mediaId); // Protect from cleanup
    Task UnpinPhotoFromCacheAsync(int mediaId);

    // Manual operations
    Task ClearAllCacheAsync(); // Delete all cached photos
    Task RemoveFromCacheAsync(int mediaId); // Delete specific photo from cache

    // Prefetching for performance
    Task PrefetchPhotosAsync(List<Media> photos, int maxConcurrent = 3);
}
```

#### IPhotoMarkingService

```csharp
// FamilyWall.Core/Abstractions/IPhotoMarkingService.cs
public interface IPhotoMarkingService
{
    // Rating operations
    Task SetRatingAsync(int mediaId, int rating); // 1-5 stars
    Task<int> GetRatingAsync(int mediaId);

    // Favorite operations (local + provider sync)
    Task SetFavoriteAsync(int mediaId, bool isFavorite, bool syncToProvider = true);
    Task<bool> GetFavoriteAsync(int mediaId);

    // Never show again
    Task MarkNeverShowAgainAsync(int mediaId);
    Task UnmarkNeverShowAgainAsync(int mediaId);
    Task<bool> IsMarkedNeverShowAgainAsync(int mediaId);
    Task<List<Media>> GetNeverShowAgainPhotosAsync();

    // Mark for deletion
    Task MarkForDeletionAsync(int mediaId, string? reason = null);
    Task UnmarkForDeletionAsync(int mediaId);
    Task<bool> IsMarkedForDeletionAsync(int mediaId);
    Task<List<Media>> GetMarkedForDeletionPhotosAsync();

    // Batch deletion operations
    Task<int> DeleteMarkedPhotosAsync(bool deleteFromProvider = false);
    Task ClearAllDeletionMarksAsync();

    // Undo support (simple last action tracking)
    Task<bool> UndoLastMarkingAsync();

    // Batch operations
    Task BatchSetRatingAsync(List<int> mediaIds, int rating);
    Task BatchMarkNeverShowAgainAsync(List<int> mediaIds);
    Task BatchMarkForDeletionAsync(List<int> mediaIds, string? reason = null);
}
```

### 5. Provider Implementations

#### OneDrivePhotoProvider

```csharp
// FamilyWall.Integrations.OneDrive/OneDrivePhotoProvider.cs
public class OneDrivePhotoProvider : IPhotoProvider
{
    public string ProviderType => "OneDrive";
    public string ProviderId { get; }
    public string DisplayName { get; set; }

    private readonly IGraphClient _graphClient;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<OneDrivePhotoProvider> _logger;

    public OneDrivePhotoProvider(
        string providerId,
        CloudProviderSettings settings,
        IGraphClient graphClient,
        ITokenStore tokenStore,
        ILogger<OneDrivePhotoProvider> logger)
    {
        ProviderId = providerId;
        _graphClient = graphClient;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<List<PhotoContainer>> GetContainersAsync()
    {
        // GET /me/drive/root/children
        var folders = await _graphClient.GetRootFoldersAsync();

        return folders.Select(f => new PhotoContainer
        {
            Id = f.Id,
            Name = f.Name,
            Path = $"/{f.Name}",
            Type = ContainerType.Folder
        }).ToList();
    }

    public async Task<List<PhotoMetadata>> GetPhotosInContainerAsync(
        string containerId,
        bool recursive = false)
    {
        // GET /me/drive/items/{containerId}/children?$filter=file ne null
        var items = await _graphClient.GetItemsInFolderAsync(containerId, recursive);

        return items
            .Where(i => IsPhotoFile(i.Name))
            .Select(i => new PhotoMetadata
            {
                ProviderId = ProviderId,
                ProviderType = ProviderType,
                PhotoId = i.Id,
                FileName = i.Name,
                FileSizeBytes = i.Size,
                CreatedUtc = i.CreatedDateTime.UtcDateTime,
                ModifiedUtc = i.LastModifiedDateTime.UtcDateTime,
                TakenUtc = i.Photo?.TakenDateTime?.UtcDateTime,
                Width = i.Image?.Width,
                Height = i.Image?.Height,
                Latitude = i.Location?.Coordinates?.Latitude,
                Longitude = i.Location?.Coordinates?.Longitude,
                CameraModel = i.Photo?.CameraModel,
                IsFavorite = i.IsFavorite ?? false,
                ETag = i.ETag
            }).ToList();
    }

    public async Task<DeltaResult?> GetPhotosDeltaAsync(
        string containerId,
        string? deltaToken = null)
    {
        // GET /me/drive/items/{containerId}/delta?token={deltaToken}
        var deltaResult = await _graphClient.GetDeltaAsync(containerId, deltaToken);

        return new DeltaResult
        {
            Photos = deltaResult.Items.Select(/* map to PhotoMetadata */).ToList(),
            NewDeltaToken = deltaResult.DeltaToken
        };
    }

    public async Task SetFavoriteStatusAsync(string photoId, bool isFavorite)
    {
        // PATCH /me/drive/items/{photoId}
        await _graphClient.UpdateItemAsync(photoId, new { isFavorite });
    }

    public PhotoProviderCapabilities GetCapabilities()
    {
        return new PhotoProviderCapabilities
        {
            SupportsAuthentication = true,
            SupportsDeltaSync = true,
            SupportsFavorites = true,
            SupportsMetadataWrite = true,
            SupportsAlbums = false,
            SupportsFolders = true,
            RequiresNetwork = true,
            MaxFileSize = 250L * 1024 * 1024 * 1024, // 250GB
            SupportedFormats = new() { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".webp" }
        };
    }

    // ... other methods
}
```

#### LocalDriveProvider

```csharp
// FamilyWall.Infrastructure/LocalDriveProvider.cs
public class LocalDriveProvider : IPhotoProvider
{
    public string ProviderType => "LocalDrive";
    public string ProviderId { get; }
    public string DisplayName { get; set; }

    private readonly string _rootPath;
    private readonly FileSystemWatcher? _watcher;

    public LocalDriveProvider(string providerId, LocalProviderSettings settings)
    {
        ProviderId = providerId;
        _rootPath = settings.RootPath;

        if (settings.WatchForChanges)
        {
            _watcher = new FileSystemWatcher(_rootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };
            _watcher.EnableRaisingEvents = true;
        }
    }

    public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);

    public async Task<List<PhotoContainer>> GetContainersAsync()
    {
        var directories = Directory.GetDirectories(_rootPath);

        return directories.Select(dir => new PhotoContainer
        {
            Id = dir,
            Name = Path.GetFileName(dir),
            Path = dir.Replace(_rootPath, "").TrimStart('\\', '/'),
            Type = ContainerType.Folder
        }).ToList();
    }

    public async Task<List<PhotoMetadata>> GetPhotosInContainerAsync(
        string containerId,
        bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".webp" };

        var files = Directory.GetFiles(containerId, "*.*", searchOption)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();

        var metadata = new List<PhotoMetadata>();

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);

            metadata.Add(new PhotoMetadata
            {
                ProviderId = ProviderId,
                ProviderType = ProviderType,
                PhotoId = file, // Use file path as ID
                FileName = fileInfo.Name,
                ContainerId = containerId,
                ContainerPath = Path.GetDirectoryName(file)?.Replace(_rootPath, "") ?? "",
                FileSizeBytes = fileInfo.Length,
                CreatedUtc = fileInfo.CreationTimeUtc,
                ModifiedUtc = fileInfo.LastWriteTimeUtc,
                Sha1Hash = await ComputeSha1Async(file)
            });
        }

        return metadata;
    }

    public PhotoProviderCapabilities GetCapabilities()
    {
        return new PhotoProviderCapabilities
        {
            SupportsAuthentication = false,
            SupportsDeltaSync = false,
            SupportsFavorites = false,
            SupportsMetadataWrite = false,
            SupportsAlbums = false,
            SupportsFolders = true,
            RequiresNetwork = false,
            MaxFileSize = long.MaxValue,
            SupportedFormats = new() { ".jpg", ".jpeg", ".png", ".gif", ".heic", ".webp", ".bmp" }
        };
    }

    // ... other methods
}
```

#### PhotoProviderFactory

```csharp
// FamilyWall.Services/PhotoProviderFactory.cs
public class PhotoProviderFactory : IPhotoProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PhotoProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPhotoProvider CreateProvider(PhotoSourceSettings settings)
    {
        return settings.ProviderType switch
        {
            "OneDrive" => new OneDrivePhotoProvider(
                settings.Id,
                settings.CloudSettings!,
                _serviceProvider.GetRequiredService<IGraphClient>(),
                _serviceProvider.GetRequiredService<ITokenStore>(),
                _serviceProvider.GetRequiredService<ILogger<OneDrivePhotoProvider>>()),

            "LocalDrive" => new LocalDriveProvider(
                settings.Id,
                settings.LocalSettings!),

            "NAS" => new NASProvider(
                settings.Id,
                settings.LocalSettings!),

            _ => throw new NotSupportedException($"Provider type '{settings.ProviderType}' is not supported")
        };
    }

    public List<string> GetSupportedProviderTypes()
    {
        return new List<string> { "OneDrive", "LocalDrive", "NAS" };
    }

    public PhotoProviderInfo GetProviderInfo(string providerType)
    {
        return providerType switch
        {
            "OneDrive" => new PhotoProviderInfo
            {
                ProviderType = "OneDrive",
                DisplayName = "Microsoft OneDrive",
                Description = "Sync photos from OneDrive cloud storage",
                IconName = "onedrive-icon",
                RequiresAuthentication = true,
                IsCloudProvider = true
            },
            "LocalDrive" => new PhotoProviderInfo
            {
                ProviderType = "LocalDrive",
                DisplayName = "Local Drive",
                Description = "Photos from a local folder on this computer",
                IconName = "folder-icon",
                RequiresAuthentication = false,
                IsCloudProvider = false
            },
            "NAS" => new PhotoProviderInfo
            {
                ProviderType = "NAS",
                DisplayName = "Network Drive (NAS)",
                Description = "Photos from a network share or NAS device",
                IconName = "network-icon",
                RequiresAuthentication = false,
                IsCloudProvider = false
            },
            _ => throw new NotSupportedException()
        };
    }
}
```

### 6. Unified Sync Service

```csharp
// FamilyWall.Services/UnifiedPhotoSyncService.cs
public class UnifiedPhotoSyncService : BackgroundService, IPhotoSyncService
{
    private readonly IPhotoProviderFactory _providerFactory;
    private readonly IOptions<PhotoSettings> _settings;
    private readonly IPhotoIndex _photoIndex;
    private readonly ILogger<UnifiedPhotoSyncService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Initial delay

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAllSourcesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Periodic sync
        }
    }

    public async Task SyncAllSourcesAsync(CancellationToken cancellationToken = default)
    {
        var enabledSources = _settings.Value.PhotoSources
            .Where(s => s.Enabled)
            .OrderBy(s => s.Priority)
            .ToList();

        foreach (var sourceSettings in enabledSources)
        {
            try
            {
                var provider = _providerFactory.CreateProvider(sourceSettings);
                await SyncSourceAsync(provider, sourceSettings, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync photo source {SourceId}", sourceSettings.Id);
            }
        }
    }

    private async Task SyncSourceAsync(
        IPhotoProvider provider,
        PhotoSourceSettings settings,
        CancellationToken cancellationToken)
    {
        // Authenticate if required
        if (provider.GetCapabilities().SupportsAuthentication)
        {
            if (!await provider.IsAuthenticatedAsync())
            {
                _logger.LogWarning("Provider {ProviderId} is not authenticated", provider.ProviderId);
                return;
            }
        }

        // Sync each selected container
        foreach (var container in settings.SelectedContainers.Where(c => c.Enabled))
        {
            await SyncContainerAsync(provider, container, cancellationToken);
        }
    }

    private async Task SyncContainerAsync(
        IPhotoProvider provider,
        ContainerSelection container,
        CancellationToken cancellationToken)
    {
        List<PhotoMetadata> photos;

        // Try delta sync first if supported
        if (provider.GetCapabilities().SupportsDeltaSync && container.DeltaToken != null)
        {
            var deltaResult = await provider.GetPhotosDeltaAsync(container.ContainerId, container.DeltaToken);
            if (deltaResult != null)
            {
                photos = deltaResult.Photos;
                container.DeltaToken = deltaResult.NewDeltaToken;
            }
            else
            {
                photos = await provider.GetPhotosInContainerAsync(container.ContainerId, container.Recursive);
            }
        }
        else
        {
            photos = await provider.GetPhotosInContainerAsync(container.ContainerId, container.Recursive);
        }

        // Index photos
        foreach (var photoMetadata in photos)
        {
            await IndexPhotoAsync(photoMetadata, cancellationToken);
        }

        container.LastSyncUtc = DateTime.UtcNow;
    }

    private async Task IndexPhotoAsync(PhotoMetadata metadata, CancellationToken cancellationToken)
    {
        var existing = await _photoIndex.GetMediaByProviderPhotoIdAsync(
            metadata.ProviderId,
            metadata.PhotoId);

        if (existing != null)
        {
            await _photoIndex.UpdatePhotoMetadataAsync(existing.Id, metadata);
        }
        else
        {
            await _photoIndex.AddPhotoAsync(metadata);
        }
    }
}
```

### 7. Cache Management Implementation

```csharp
// FamilyWall.Services/PhotoCacheManager.cs
public class PhotoCacheManager : IPhotoCacheManager
{
    public async Task<CacheCleanupResult> CleanupCacheAsync(bool force = false)
    {
        var settings = _options.Value.CacheManagement;
        var stats = await GetCacheStatisticsAsync();

        if (!force && stats.PercentUsed < settings.CleanupTriggerPercent)
            return new CacheCleanupResult { WasNeeded = false };

        var targetSize = (settings.MaxCacheSizeMB * 1024 * 1024) * settings.CleanupTargetPercent / 100;
        var bytesToFree = stats.TotalCacheSizeBytes - targetSize;

        // Query photos eligible for cleanup (LRU - least recently accessed)
        var eligiblePhotos = await _dbContext.Media
            .Where(m => m.LocalCachePath != null
                        && !m.IsCachePinned
                        && !m.Favorite
                        && !m.ProviderFavorite
                        && (m.Rating < settings.HighRatedThreshold || !settings.AlwaysKeepHighRated))
            .OrderBy(m => m.LocalCacheLastAccessUtc) // LRU
            .ThenBy(m => m.Rating) // Then by rating (lowest first)
            .ThenBy(m => m.ShownCount) // Then by view count
            .ToListAsync();

        // Execute cleanup...
        var result = new CacheCleanupResult { WasNeeded = true };
        long freedBytes = 0;

        foreach (var photo in eligiblePhotos)
        {
            if (freedBytes >= bytesToFree) break;

            // Delete file and update database
            File.Delete(photo.LocalCachePath);
            freedBytes += photo.LocalCacheFileSizeBytes ?? 0;
            result.PhotosDeleted++;

            photo.LocalCachePath = null;
            photo.LocalCacheFileSizeBytes = null;
        }

        await _dbContext.SaveChangesAsync();
        return result;
    }
}
```

---

## Database Migrations

```sql
-- Add provider-based columns to Media table
ALTER TABLE Media ADD COLUMN ProviderId TEXT NULL;
ALTER TABLE Media ADD COLUMN ProviderType TEXT NULL;
ALTER TABLE Media ADD COLUMN ProviderPhotoId TEXT NULL;
ALTER TABLE Media ADD COLUMN ContainerId TEXT NULL;
ALTER TABLE Media ADD COLUMN ContainerPath TEXT NULL;
ALTER TABLE Media ADD COLUMN ContainerType TEXT NULL;
ALTER TABLE Media ADD COLUMN ProviderFavorite INTEGER NOT NULL DEFAULT 0;

-- Enhanced metadata
ALTER TABLE Media ADD COLUMN FileName TEXT NULL;
ALTER TABLE Media ADD COLUMN CameraModel TEXT NULL;
ALTER TABLE Media ADD COLUMN Latitude REAL NULL;
ALTER TABLE Media ADD COLUMN Longitude REAL NULL;
ALTER TABLE Media ADD COLUMN LocationName TEXT NULL;

-- Smart filtering metadata
ALTER TABLE Media ADD COLUMN AddedToIndexUtc TEXT NULL;

-- Photo marking
ALTER TABLE Media ADD COLUMN NeverShowAgain INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Media ADD COLUMN MarkedForDeletion INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Media ADD COLUMN MarkedForDeletionUtc TEXT NULL;
ALTER TABLE Media ADD COLUMN MarkedForDeletionReason TEXT NULL;

-- Cache management
ALTER TABLE Media ADD COLUMN LocalCachePath TEXT NULL;
ALTER TABLE Media ADD COLUMN LocalCacheFileSizeBytes INTEGER NULL;
ALTER TABLE Media ADD COLUMN LocalCacheDownloadedUtc TEXT NULL;
ALTER TABLE Media ADD COLUMN LocalCacheLastAccessUtc TEXT NULL;
ALTER TABLE Media ADD COLUMN IsCachePinned INTEGER NOT NULL DEFAULT 0;

-- Sync metadata
ALTER TABLE Media ADD COLUMN LastSyncUtc TEXT NULL;
ALTER TABLE Media ADD COLUMN ETag TEXT NULL;

-- Indexes for performance
CREATE INDEX IX_Media_ProviderId ON Media(ProviderId);
CREATE INDEX IX_Media_ProviderPhotoId ON Media(ProviderPhotoId);
CREATE INDEX IX_Media_ProviderFavorite ON Media(ProviderFavorite);
CREATE INDEX IX_Media_AddedToIndexUtc ON Media(AddedToIndexUtc);
CREATE INDEX IX_Media_NeverShowAgain ON Media(NeverShowAgain);
CREATE INDEX IX_Media_MarkedForDeletion ON Media(MarkedForDeletion);
CREATE INDEX IX_Media_LocalCacheLastAccessUtc ON Media(LocalCacheLastAccessUtc);
CREATE INDEX IX_Media_IsCachePinned ON Media(IsCachePinned);

-- Composite indexes for complex queries
CREATE INDEX IX_Media_SmartFilter ON Media(Rating, Favorite, ProviderFavorite, TakenUtc, AddedToIndexUtc);
CREATE INDEX IX_Media_CacheCleanup ON Media(IsCachePinned, Favorite, ProviderFavorite, Rating, LocalCacheLastAccessUtc);
```

---

## Dependency Injection Setup

```csharp
// MauiProgram.cs

// Provider factory
builder.Services.AddSingleton<IPhotoProviderFactory, PhotoProviderFactory>();

// OneDrive integration
builder.Services.AddSingleton<IGraphClient, GraphClient>();
builder.Services.AddSingleton<ITokenStore, TokenStore>();

// Sync service
builder.Services.AddSingleton<IPhotoSyncService, UnifiedPhotoSyncService>();
builder.Services.AddHostedService<UnifiedPhotoSyncService>(sp =>
    sp.GetRequiredService<IPhotoSyncService>() as UnifiedPhotoSyncService);

// Cache management
builder.Services.AddSingleton<IPhotoCacheManager, PhotoCacheManager>();

// Photo marking
builder.Services.AddScoped<IPhotoMarkingService, PhotoMarkingService>();
```

---

## Implementation Plan

### Phase 1: Foundation & OneDrive Provider (Weeks 1-3)
**Goal:** Set up extensible architecture and implement first provider (OneDrive)

**Tasks:**
1. Create core abstractions (IPhotoProvider, PhotoContainer, PhotoMetadata)
2. Create unified configuration models (PhotoSourceSettings, etc.)
3. Implement PhotoProviderFactory
4. Implement OneDrivePhotoProvider
5. Add OneDrive authentication with device code flow
6. Test authentication and folder browsing

**Deliverable:** User can authenticate with OneDrive and browse folders

---

### Phase 2: Sync Service & Photo Indexing (Weeks 4-5)
**Goal:** Sync photos from OneDrive to local database

**Tasks:**
1. Create UnifiedPhotoSyncService
2. Implement full folder sync
3. Add photo enumeration and metadata extraction
4. Create database migration for new columns
5. Test end-to-end sync

**Deliverable:** Photos from OneDrive appear in FamilyWall slideshow

---

### Phase 3: Multi-Account & Delta Sync (Week 6)
**Goal:** Support multiple accounts and efficient incremental sync

**Tasks:**
1. Support multiple photo sources in configuration
2. Implement delta sync for OneDrive
3. Store delta tokens per container
4. Test with multiple OneDrive accounts

**Deliverable:** Multiple OneDrive accounts sync efficiently

---

### Phase 4: Local/NAS Provider (Week 7)
**Goal:** Add local folder and NAS support

**Tasks:**
1. Implement LocalDriveProvider
2. Implement NASProvider (extends LocalDriveProvider)
3. Add file system watcher for auto-sync
4. Test with local folders and network shares

**Deliverable:** Photos from local drives and NAS appear in slideshow

---

### Phase 5: Favorite Sync (Week 8)
**Goal:** Bidirectional favorite sync with providers

**Tasks:**
1. Read favorite status from OneDrive
2. Implement SetFavoriteStatusAsync in OneDrivePhotoProvider
3. Add background service to sync favorites back
4. Test bidirectional sync

**Deliverable:** Favorites sync between FamilyWall and OneDrive

---

### Phase 6: Smart Filtering Algorithms (Weeks 9-10)
**Goal:** Implement 6 slideshow algorithms

**Tasks:**
1. Implement all 6 algorithms (Smart, Favorites, Star-Based, High-Low, Unmarked, Random)
2. Add view count tracking
3. Create algorithm selection UI
4. Test each algorithm

**Deliverable:** User can select from 6 different slideshow algorithms

---

### Phase 7: Cache Management (Week 11)
**Goal:** Intelligent local photo caching

**Tasks:**
1. Implement PhotoCacheManager
2. Add LRU cleanup algorithm
3. Implement auto-pin logic for favorites
4. Create cache statistics UI
5. Test cache cleanup

**Deliverable:** Automatic cache management keeps disk usage under control

---

### Phase 8: Photo Marking System (Week 12)
**Goal:** Advanced photo curation

**Tasks:**
1. Implement PhotoMarkingService
2. Create photo marking UI overlay
3. Add keyboard shortcuts
4. Implement batch operations
5. Add undo support

**Deliverable:** Complete photo marking and curation system

---

### Phase 9: Polish & Testing (Weeks 13-14)
**Goal:** Production-ready feature

**Tasks:**
1. Performance optimization
2. Error handling improvements
3. Unit and integration tests
4. User acceptance testing
5. Documentation

**Deliverable:** Production-ready extensible photo provider system

---

## Microsoft Graph API Endpoints

| Endpoint | Purpose | Permissions |
|----------|---------|-------------|
| `GET /me/drive/root/children` | List root folders | Files.Read |
| `GET /me/drive/items/{id}/children` | List folder contents | Files.Read |
| `GET /me/drive/items/{id}/delta` | Incremental sync | Files.Read |
| `GET /me/drive/items/{id}` | Get file metadata | Files.Read |
| `GET /me/drive/items/{id}/thumbnails` | Get thumbnails | Files.Read |
| `GET /me/drive/items/{id}/content` | Download file | Files.Read |
| `PATCH /me/drive/items/{id}` | Update metadata (favorites) | Files.ReadWrite |

---

## Performance Considerations

### Sync Performance

**Initial Sync:**
- 1,000 photos: ~5-10 minutes
- 10,000 photos: ~30-60 minutes
- Bottleneck: Thumbnail downloads

**Delta Sync:**
- 10-100 new photos: ~30-60 seconds
- Runs every 30 minutes in background

**Optimizations:**
- Batch API calls (max 20 items per request)
- Parallel thumbnail downloads (max 5 concurrent)
- Local thumbnail caching
- Delta tokens for incremental sync
- Database indexes on key columns

### Algorithm Performance

| Algorithm | Query Complexity | Performance (10K photos) |
|-----------|------------------|-------------------------|
| Smart | High (multiple filters + scoring) | <150ms |
| Favorites | Low (single filter) | <10ms |
| Star-Based | Medium (grouping + weighting) | <50ms |
| High-Low | Low (alternating filters) | <10ms |
| Unmarked | Low (single filter) | <10ms |
| Random | Medium (count + skip) | <30ms |

---

## Security Considerations

1. **Token Storage**: Use DPAPI (Windows) / Keychain (macOS/iOS) for token encryption
2. **Scope Minimization**: Request only `Files.Read` initially, upgrade to `Files.ReadWrite` for favorites
3. **User Consent**: Clear messaging about data access
4. **Local Storage**: Database in app-specific directory (user-level permissions)
5. **HTTPS Only**: All Graph API calls over HTTPS
6. **Token Refresh**: Handle gracefully with user re-authentication
7. **Multi-Account Isolation**: Separate token storage per account

---

## Testing Strategy

### Unit Tests
- OneDriveClient methods (mocked Graph API)
- Smart filtering algorithm (various scenarios)
- Configuration parsing and validation
- Token storage encryption/decryption

### Integration Tests
- End-to-end sync with real OneDrive account
- Multi-account authentication
- Delta sync over multiple cycles
- Favorite bidirectional sync

### Performance Tests
- Large folder sync (10,000+ photos)
- Concurrent account sync
- Smart filtering with large database
- Memory usage monitoring

---

## Estimated Timeline

**Total: 14 weeks from start to production**

- Phase 1-3: OneDrive foundation (Weeks 1-6)
- Phase 4: Local/NAS support (Week 7)
- Phase 5: Favorite sync (Week 8)
- Phase 6-7: Smart filtering + cache (Weeks 9-11)
- Phase 8: Photo marking (Week 12)
- Phase 9: Polish & testing (Weeks 13-14)

---

## Conclusion

This technical architecture provides:

✅ **Extensible Design**: `IPhotoProvider` interface enables any provider
✅ **OneDrive Complete**: Full implementation with delta sync, favorites, metadata
✅ **Local Support**: LocalDrive and NAS providers with file watchers
✅ **Performance**: Indexed queries, LRU caching, delta sync
✅ **Scalability**: Handles 10,000+ photos efficiently
✅ **Maintainability**: Clean separation of concerns, testable
✅ **Future-Ready**: Google Photos, iCloud easily added

The implementation plan is realistic and iterative, delivering value at each phase while building toward the complete vision.
