# FamilyWall MVP Scaffolding – Complete ✅

## What Was Built

A complete .NET 9 MAUI Blazor Hybrid solution scaffolded from scratch with:

### Solution Structure (6 Projects)

1. **FamilyWall.App** – MAUI Blazor Hybrid shell with Windows/Android/iOS/Mac support
2. **FamilyWall.Core** – Domain models, settings, and abstractions
3. **FamilyWall.Infrastructure** – SQLite persistence with EF Core, credential store
4. **FamilyWall.Services** – Background hosted services
5. **FamilyWall.Integrations.Graph** – Microsoft Graph client stub
6. **FamilyWall.Integrations.HA** – Home Assistant + MQTT client stubs

### Core Features Implemented

#### Domain Layer (`FamilyWall.Core`)
- ✅ `Media` model with EXIF metadata, ratings, favorites, shown tracking
- ✅ `MediaTag` for photo tagging
- ✅ `CalendarEvent` with birthday support
- ✅ `AppSettings` with nested config for Photos, Calendar, NightMode, HA, MQTT, Privacy
- ✅ Abstractions: `IPhotoIndex`, `ITokenStore`, `IDbPathProvider`

#### Infrastructure Layer (`FamilyWall.Infrastructure`)
- ✅ `AppDbContext` with EF Core and SQLite
- ✅ Schema with indexes for Media (TakenUtc, Favorite, Source), CalendarEvent (ProviderKey, StartUtc)
- ✅ `PhotoIndex` implementation with smart photo selection (same date in prior years, ±3 days)
- ✅ `CredentialLockerTokenStore` with Windows DPAPI encryption (conditional compilation for cross-platform)
- ✅ `DbPathProvider` for app data directory management

#### Background Services (`FamilyWall.Services`)
- ✅ `PhotoIndexingService` – Stub for scanning NAS/OneDrive/Local sources every 30 minutes
- ✅ `PresenceService` – Stub for camera motion detection and Night Mode orchestration

#### Integration Stubs (`FamilyWall.Integrations.*`)
- ✅ `GraphClient` – Interface for OneDrive photos and Microsoft calendar (Device Code flow ready)
- ✅ `HomeAssistantClient` – WebSocket + REST stub for HA control
- ✅ `MqttClientFactory` – Stub for publishing presence/state and MQTT Discovery

#### MAUI App (`FamilyWall.App`)
- ✅ `MauiProgram.cs` configured with:
  - Generic Host for background services
  - Embedded `appsettings.json` + user override support
  - EF Core SQLite with auto-migration
  - DI wiring for all services and integrations
  - Logging and Blazor WebView with DevTools
- ✅ Blazor Pages:
  - `/photos` – Photo slideshow with next/favorite controls
  - `/calendar` – Event list with birthday highlighting
  - `/settings` – Read-only configuration viewer
- ✅ Navigation updated in `NavMenu.razor`
- ✅ Database auto-created on first run

### Configuration (`appsettings.json`)
- Photo sources (NAS/OneDrive/Local) with priority and enable flags
- Slideshow interval, cache size, prefetch count
- Calendar providers and TTL
- Night Mode schedule (start/end time, motion threshold, inactivity timeout)
- Home Assistant base URL and enable flag
- MQTT broker, port, topic prefix
- Privacy settings (recording consent, retention days)

### Build Status
✅ **Build succeeded** for `net9.0-windows10.0.19041.0` target  
⚠️ Android/iOS targets require platform SDKs (not needed for Surface wall display)

## Next Steps (Post-MVP)

### Immediate (Functional MVP)
1. **Photo Scanning**: Implement `PhotoIndexingService` with `MetadataExtractor` for EXIF
2. **Microsoft Graph**: Add Device Code flow and token refresh in `GraphClient`
3. **Calendar Sync**: Fetch events from Graph `/me/calendar/calendarView`
4. **Image Caching**: Downscale and cache photos for smooth slideshow transitions

### Near-Term (Interactive Features)
5. **Night Mode**: Integrate Windows `MediaCapture` and OpenCV for motion detection
6. **Home Assistant**: Implement WebSocket connection and REST service calls
7. **MQTT Discovery**: Publish entities on boot and state changes
8. **Presence Detection**: Sensor fusion (camera + optional PIR via ESP32)

### Stretch (Polish & Edge Cases)
9. **Ratings/Favorites Sync**: Persist rating changes back to EXIF or sidecar files
10. **Offline Resilience**: Health banner for degraded connectivity
11. **Kiosk Mode**: Auto-launch on login, power management, dedicated user account
12. **MSIX Packaging**: Sign and deploy for clean install on Surface devices

## How to Run

```pwsh
cd c:\repo\FamilyWall
dotnet restore
dotnet build -f net9.0-windows10.0.19041.0
cd src/FamilyWall.App
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

Or open `FamilyWall.sln` in Visual Studio 2022 and press F5.

## Key Files to Review

- `documents/design/architecture.md` – Full technical specifications
- `documents/features/01-structure/plan.md` – Implementation roadmap and best practices
- `src/FamilyWall.App/MauiProgram.cs` – DI and service wiring
- `src/FamilyWall.Infrastructure/Data/AppDbContext.cs` – Database schema
- `src/FamilyWall.Core/Settings/AppSettings.cs` – Configuration model

## Architecture Alignment

✅ Connected-first (local-only) – no owned cloud  
✅ Modular structure with clear separation of concerns  
✅ Generic Host for background services  
✅ SQLite for metadata and caching  
✅ Windows DPAPI for token encryption  
✅ Blazor Hybrid for UI with native fallback paths ready  
✅ Configuration via embedded defaults + user overrides  

The foundation is solid and ready for feature implementation! 🚀
