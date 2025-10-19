# Design Phase Checklist

Track and resolve the key questions and challenges before implementation. Check off items as decisions are made and documented in `design/architecture.md` and related docs.

Legend: [ ] open • [x] resolved/decided

## Cross-cutting alignment
- [x] Recording defaults conflict: reconcile "recording off by default" vs "Night Mode recording on by default (3 days)"; define first-run consent UX and storage/retention controls.
- [ ] Mode defaults: decide whether to always open Calendar on presence or resume last-used mode; consider time-of-day context rules.
Make it configurable: alsways use a specific mode, or always use last mode

- [ ] Presence heuristic: specify dwell-time/velocity thresholds so passersby don't trigger interactive mode; define region-of-interest.
Make a best effort estimation; and make this configurable. It will require testing to fine tune

- [ ] Offline scope: define calendar cache TTL, photo prefetch depth, and degraded behavior for Home Control.
The goal is to be able to keep working with limited disruptions, show last image, show cached 7 day calendar; should be connected again withing hours.

- [ ] Accessibility targets: finalize text sizes, contrast, and touch targets for 1–3 m viewing across Surface sizes; decide on auto or per-device scaling.
Do a best effort design, will finetune based on user feedback

## Platform and app shell (Blazor Hybrid on .NET 9 MAUI)
- [ ] Kiosk strategy on Windows: Assigned Access vs auto-login + auto-launch vs custom shell; handling Windows updates and OS chrome suppression.
This device should get it's own loging, that is automatically logged in. This login has access to all resources necessary, but no more.

- [ ] Camera integration path: WinRT/MediaCapture via MAUI with low-latency preview; define interop boundary with Blazor (WebView2 vs native view overlay).
preview native and UI either native overlays (XAML) or adjacent Blazor

- [ ] Background/lifecycle: policy for keeping camera pipeline warm while screen/backlight is off; Modern Standby settings and permissions.
No idea yet

- [ ] Rendering path: choose between WebView2/CSS animations vs native (WinUI/Skia) for smooth transitions and compositing.

## Presence and motion detection
- [ ] Primary sensor strategy: camera motion vs face detection vs PIR; fallback order and conflict resolution.

- [ ] Wake latency: plan to meet <300 ms wake-to-live; keep capture graph warm; mitigate device re-enumeration.
Yes

- [ ] False positives: define filters/hysteresis for pets, headlights, shadows; day/night threshold profiles.
Not yet


## Photo module
- [ ] Source unification: OneDrive + NAS ID strategy; content hashing for dedupe; moved/renamed detection.
- [ ] "Same date" selection: ±N-day window, multi-photo per year rules, look-back range, tie-breakers by rating/quality.
- [ ] Quality/duplicate avoidance: heuristics (pHash, EXIF noise/blur, face count) and thresholds.
- [ ] Ratings storage: EXIF/XMP sidecars vs SQLite; cross-device sync and conflict policy.
- [ ] Maps: provider choice, offline tiles policy/licensing, key storage, and UI performance budget.
- [ ] Prefetch/caching: number of photos to pre-decode; memory guardrails; 60 fps transition target.

## Calendar module
- [ ] Identity/auth: shared calendar approach (shared mailbox/calendar/delegation) for a family; kiosk auth flow (Device Code), token storage/renewal.
- [ ] Birthdays: data source (Contacts vs dedicated calendar), all-day handling, timezone normalization.
Please use shared contact list, or calendar birthday

- [ ] Offline edits: conflict resolution, retry/backoff, user feedback for pending/failed sync on kiosk UI.
No offline edits; when offline, disable editing

- [ ] Filters/ownership: model for "for whom" tagging; color mapping; fast toggles within 2 taps.

## Night Mode
- [ ] Entry/exit precedence: schedule vs manual vs ambient; how manual overrides are cleared.
- [ ] Live view UX: animation to reduce abruptness, inactivity timers, and touch-to-dismiss conventions.
- [ ] Recording model: ring buffer size, clip duration, default 3-day retention, encryption (DPAPI) and review/delete UI.

## Home Assistant (HA) integration
- [ ] MVP approach: embedded Lovelace UI vs native controls vs hybrid; risk and scope trade-offs.
Us a hybrid approach; start with embedding; but later make the most important contrls natively available.

## State machine and UX flow
- [ ] Define full state machine with transitions and priorities among Passive, Calendar, Photos, Home, Night, Night Live.
Sounds overkill, but if needed

## Data and storage
- [ ] SQLite schema: Photos, People/Tags, Ratings, Sources, Calendar cache, UI State, Presence events; required indexes and retention policies.

## Security and privacy
- [ ] Network boundaries: LAN-only mode, firewall rules, certificate management.
- [ ] Telemetry: opt-in diagnostics, log retention, local-only defaults.

## Performance and power
- [ ] KPIs and measurement: define wake-to-live, frame rate, hitch budget; plan on-device measurements.
- [ ] Power policies: day-time sleep suppression and night-time backlight behavior with Modern Standby; required privileges.
- [ ] Stability: watchdog/auto-restart strategy; memory leak monitoring and recovery.

## Offline and resilience
- [ ] Cache TTLs: calendar and photo index behavior during outages; degraded UI messages.
- [ ] Backpressure and retries: network flap handling without UI stalls; background queues with exponential backoff.

## Build, deployment, and updates
- [ ] Packaging: MSIX signing, App Installer/winget update channel; rollback strategy.
- [ ] Autostart and recovery: post-update relaunch, crash recovery, watchdog service vs scheduled task.
- [ ] Config management: JSON schema, import/export, secret redaction.

## Testing, observability, and ops
- [ ] Simulators: presence/time travel/network toggles for reliable test scenarios.
- [ ] Logging: structured logs with circular buffers and on-device viewer.
- [ ] Metrics: frame-time histograms, motion false-positive rate, calendar sync latency dashboard (optional).

## Hardware variability
- [ ] Camera diversity: handle different Surface sensors, low-light performance, driver quirks; PIR fallback.
- [ ] External sensors: USB/PIR/MQTT discovery and calibration; time sync considerations.

## Licensing and third-party
- [ ] Maps licensing and offline cache policy; key storage.
- [ ] OpenCV/ML redistribution/licensing considerations for MAUI/Windows.
