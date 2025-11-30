# Photo Provider Integration - Functional Requirements

## Overview

Build an extensible photo provider system for FamilyWall that allows users to connect multiple photo sources (OneDrive, Google Photos, local drives, NAS, iCloud, etc.) and display them in an intelligent slideshow with advanced curation features.

## Goals

1. **Multiple Photo Sources**: Connect to various photo sources through a unified interface
2. **Multi-Account Support**: Allow multiple accounts per provider type (e.g., family members' OneDrive accounts)
3. **Multi-Container Support**: Select specific folders/albums from each account
4. **Rich Metadata Display**: Show comprehensive photo information (location, date, camera, GPS)
5. **Favorite Sync**: Bidirectional favorite sync with cloud providers
6. **Intelligent Slideshow**: Smart algorithms to show the right photos at the right time
7. **Photo Curation**: Rate, favorite, hide, or mark photos for deletion
8. **Automatic Cache Management**: Manage local storage intelligently
9. **Mixed Sources**: Combine photos from all sources in a single slideshow

---

## User Stories

### Story 1: Connect Multiple Photo Sources
**As a user**, I want to connect multiple photo sources (OneDrive, local folders, NAS) so that all my family photos appear in the slideshow regardless of where they're stored.

**Acceptance Criteria:**
- User can add multiple photo sources from a unified settings screen
- Supported providers: OneDrive, Local Folder, Network Drive (NAS)
- Future providers: Google Photos, iCloud (shown as "coming soon")
- Each source has a friendly display name
- Sources can be enabled/disabled independently
- Sources can be prioritized for sync order

### Story 2: OneDrive Authentication
**As a user**, I want to authenticate with my Microsoft account using a device code so I can access my OneDrive photos without entering passwords on the tablet.

**Acceptance Criteria:**
- Device code flow authentication (user-friendly for tablets)
- Display code and instructions clearly
- Show authentication status (connected/disconnected)
- Multiple OneDrive accounts supported (e.g., "Dad's OneDrive", "Mom's OneDrive")
- Sign out option per account

### Story 3: Folder/Album Selection
**As a user**, I want to select specific folders from each photo source so I have control over which photos appear in the slideshow.

**Acceptance Criteria:**
- Browse folder/album structure for each source
- Checkbox selection for folders
- Recursive option (include subfolders)
- Show photo count per folder (if available)
- Selected folders persist across app restarts
- Last sync time displayed per folder

### Story 4: Local Folder Support
**As a user**, I want to add local folders from my computer so I can include photos stored on this device.

**Acceptance Criteria:**
- Browse and select local folders
- Optional automatic sync when files change (file watcher)
- No authentication required
- Works offline
- Display folder path clearly

### Story 5: Network Drive (NAS) Support
**As a user**, I want to connect to my home NAS so I can include our family photo archive in the slideshow.

**Acceptance Criteria:**
- Enter network path (e.g., `\\nas\photos`)
- Optional username/password for authentication
- Automatic connection on startup
- Show connection status
- Optional file watcher for automatic sync

### Story 6: Photo Metadata Display
**As a user**, I want to see rich information about each photo (when/where it was taken, camera used, original folder) to provide context during the slideshow.

**Acceptance Criteria:**
- Display filename and folder path
- Show date/time taken (from EXIF)
- Show location name and GPS coordinates (if available)
- Show camera model (if available)
- Optional metadata overlay (can be toggled on/off)
- Metadata updates automatically when photo changes

### Story 7: Favorite Sync (OneDrive)
**As a user**, I want photos I've marked as favorites in OneDrive to appear more frequently, and I want favorites I mark in FamilyWall to sync back to OneDrive.

**Acceptance Criteria:**
- Read favorite status from OneDrive during sync
- Favorite indicator visible in slideshow
- Toggle favorite status directly in FamilyWall
- Changes sync back to OneDrive within 5 minutes
- Handle conflicts gracefully (OneDrive wins)

### Story 8: Smart Slideshow Algorithms
**As a user**, I want to choose from different slideshow modes so I can control which photos appear and how often.

**Acceptance Criteria:**
- **6 Algorithm Options:**
  1. **Smart** - Intelligent weighted scoring (same-day history, recent, ratings, favorites)
  2. **Favorites First** - Show only favorites and highly-rated photos (4-5 stars)
  3. **Star-Based** - Probability selection by rating (5★=80%, 4★=60%, etc.)
  4. **High-Low** - Alternate between high-quality and variety
  5. **Unmarked** - Only show unrated/unfavorited photos (for review mode)
  6. **Random** - Complete shuffle with no filtering

- Algorithm selector in Settings with clear descriptions
- Algorithm-specific settings appear when selected
- Preview of next 10 photos with current settings
- Changes take effect immediately

### Story 9: Smart Algorithm - Same Day in History
**As a user**, I want the slideshow to prioritize photos taken on this day in previous years (e.g., March 15 in 2020, 2021, 2022) to relive memories.

**Acceptance Criteria:**
- Checks current date and finds photos taken within ±3 days in any past year
- Configurable day window (1-7 days)
- Prioritizes these "throwback" photos
- Falls back to other photos if no matches
- Can be enabled/disabled
- Weight multiplier configurable (default: 3.0x)

### Story 10: Smart Algorithm - Recent Photos
**As a user**, I want recently added photos to appear more frequently so I see the latest family moments.

**Acceptance Criteria:**
- Tracks when photos were first indexed
- Prioritizes photos added within last 30 days (configurable 7-90 days)
- Can be enabled/disabled
- Weight multiplier configurable (default: 2.0x)

### Story 11: Star Ratings
**As a user**, I want to rate photos with 1-5 stars so I can curate my collection and control slideshow frequency.

**Acceptance Criteria:**
- Click stars to rate (1-5, or 0 for unrated)
- Ratings stored locally
- Keyboard shortcut: press 1-5 to rate current photo
- Highly-rated photos appear more often in Smart algorithm
- Star-Based algorithm uses probability by rating
- Ratings visible in photo management screen

### Story 12: Photo Marking - Never Show Again
**As a user**, I want to hide certain photos from the slideshow without deleting them.

**Acceptance Criteria:**
- "Hide" button during slideshow
- Keyboard shortcut: H
- Hidden photos excluded from all slideshow algorithms
- Can view/manage hidden photos in Settings
- Undo support (5-second toast notification)
- Batch hide option in photo management

### Story 13: Photo Marking - Mark for Deletion
**As a user**, I want to flag photos for batch deletion so I can clean up my collection.

**Acceptance Criteria:**
- "Mark for Deletion" button during slideshow
- Keyboard shortcut: D
- Optional reason/note when marking
- Marked photos excluded from slideshow
- View all marked photos in Settings
- Batch delete with confirmation dialog
- Option to delete from source (OneDrive, local, etc.)
- Undo support

### Story 14: Cache Management
**As a user**, I want the app to automatically manage disk space so I don't run out of storage while ensuring my favorite photos are always available offline.

**Acceptance Criteria:**
- Configurable maximum cache size (1GB - 50GB or unlimited)
- Automatic cleanup when cache exceeds 90% of limit
- Favorites and high-rated photos (4-5 stars) protected from cleanup
- Minimum photo count guaranteed (e.g., 500 photos always available)
- LRU cleanup (least recently viewed deleted first)
- Cache statistics displayed (size, photo count, % used)
- Manual "Cleanup Now" button
- "Clear All Cache" option with confirmation

### Story 15: Photo Management Screen
**As a user**, I want a dedicated screen to review, filter, and manage all my photos.

**Acceptance Criteria:**
- **Tabs:**
  - All Photos
  - Favorites
  - High Rated (4-5 stars)
  - Never Show Again
  - Marked for Deletion
- Thumbnail grid view
- Metadata overlay on hover
- Quick actions (favorite, rate, hide, delete mark)
- Batch selection mode
- Filters: source, folder, date range
- Batch operations: rate, hide, delete
- Confirmation dialogs for destructive actions

### Story 16: Incremental Sync
**As a user**, I want photo sync to be fast and efficient so I'm not waiting for long sync operations.

**Acceptance Criteria:**
- OneDrive uses delta queries (only changed photos)
- Local/NAS use file watchers (automatic detection)
- Initial sync shows progress indicator
- Periodic background sync (every 30 minutes)
- Manual "Sync Now" option
- Sync status indicator (last sync time, photo count)

### Story 17: Keyboard Navigation
**As a user**, I want to control the slideshow and photo marking with keyboard shortcuts for efficiency.

**Acceptance Criteria:**
- **1-5**: Set star rating
- **F**: Toggle favorite
- **H**: Hide photo (never show again)
- **D**: Mark for deletion
- **Z**: Undo last action
- **Space/Right Arrow**: Next photo
- **Left Arrow**: Previous photo
- **Esc**: Exit slideshow

---

## UI Requirements

### Settings Screen - Photo Sources Section

```
┌─────────────────────────────────────────────────────────┐
│  Photo Sources                                          │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │  [Add Photo Source ▼]                          │    │
│  │    • Microsoft OneDrive                        │    │
│  │    • Google Photos (coming soon)               │    │
│  │    • Local Folder                              │    │
│  │    • Network Drive (NAS)                       │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ 📁 Dad's OneDrive                  [Edit] [✗] │    │
│  │ Type: OneDrive | Status: ✓ Connected          │    │
│  │ Folders: 2 selected | Photos: 1,234           │    │
│  │ Last sync: 5 minutes ago                      │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ 💾 Local Pictures                  [Edit] [✗] │    │
│  │ Type: Local Folder | Path: C:\Users\...\Pics  │    │
│  │ Folders: 1 selected | Photos: 456             │    │
│  │ Last scan: 2 hours ago                        │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  Total: 1,690 photos from 2 sources                    │
└─────────────────────────────────────────────────────────┘
```

### Add Photo Source Wizard

**Step 1: Select Provider Type**
```
What type of photo source do you want to add?

┌───────────────────────────────────────────────┐
│  ☁️  Cloud Providers                         │
│  ┌─────────────────────────────────────────┐ │
│  │ 📁 Microsoft OneDrive                   │ │
│  │ Sync photos from your OneDrive account │ │
│  └─────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────┐ │
│  │ 📸 Google Photos (coming soon)          │ │
│  │ Sync albums from Google Photos          │ │
│  └─────────────────────────────────────────┘ │
│                                               │
│  💾  Local Sources                           │
│  ┌─────────────────────────────────────────┐ │
│  │ 📂 Local Folder                         │ │
│  │ Photos from a folder on this computer   │ │
│  └─────────────────────────────────────────┘ │
│  ┌─────────────────────────────────────────┐ │
│  │ 🖧 Network Drive (NAS)                  │ │
│  │ Photos from a network share or NAS      │ │
│  └─────────────────────────────────────────┘ │
└───────────────────────────────────────────────┘
```

**Step 2a: OneDrive Setup**
```
Configure Microsoft OneDrive

Display Name: [Dad's OneDrive                    ]

[ Authenticate with Microsoft ]

Status: ⏳ Not yet authenticated
```

**Step 2b: Local Folder Setup**
```
Configure Local Folder

Display Name: [Local Pictures                    ]

Folder Path:  [C:\Users\Dad\Pictures] [Browse...]

[✓] Watch for changes (auto-sync new photos)
[✓] Follow symbolic links

[Cancel]  [Next: Select Folders]
```

**Step 2c: Network Drive Setup**
```
Configure Network Drive (NAS)

Display Name:    [NAS Family Photos               ]

Network Path:    [\\nas\photos        ] [Browse...]

Authentication (optional):
Username:        [admin                           ]
Password:        [••••••••••                      ]

[✓] Watch for changes (auto-sync new photos)

[Cancel]  [Next: Select Folders]
```

**Step 3: Select Folders/Albums**
```
Select folders to sync from "Dad's OneDrive"

[✓] Camera Roll (456 photos)
    [✓] Vacation 2024 (123 photos)
    [ ] Screenshots (0 photos)
[✓] Family Events (789 photos)
    [✓] Birthday 2024 (45 photos)
    [✓] Christmas 2023 (67 photos)
[ ] Work (234 photos)

[Select All] [Deselect All]

[Cancel]  [Back]  [Finish]
```

### Settings Screen - Slideshow Algorithm Section

```
┌─────────────────────────────────────────────────────────┐
│  Slideshow Algorithm                                    │
│                                                         │
│  Algorithm: [Smart ▼]                                   │
│    • ⚡ Smart - Intelligent weighted scoring           │
│    • ⭐ Favorites First - Best photos only             │
│    • 🎲 Star-Based - Probability by rating             │
│    • ↕️ High-Low - Alternating quality                 │
│    • 📋 Unmarked - Review mode                         │
│    • 🔀 Random - Complete shuffle                      │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ Smart Algorithm Settings                        │   │
│  │                                                 │   │
│  │ Filters:                                        │   │
│  │ [✓] Same Day in History  (window: ±3 days)     │   │
│  │     Weight: ████████████─────── 3.0x           │   │
│  │                                                 │   │
│  │ [✓] Recent Photos  (last 30 days)              │   │
│  │     Weight: ████████─────────── 2.0x           │   │
│  │                                                 │   │
│  │ [✓] High Ratings  (4-5 stars)                  │   │
│  │     Weight: ██████████────────── 2.5x          │   │
│  │                                                 │   │
│  │ [✓] Favorites                                   │   │
│  │     Weight: ████████████████──── 4.0x          │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  [Preview Next 10 Photos]                               │
└─────────────────────────────────────────────────────────┘
```

### Settings Screen - Cache Management Section

```
┌─────────────────────────────────────────────────────────┐
│  Cache Management                                       │
│                                                         │
│  Current Status:                                        │
│  ┌─────────────────────────────────────────────────┐   │
│  │ 3.2 GB used / 5.0 GB max  (64%)                 │   │
│  │ ████████████████████────────────────            │   │
│  │ 1,234 photos cached (456 pinned)                │   │
│  │ Last cleanup: 2 hours ago (freed 234 MB)        │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
│  Settings:                                              │
│  Max cache size:      ████████──────── 5 GB            │
│  Retention period:    ████████──────── 30 days         │
│  Minimum photos:      ████───────────── 500            │
│                                                         │
│  Cleanup triggers:                                      │
│  Start at:  ████████████████████─ 90% full             │
│  Target:    ██████████████──────── 70% full            │
│                                                         │
│  Protection:                                            │
│  [✓] Always keep favorites                              │
│  [✓] Always keep high-rated (4-5 stars)                │
│  [✓] Auto cleanup enabled                               │
│                                                         │
│  [Cleanup Now]  [Clear All Cache]  [Rebuild Cache]      │
└─────────────────────────────────────────────────────────┘
```

### Slideshow - Photo Display with Controls

```
┌────────────────────────────────────────────┐
│                 Photo Image                 │
│                                             │
│  [Metadata Info]                    [Time]  │ ← Top overlay
│                                             │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │ Filename: IMG_1234.jpg               │  │ ← Bottom overlay
│  │ Folder: /Photos/Vacation 2024        │  │
│  │ Taken: July 15, 2024 at 3:45 PM      │  │
│  │ Location: Paris, France              │  │
│  │ Camera: iPhone 15 Pro                │  │
│  │                                      │  │
│  │ ⭐⭐⭐⭐☆  ♥️ Favorite  [Hide] [🗑️]    │  │ ← Quick actions
│  └──────────────────────────────────────┘  │
└────────────────────────────────────────────┘

Keyboard Shortcuts:
- 1-5: Set rating
- F: Toggle favorite
- H: Hide photo
- D: Mark for deletion
- Z: Undo
- Space/Right: Next photo
- Left: Previous photo
```

### Photo Management Screen

```
┌─────────────────────────────────────────────────────────┐
│  Photo Management                                       │
│                                                         │
│  [All] [Favorites] [High Rated] [Hidden] [Marked]      │
│                                                         │
│  Filters:  [Source: All ▼] [Folder: All ▼]             │
│           [Date: All time ▼]                            │
│                                                         │
│  [ ] Select All    [✓ Rate] [❤️ Fav] [👁️ Hide] [🗑️ Del] │
│                                                         │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│  │ Photo1 │ │ Photo2 │ │ Photo3 │ │ Photo4 │          │
│  │ ⭐⭐⭐⭐⭐ │ │ ⭐⭐⭐☆☆ │ │ ⭐⭐⭐⭐☆ │ │ ☆☆☆☆☆  │          │
│  │ ❤️      │ │        │ │ ❤️      │ │        │          │
│  └────────┘ └────────┘ └────────┘ └────────┘          │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐          │
│  │ Photo5 │ │ Photo6 │ │ Photo7 │ │ Photo8 │          │
│  │ ⭐⭐⭐⭐☆ │ │ ⭐⭐☆☆☆ │ │ ⭐⭐⭐⭐⭐ │ │ ☆☆☆☆☆  │          │
│  │        │ │        │ │ ❤️      │ │        │          │
│  └────────┘ └────────┘ └────────┘ └────────┘          │
│                                                         │
│  Showing 1,234 photos                                   │
└─────────────────────────────────────────────────────────┘
```

---

## Algorithm Comparison

| Algorithm | Best Use Case | Selection Strategy | Variety | Focus |
|-----------|---------------|-------------------|---------|-------|
| **Smart** | Balanced experience | Weighted scoring (multiple factors) | High | Balanced quality + nostalgia |
| **Favorites** | Showcase best photos | Favorites and 4-5 stars only | Low | Quality |
| **Star-Based** | Probability-weighted mix | Rating-based probability | Medium-High | Quality with variety |
| **High-Low** | Alternating quality | Alternate high-quality and others | Medium | Balanced with rhythm |
| **Unmarked** | Review/rate new photos | Only unrated/unfavorited | N/A | Discovery |
| **Random** | Complete shuffle | Pure random selection | Highest | Pure variety |

---

## Configuration Examples

### Example 1: OneDrive + Local Folder

```json
{
  "App": {
    "Photos": {
      "PhotoSources": [
        {
          "Id": "onedrive-dad",
          "ProviderType": "OneDrive",
          "DisplayName": "Dad's OneDrive",
          "Enabled": true,
          "Priority": 1,
          "CloudSettings": {
            "TenantId": "consumers",
            "ClientId": "cb394926-7118-4e3a-8507-885a30f376be"
          },
          "SelectedContainers": [
            {
              "ContainerPath": "/Camera Roll",
              "Enabled": true,
              "Recursive": true
            }
          ]
        },
        {
          "Id": "local-pictures",
          "ProviderType": "LocalDrive",
          "DisplayName": "Local Pictures",
          "Enabled": true,
          "Priority": 2,
          "LocalSettings": {
            "RootPath": "C:\\Users\\Dad\\Pictures",
            "WatchForChanges": true
          },
          "SelectedContainers": [
            {
              "ContainerPath": "/Vacation",
              "Enabled": true,
              "Recursive": true
            }
          ]
        }
      ],
      "SmartFiltering": {
        "SelectedAlgorithm": "Smart",
        "EnableSameDayInHistory": true,
        "EnableFavorites": true
      },
      "CacheManagement": {
        "MaxCacheSizeMB": 5120,
        "AutoCleanupEnabled": true
      }
    }
  }
}
```

### Example 2: Multiple Family OneDrive Accounts

```json
{
  "App": {
    "Photos": {
      "PhotoSources": [
        {
          "Id": "onedrive-dad",
          "ProviderType": "OneDrive",
          "DisplayName": "Dad's OneDrive",
          "Enabled": true
        },
        {
          "Id": "onedrive-mom",
          "ProviderType": "OneDrive",
          "DisplayName": "Mom's OneDrive",
          "Enabled": true
        },
        {
          "Id": "onedrive-kids",
          "ProviderType": "OneDrive",
          "DisplayName": "Kids' OneDrive",
          "Enabled": true
        }
      ]
    }
  }
}
```

---

## Success Metrics

1. **Multi-Source Adoption**: 50%+ users add 2+ photo sources
2. **OneDrive Adoption**: 80%+ users connect at least one OneDrive account
3. **Smart Filtering Usage**: 80%+ users enable at least one smart filter
4. **Photo Marking**: 50%+ users rate or mark photos within first week
5. **Cache Efficiency**: Cache stays under configured limit >99% of the time
6. **User Satisfaction**: >4.5/5 rating for photo features

---

## Future Enhancements

### Additional Providers
- Google Photos integration
- iCloud Photos integration
- Dropbox integration
- Flickr integration
- Amazon Photos integration

### AI Features
- Face detection and tagging
- AI-powered photo captioning
- Smart search ("beach photos from 2023")
- Automatic photo quality scoring

### Advanced Features
- Reverse geocoding (GPS → location name)
- Video support in slideshow
- Basic photo editing (crop, rotate, filters)
- Shared albums
- Photo timelines and stories
- Custom smart albums (auto-group by location, date, people)

---

## Conclusion

This feature set transforms FamilyWall into a comprehensive family photo hub by:

✅ Supporting multiple photo sources (cloud + local + NAS)
✅ Providing intelligent slideshow algorithms
✅ Enabling advanced photo curation (ratings, favorites, hiding, deletion)
✅ Managing local storage automatically
✅ Syncing favorites bidirectionally with OneDrive
✅ Displaying rich metadata for context
✅ Offering flexible configuration and control

The design prioritizes user experience while maintaining technical flexibility for future enhancements.
