# Application starter and best practices (FamilyWall)

This note turns the architecture into an actionable starter you can follow to scaffold the app reliably, with clear defaults that match the local-only, Windows-first, MAUI Blazor Hybrid kiosk design.

## Goals and constraints

- Single-process MAUI (.NET 9) Blazor Hybrid app, Windows-first (Surface wall panel), optional background Windows service later if really needed
- Connected-first (local-only): the device is almost always online, but all logic runs locally; no owned cloud. Direct Graph/Google/HA/MQTT from device
- Touch-first UX; kiosk boot, display-off/night mode with camera motion
- Local SQLite for metadata and cache; NAS and OneDrive as photo sources

Assumptions: .NET 9 SDK installed; Windows 11 device; WebView2 runtime present.

## Recommended solution structure

Use a modular layout to keep UI, domain, integrations, and background jobs clean:

- src/FamilyWall.App – MAUI Blazor Hybrid (WinUI) shell; DI, navigation, app lifecycle
- src/FamilyWall.UI – Razor components (shared UI), styles, theming
- src/FamilyWall.Core – Domain models, abstractions, cross-platform primitives
- src/FamilyWall.Infrastructure – SQLite persistence, caching, file/NAS access, settings
- src/FamilyWall.Services – Background hosted services (indexing, prefetch, presence)
- src/FamilyWall.Integrations.Graph – Microsoft Graph client wrapper (photos, calendar)
- src/FamilyWall.Integrations.Google – Google Calendar wrapper (optional)
- src/FamilyWall.Integrations.HA – Home Assistant (WebSocket + REST) + MQTT client
- tests/* – Unit tests per project; smoke tests for app startup and DB migrations

Keep Windows-specific camera and power-management code inside FamilyWall.App (WinUI handlers) behind interfaces defined in Core.

## Key packages (NuGet)

- UI/MAUI: Microsoft.Maui, Microsoft.AspNetCore.Components.WebView.Maui, Microsoft.Web.WebView2
- MVVM & utilities: CommunityToolkit.Mvvm
- Storage: Microsoft.EntityFrameworkCore.Sqlite (or Dapper + Microsoft.Data.Sqlite), SQLitePCLRaw.bundle_e_sqlite3
- Config & logging: Microsoft.Extensions.Options.ConfigurationExtensions, Serilog, Serilog.Sinks.File
- Photos/Imaging: MetadataExtractor (EXIF), SixLabors.ImageSharp (optional), OpenCvSharp4.runtime.win (motion)
- Auth & APIs: Microsoft.Identity.Client (MSAL), Microsoft.Graph, Google.Apis.Calendar.v3 (optional)
- Messaging: MQTTnet
- Web: System.Net.WebSockets.Client, Refit (optional for REST wrappers)

## Hosting and DI pattern (single-process)

- Use the Generic Host inside MAUI to get AddHostedService and Options pattern
- Add background services for: photo indexing, prefetch/image cache warmer, presence/motion fusion, MQTT/HA sync
- Use IOptions<T> for settings and store secrets in Windows Credential Locker (DPAPI)

Minimal Program.cs skeleton (excerpt):

// inside FamilyWall.App
var builder = MauiApp.CreateBuilder();
builder
	.UseMauiApp<App>()
	.ConfigureFonts(fonts => fonts.AddFont("InterVariable.ttf", "Inter"));

// Configuration: read appsettings.json embedded + user-override from local folder
using var cfgStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FamilyWall.App.appsettings.json");
builder.Configuration.AddJsonStream(cfgStream!)
				   .AddJsonFile(Path.Combine(FileSystem.AppDataDirectory, "appsettings.user.json"), optional: true, reloadOnChange: true);

// Services
builder.Services
	.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection("App"))
	.Services
	.AddSingleton<IDbPathProvider, DbPathProvider>()
	.AddDbContext<AppDbContext>() // or add a SqliteConnection factory
	.AddSingleton<IPhotoIndex, PhotoIndex>()
	.AddHostedService<PhotoIndexingService>()
	.AddHostedService<PresenceService>()
	.AddSingleton<IMqttClientFactory, MqttClientFactory>()
	.AddSingleton<IHomeAssistantClient, HomeAssistantClient>()
	.AddSingleton<IGraphClient, GraphClient>()
	.AddSingleton<ITokenStore, CredentialLockerTokenStore>();

return builder.Build();

Notes:
- Keep hosted services lightweight; marshal UI changes via Dispatcher/MainThread
- Use CancellationToken and backoff; persist cursors (last scanned photo, last event sync)

## Data and caching

- SQLite for: media index (path, hash, EXIF, last seen, rating), calendars cache (to reduce chattiness and enable brief offline continuity), user prefs
- Use a local file cache for downscaled images to meet slideshow latency targets
- Favor EF Core migrations for maintainability; keep simple table shapes to enable Dapper swap if needed
- NAS: prefer UNC paths for SMB; for WebDAV mirror to a local folder for low-latency slideshow

Suggested tables (concise):
- Media(id, source, path, sha1, takenUtc, width, height, location, rating, favorite, lastShownUtc, shownCount)
- MediaTag(mediaId, tag)
- CalendarEvent(id/providerCompoundKey, title, startUtc, endUtc, isAllDay, isBirthday, source, etag)

## Auth and secrets

- Device Code flow (MSAL) for Microsoft Graph and Google where used
- Persist refresh tokens via Windows Credential Locker (DPAPI-protected); key by provider and scope
- Build a small AuthService that abstracts: ensure signed-in, token refresh, sign-out, consent

## Sensors, presence, and Night Mode

- Camera capture via MediaCapture/WinUI; analyze with OpenCvSharp at ~10–15 fps when in Night Mode
- Start with minimal differencing thresholds; add morphology/hysteresis only if field-testing shows false positives
- Keep pipeline warm while screen is off; prevent system sleep but allow backlight-off; show explicit LIVE indicator when preview is visible
- Fallback path: PIR (ESP32) via MQTT topic; if no camera/PIR, show low-glare clock

## Home Assistant and MQTT

- MQTTnet for publishing presence, screen state, and optional discovery entities on boot
- HA WebSocket (/api/websocket) for state_changed subscriptions, REST for actions
- Store a Long-Lived Access Token in Credential Locker; validate HTTPS

Topics (runtime):
- home/surface_panel/status → online|offline
- home/surface_panel/motion/state → ON|OFF
- home/surface_panel/screen/state → off|ambient|interactive|night_live

## UI composition

- MAUI Shell + BlazorWebView pages per module: Photos, Calendar, Home Control, Settings, Night Mode
- Use CSS animations in WebView for slides where possible; fall back to native composition only when needed for latency
- Provide large, high-contrast controls for 1–3 m viewing; support swipe and tap

## Kiosk boot and lifecycle

- Create a dedicated Windows local user with auto-login; configure the app to auto-start (Startup folder or Scheduled Task)
- Prevent sleep during day; allow display-off at night via power plan or WinRT APIs
- MSIX package and sign for clean deployment; keep updates manual or via LAN share

## Configuration model

- appsettings.json (embedded defaults) + appsettings.user.json (mutable per device)
- Sections: App, Photos (sources, NAS path, OneDrive options), Calendar (providers), NightMode (schedule, thresholds), HomeAssistant (baseUrl, token), Mqtt (broker, topics), Privacy (recording consent, retentionDays)

## Edge cases to plan for

- Connectivity blips: continue slideshow and read cached calendar; queue UI writes or disable edits briefly with clear indicator. Health banner for degraded connectivity
- Permissions: camera/microphone; network share access for kiosk user
- Large libraries: index in batches with resumable cursors; compute hashes efficiently; skip videos initially if needed
- Stall recovery: watchdog timers restart camera/indexers; persistent health flags

## First milestones (MVP path)

1) App shell + BlazorWebView with three tabs: Photos, Calendar (read-only), Settings
2) SQLite schema + simple migrations; import a local folder of photos and show slideshow
3) Presence service (mock) toggling screen state; hook to UI state machine
4) One provider integration: Microsoft Graph read-only for calendar (Device Code) with cached events; prefer live reads with short cache TTL
5) Night Mode: camera preview on motion (no recording), LIVE indicator, resume after inactivity
6) MQTT: publish status/screen state; optionally subscribe to wake command

Stretch after MVP: Home Assistant embedded view + a few native quick actions; ratings/favorites sync; PIR via ESP32; optional short motion clips with DPAPI encryption and 3-day retention (with explicit consent).

## Minimal acceptance checks

- Cold start < 3 s to splash; slideshow advances smoothly at 4–6 s cadence from cached images or network-backed images with prefetch
- Night Mode wake-to-live < 300 ms after motion
- Connectivity behavior: when online, refreshes calendar within 60 s of changes and prefetches next N photos; during brief outages, shows cached content and indicates degraded state
- No cloud dependencies beyond third-party APIs; tokens never leave device

---

If you want, I can scaffold the folders and a minimal MAUI Blazor Hybrid app skeleton next, including an example appsettings setup and a stubbed PhotoIndexingService.

