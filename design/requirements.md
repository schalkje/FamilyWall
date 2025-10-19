# 🏠 Home Controller Wall Display – Requirements

This document captures user-facing requirements only. All implementation details and technologies are documented in `architecture.md`.

## 1. Overview

Purpose: Replace the traditional wall calendar with a smart, wall-mounted touch display that blends daily scheduling with personal memories. The display shows family photos related to the current date and a shared calendar view. When someone approaches, it opens into one of the interactive modes—Calendar, Photo, or Home Control—for browsing photos, managing the calendar, or controlling the home.

Out of scope for this document: specific platforms, frameworks, devices, or integrations. See `architecture.md` for technical choices.

---

## 2. Modes and Experience

The system operates in distinct modes with clear transitions. There are three interactive modes: Calendar, Photo, and Home Control.

| Mode | Purpose | Interaction | Display requirements |
| --- | --- | --- | --- |
| Passive (default) | Ambient display replacing a wall calendar | No touch | Daily photo (same/similar date in past), current date/time, upcoming appointments and birthdays for today + next 7 days, subtle screen transitions every few minutes |
| Calendar (interactive) | Manage and view calendar items | Touch + proximity | Clear agenda for today + next 7 days, add/edit events, highlight birthdays, filters for views |
| Photo (interactive) | Browse and enjoy family photos | Touch + proximity | Swipeable photo viewer, favorite/rate, details, optional map when location exists |
| Home Control (interactive) | Optional home control panel | Touch + proximity | Control common home devices and scenes; see room status and run automations |
| Night Mode | Bedtime security/low-power behavior | Presence to wake; touch to dismiss | Screen off until motion; on motion, show live camera view; auto-return to black after inactivity |

Detailed requirements for Night Mode and Home Control are maintained in dedicated documents:

- Night Mode Requirements: `night-mode.md`
- Home Control Requirements: `home-control.md`

---

## 3. Photo Experience Requirements

- Photo Of The Day logic shows photos from the same calendar date in prior years; when not available, use a nearby window (e.g., ±3 days).
- Users can:
  - Swipe to next/previous photo
  - Mark as favorite or rate
  - View photo details (date, people, tags)
  - View location on a map when location data is available
  - Browse by date
- The system should avoid showing low-quality or duplicate photos where possible.
- The system should prioritize favorites and high rated photo's, randomly mixed with unrated photo's
- Photos should render quickly with smooth transitions.

---

## 4. Calendar Experience Requirements

- Show today’s events and birthdays and a concise view of the next 7 days (scrollable if needed).
- Clearly highlight birthdays and special dates.
- Users can add or edit appointments and recurring dates using touch input; optional voice input is supported when enabled.
- Users can filter views (e.g., personal, family, work) and quickly toggle visibility.
- Conflicts and overlaps should be visually clear.
- It's a family calendar; events should clearly depict for whom it is applicable; one more or all family members

---

## 5. Presence, Inactivity, and Transitions

- Switch from Passive to the last-used interactive mode when a person is detected in front of the device (configurable default is Calendar on first use).
- people moving before the device without stopping won't trigger passive to interactive mode
- Revert from Interactive to Passive after a period of inactivity (configurable).
- Enter Night Mode via schedule, manual action, or ambient condition; exit by schedule, manual action, or morning conditions.
- Transitions should be smooth and avoid abrupt flashes in low-light conditions.

---

## 6. UI and Styling

- Passive Mode: Calm, minimal aesthetic; full-screen photo with unobtrusive overlays for day/date and agenda.
- Interactive Modes: Touch-friendly layouts with large targets; swipe interactions and a simple mode switcher to move between Calendar, Photo, and Home Control.
- Themes: Light/dark themes should adapt to time of day or user preference.
- Accessibility: Provide sufficient contrast, readable typography from 1–3 meters, and clear focus/selection states.

---

## 7. Non-functional Requirements

- Performance
  - Wake-to-interaction should feel immediate; Night Mode wake-to-live target under 300 ms when feasible.
  - UI interactions should render at smooth frame rates.
- Availability & Resilience
  - The display should remain usable for core tasks even with intermittent connectivity (e.g., show cached agenda/photo).
  - The system should recover gracefully from temporary errors without user intervention.
- Privacy & Security
  - By default, photos and camera frames are processed locally; no cloud uploads without explicit opt-in.
  - Recording is off by default except when in night mode; if enabled, users control retention and storage location; a clear on-screen indicator shows when the live camera is visible.
  - night mode recording is on by default, with a retention of 3 days
- Kiosk & Safety
  - Full-screen operation with minimal OS chrome; prevent unintended sleeps during the day.
  - Provide a quick manual “Good night” action and a simple way to dismiss Night Mode live view.

---

## 8. Visual Mockups (Conceptual)

Passive Mode:

```
----------------------------------------------------
|   [Beautiful landscape photo - full screen]      |
|   [Bottom Overlay:                               |
|    📅 Fri, Oct 18   ☀ 14°C                        |
|    🕓  Upcoming: Birthday - Chris, 15:00 Dentist ]|
----------------------------------------------------
```

Interactive Modes:

```
[ Mode switcher: 📅 Calendar | 📸 Photos | 🏠 Home ]

📅 Calendar:
[Timeline of week] [+ Add Appointment]
[Color tags: Blue=Personal, Red=Family]

📸 Photos:
[Photo Viewer] [⭐  ❤️  📍]
[Swipe to browse | “Where was this taken?”]

🏠 Home Control:
[Rooms Grid][Scenes Row][Favorites]
[Large toggles | Dimmer sliders | Status chips]
```

---

## 9. Future Enhancements

- Voice commands (e.g., “Show tomorrow’s photos”, “Add appointment for Friday”).
- Personalization by detected person (e.g., show their calendar) where privacy-appropriate.
- Expanded home information (weather, lights, household to-do list).
- Daily summary email with photo and agenda.
- Optional AI photo tagging to improve discovery.
