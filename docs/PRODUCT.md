# Hearth — Product Vision

## The Core Idea

Most home dashboards are designed like control panels — dense, utilitarian, always demanding attention. Hearth is the opposite. It's designed to *recede*. Like a fireplace, it's always on, always present, but it earns its place in the room by being beautiful and calm rather than noisy and cluttered.

**The pitch:** A living picture frame that knows your home.

---

## Technical Constraints

- **Language:** C#/.NET
- **Architecture:** Microservice-oriented — each concern (weather, art, plants, tasks, display) is its own service
- **Hardware:** Raspberry Pi 5+ connected to an 11–13" color e-paper display, housed inside a physical frame
- **Web access:** Accessible via browser on desktop, tablet, and phone — but the e-paper frame is the primary experience, not a secondary one
- **Local-first:** Core features require no cloud accounts. All state is stored on the Pi. External APIs (weather, Spotify, calendar) are optional enhancements.

---

## Who It's For

The target user is comfortable with a Raspberry Pi but doesn't want to spend a weekend in config files. They care deeply about their home looking good. They've probably tried MagicMirror or a Home Assistant dashboard, been impressed by the capability, but frustrated that it looks like a developer made it for themselves.

Specifically:
- People who want a wall-mounted home display that looks like art, not a gadget
- Remote workers who want ambient awareness of their day without opening a laptop
- Families who want a shared "household brain" — one place to see everyone's schedules, chores, grocery list

---

## The Display Philosophy

E-paper is the right medium for Hearth. Unlike LCD or OLED, e-paper emits no light — it reflects ambient light like actual paper or a printed photograph. This means:

- No glow in a dark room. It doesn't disturb sleep or ruin the mood of a space.
- The image persists indefinitely with zero power draw. Hearth can show your household's state all day at essentially no cost.
- It genuinely looks like something hanging on your wall, not a screen.

The tradeoff is refresh rate — full color e-paper refreshes can take 15–30 seconds and cause a brief flicker. Hearth embraces this constraint rather than fighting it. Updates are **intentional and scheduled**, not continuous. The display isn't a live feed; it's a thoughtful snapshot.

### Color vs. Black & White Mode

The display supports two rendering modes, switchable on demand or by schedule:

**Color Mode** — uses the full palette of the e-paper display. Rich art, color-coded weather, visual plant indicators. Slower to refresh (~15–30s), but visually warm and expressive.

**Black & White Mode** — renders in pure B&W. Crisper, faster refresh (~2–5s), lower visual noise. Better for focused contexts (working from home, nighttime, minimal preference). Has an intentional "printed broadsheet" aesthetic that works well with certain art styles and typography.

Switching modes is a first-class feature, not a setting buried in config. The right mode depends on the room, the time of day, and the person.

### Ambient Mode (default)

The display shows a full-frame image — a curated photo, your own image, or a generated piece — with a minimal overlay anchored to one corner:

- Current temperature + weather icon
- Date and time (small, unobtrusive)
- A subtle plant indicator if anything is overdue for watering (a single wilting icon, not a list)

Nothing else. The art is the display. Information is peripheral.

### Daily Mode (scheduled or on-demand)

The display shifts to a structured layout with distinct zones:

- **Top:** Date, day of week, current weather + high/low
- **Left column:** Today's tasks and grocery list items
- **Right column:** Next 2–3 calendar events; now-playing if music is active
- **Bottom:** 5-day weather forecast as a simple icon row

Typography is large enough to read from across the room (3–4 meters). No small print. Layout is fixed — not configurable per-user in v1.

The mode schedule is configurable (e.g. Ambient all day, Daily at 7am and 6pm), but Ambient is the default.

---

## UX Flows

### First Run

1. User powers on the Pi. Hearth starts automatically.
2. The display shows a welcome screen with the local network URL (e.g. `hearth.local`).
3. On their phone or laptop, the user visits the URL and is walked through a short setup: give the household a name, pick an art source (Unsplash / local folder), set their location for weather.
4. The display refreshes with a real layout. Setup is done.

Optional integrations (Spotify, Google Calendar, etc.) are surfaced in a separate "Connections" screen — not during first run. The frame should look good before any accounts are connected.

### Day-to-Day Phone Interaction

The web UI (phone browser, no app install) has four screens:

1. **Home** — live preview of what's on the frame, button to trigger an immediate refresh, mode toggle (Color / B&W, Ambient / Daily)
2. **Tasks** — add/complete household tasks and grocery list items; changes trigger a display refresh
3. **Plants** — see plant list, mark as watered, add new plants
4. **Settings** — refresh schedule, art source, integrations, display mode schedule

All screens are usable one-handed on a phone. No dense forms.

### Display Mode Transitions

When a mode change is triggered (by schedule or phone):
1. The Display Service fetches fresh data from all relevant services
2. It renders the new layout to a bitmap
3. It pushes the frame to the e-paper hardware (full refresh — flicker is expected and acceptable)
4. The previous image is gone; the new one persists until the next scheduled refresh

There are no animations or partial-refresh transitions between Ambient and Daily. The flicker is the transition.

### Art Rotation

- In Ambient Mode, art rotates on a configurable interval (default: every 30 minutes, aligned with the refresh schedule)
- Each image is fetched, resized to the display resolution, and dithered by the Art Service before being cached
- In Daily Mode, no art is shown — the layout is full-frame structured content

---

## Feature Set

### Phase 1 — The Frame
- Art display: Unsplash integration, your own photos, or a local image folder — dithered and optimized for e-paper rendering
- Weather: current conditions + today's forecast + 5-day (Open-Meteo or similar free API, no key required)
- Clock and date, always present in both modes
- Color/B&W mode toggle: switchable on demand or by schedule
- Scheduled display refresh: configurable cadence, full refresh only in v1
- First-run setup flow via local web UI
- Local-network web UI with Home, Tasks, Plants, Settings screens

### Phase 2 — The Home
- Plant tracker: name your plants, set watering intervals, visual wilt indicator in Ambient overlay, full list in Daily mode
- Daily tasks: shared household to-do list, completable from the web UI
- Grocery list: add from phone, shown in Daily mode alongside tasks

### Phase 3 — The Pulse
- Music: show what's playing on Spotify (track, artist) in Daily mode
- Household calendar: pull from Google/Apple Calendar, show next 2–3 events
- Gentle notifications: surface urgent items (weather alert, plant critically overdue) as a temporary overlay before reverting to Ambient

### Phase 4 — The Soul
- Per-room profiles: different frames in different rooms with different mode schedules and widget sets
- Plugin/widget system: community-extensible; each widget maps to a microservice that implements a standard interface

---

## Architecture Overview

Each capability is a self-contained microservice running on the Pi. Services expose REST APIs consumed by:
1. The **Display Service** — which owns the render pipeline and writes frames to the e-paper hardware
2. The **Web/API Gateway** — which serves the phone UI and routes requests to services

### Service Decomposition

| Service | Responsibility |
|---|---|
| Display Service | Renders layouts to bitmap, drives e-paper hardware, manages refresh schedule |
| Art Service | Fetches, caches, and dithers images for e-paper output |
| Weather Service | Polls weather API on a schedule, caches current + forecast |
| Tasks Service | CRUD for household to-do and grocery lists |
| Plants Service | Tracks plant watering schedules and due dates |
| Music Service | Polls Spotify API for now-playing |
| Calendar Service | Syncs with Google/Apple Calendar |
| Web/API Gateway | Serves the phone UI, routes requests to services, handles first-run setup |

### Design Principles

- Services communicate over local HTTP on fixed, well-known ports. No service discovery in v1.
- State is persisted per-service using SQLite. No shared database.
- If a service is unavailable at render time, the Display Service renders gracefully without that data — it does not fail or wait. Stale data from the last successful fetch is used where possible.
- No external cloud infrastructure required for core operation. External API calls are made by individual services on their own polling schedules.

---

## What Makes It Stick

1. **It's beautiful out of the box.** First run should look good in 60 seconds, not require configuration.
2. **E-paper is the right call.** No glowing screen. No disturbing the room. Looks like something that belongs on a wall.
3. **Color and B&W are both first-class.** Switch the whole aesthetic of the frame based on mood, time of day, or preference — without changing any content.
4. **The phone is the remote.** Any family member can add a task, update the grocery list, or trigger a refresh from their phone — no app install, just the local web UI.
5. **It's self-hosted but not hard.** One command to start. Runs entirely on the Pi 5. No subscriptions, no cloud lock-in.
6. **Updates feel considered, not reactive.** The scheduled refresh cadence makes Hearth feel like a thoughtful daily artifact rather than a noisy feed.

---

## Competitive Landscape

|                        | Hearth | MagicMirror | Home Assistant | DAKboard |
|------------------------|--------|-------------|----------------|----------|
| Beautiful by default   | Yes    | No          | No             | Partially|
| E-paper native         | Yes    | No          | No             | No       |
| Color + B&W modes      | Yes    | No          | No             | No       |
| Self-hosted            | Yes    | Yes         | Yes            | No       |
| Ambient/art-first      | Yes    | No          | No             | No       |
| Pi 5 optimized         | Yes    | Partial     | Yes            | No       |
| Non-technical setup    | Goal   | No          | No             | Yes      |
| Free                   | Yes    | Yes         | Yes            | Freemium |

Hearth's gap: **self-hosted + e-paper-native + beautiful + accessible to non-developers**.

---

## Open Questions

These need to be decided before or early in development:

- **E-paper hardware module:** Which specific display and driver board? This determines the SPI interface, color palette (4-color vs. 7-color ACeP), resolution, and which C# library or native bindings to use.
- **Physical input:** Does the frame have any physical controls (button, tap sensor, PIR motion sensor)? The "frame tap" concept mentioned in Phase 2 tasks requires this to be defined.
- **Spotify OAuth on a local device:** Spotify's auth flow requires a redirect URI. How does this work when the device has no public URL? Options: local loopback auth flow, or a companion setup page that captures the token.
- **Unsplash attribution:** Unsplash's API requires attribution. How is this displayed on an e-paper frame without cluttering the art? Small text in a corner? Only shown in Daily mode?
- **Art in Daily Mode:** Current spec says no art in Daily Mode — the layout is full-frame structured content. Is this correct, or should there be a small art pane?

---

## The Name

*Hearth* is right. A hearth is the center of a home — warm, always present, functional but also symbolic. It doesn't demand attention. It rewards it.
