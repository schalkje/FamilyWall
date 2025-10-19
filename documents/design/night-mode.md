# Night Mode – User Requirements

This document details the user-facing requirements for Night Mode. Technical design lives in `architecture.md`.

## 1. Objective
At configured bedtime, the display turns off (screen black). When motion is detected, the screen wakes to a live camera view and returns to black after inactivity.

## 2. Entering and Exiting Night Mode
- Enter via any of:
  - Schedule: configurable per weekday (e.g., 22:30–06:30)
  - Manual: quick action (moon icon) or voice phrase when voice is enabled
  - Ambient: optional low-light threshold
- Exit via any of:
  - Scheduled wake time
  - Manual exit (tap & hold or voice)
  - Morning ambient-light threshold (optional)

## 3. Behavior
- Screen is off while in Night Mode (no bright UI)
- On motion:
  - Instantly wake screen and show a full-screen live camera view
  - Show subtle overlays: timestamp and a small "LIVE" badge
  - After the last detected motion, return to black after a configurable timeout (default 20–30 s)
- Provide a one-tap Dismiss to hide the live view immediately; long-press exits Night Mode entirely

## 4. Privacy & Safety
- Processing is local by default; no frames uploaded without explicit opt-in
- Recording is on by default; if enabled:
  - Use a rolling buffer (e.g., 10–30 s)
  - Let users choose retention days and storage location
- Show a clear visual indicator when the live camera is on-screen

## 5. Configuration
- Bedtime schedule per weekday, with holiday overrides
- Motion sensitivity threshold and cooldown
- No-motion timeout before returning to black
- Motion source selection (camera, PIR, or both)
- If recording is enabled: retention days and save location

## 6. Accessibility & UX
- Low-light safe brightness for the live view
- Smooth, non-jarring transitions
- Small corner text "Night Mode" when briefly tapped to remind current state

## 7. Failure Behavior
- If live camera cannot be shown, wake to a minimal clock view instead
- The system should continue to recognize motion for wake behavior even if one sensor fails
