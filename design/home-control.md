# Home Control – User Requirements

This document details the user-facing requirements for the Home Control mode. Technical design and integrations are in `architecture.md`.

## 1. Objective
Provide an optional, touch-first control panel to view home status and control devices and scenes directly from the wall display.

## 2. Entry
- From Interactive Mode, select the Home tab or an equivalent entry point
- Optionally triggerable via a presence gesture when in Passive Mode

## 3. Core Capabilities
- Rooms grid showing key devices per room (e.g., lights, scenes)
- Quick Actions bar with favorites and global toggles (e.g., All Lights Off)
- Scenes row for one-tap activation (e.g., Dinner, Movie, Bedtime)
- Device drawer to search and mark favorites
- Optional floorplan view with tappable elements

## 4. Feedback & State
- Device state should update in near real time
- Actions should feel immediate with optimistic UI and clear success/error feedback
- If the home system is unreachable, show a minimal offline view and a reconnect prompt

## 5. Security & Privacy
- Require explicit user consent to connect to a home system
- Clearly indicate when the microphone is listening if voice is enabled
- Store only the minimum configuration necessary; allow easy sign-out and token revocation

## 6. Accessibility & Kiosk Considerations
- Large touch targets suitable for family use (minimum ~48×48dp)
- Full-screen kiosk presentation with minimal OS chrome
- Prevent unintended sleep during active day-time use

## 7. Voice (Optional)
- Push-to-talk button or optional hotword
- Display the understood command and the result

## 8. Reliability
- Cache last known states and favorites for quick startup
- Queue user actions if temporarily offline and apply on reconnect (without duplication)
