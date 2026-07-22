# Hearth — Product Vision

## The Core Idea

Most home dashboards are designed like control panels — dense, utilitarian, always demanding attention. Hearth is the opposite. It's designed to _recede_. Like a fireplace, it's always on, always present, but it earns its place in the room by being beautiful and calm rather than noisy and cluttered.

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

The `/settings` route is implemented and currently houses the theme picker, the font theme picker (typography presets: typeface, weight, and size), and ambient mode configuration (photo source — Unsplash, local uploads, or both mixed; day and night cycling cadence; a motion toggle for slow pan/zoom, off for e-ink; categories including a seasonal one; attribution toggle; and local photo captions). Additional settings will move here as features are built out.

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
- Weather: current conditions + today's forecast + 5-day (Open-Meteo or similar free API, no key required); includes sunrise and sunset times for the current day
- Clock and date, always present in both modes
- Countdown events: one-off events tracked by days remaining; five nearest upcoming events shown in a dedicated widget
- Moon phase: front-end computed lunar phase with phase name, illumination %, SVG visualization, and countdown to the next major phase
- Today's arc: an ambient ribbon showing the shape of the day — night, daylight, golden hours, solar noon — with today's events and timed tasks as quiet marks and a dot for "now"
- News feeds: user-configured RSS/Atom feeds with a configurable article count
- Almanac: where we are in the year — season progress with equinox/solstice countdown pinned, plus two rotating one-line facts chosen by timeliness: daylight trend with the next milestone ("Last 8 pm sunset · Aug 13"), first/last frost countdown (only when ≤6 weeks out), or a curated seasonal note; computed entirely locally, no external API
- Color/B&W mode toggle: switchable on demand or by schedule
- Scheduled display refresh: configurable cadence, full refresh only in v1
- First-run setup flow via local web UI
- Local-network web UI with Home, Tasks, Plants, Settings screens

### Phase 2 — The Home

- Plant tracker: name your plants, set watering intervals, visual wilt indicator in Ambient overlay, full list in Daily mode
- Daily tasks: shared household to-do list, completable from the web UI; recurring tasks (daily through yearly)
- Grocery list: add from phone, shown in Daily mode alongside tasks

### Phase 3 — The Pulse

- Music: show what's playing on Spotify (track, artist) in Daily mode
- Household calendar: pull from Google/Apple Calendar, show next 2–3 events
- Birds nearby: recent bird sightings around the home from eBird — the frame quietly reflects the living world outside the window; rare sightings gently highlighted
- Gentle notifications: surface urgent items (weather alert, plant critically overdue) as a temporary overlay before reverting to Ambient

### Phase 4 — The Soul

- Per-room profiles: different frames in different rooms with different mode schedules and widget sets
- Plugin/widget system: community-extensible; each widget maps to a microservice that implements a standard interface

---

## What Makes It Stick

1. **It's beautiful out of the box.** First run should look good in 60 seconds, not require configuration.
2. **E-paper is the right call.** No glowing screen. No disturbing the room. Looks like something that belongs on a wall.
3. **Color and B&W are both first-class.** Switch the whole aesthetic of the frame based on mood, time of day, or preference — without changing any content.
4. **The phone is the remote.** Any family member can add a task, update the grocery list, or trigger a refresh from their phone — no app install, just the local web UI.
5. **It's self-hosted but not hard.** One command to start. Runs entirely on the Pi 5. No subscriptions, no cloud lock-in. While Hearth is built around one household's Pi 5, published container images (see [`docs/SELF-HOSTING.md`](SELF-HOSTING.md)) let other households run their own instance too — each self-hoster supplies their own location and third-party API credentials.
6. **Updates feel considered, not reactive.** The scheduled refresh cadence makes Hearth feel like a thoughtful daily artifact rather than a noisy feed.

---

## Competitive Landscape

|                      | Hearth | MagicMirror | Home Assistant | DAKboard  |
| -------------------- | ------ | ----------- | -------------- | --------- |
| Beautiful by default | Yes    | No          | No             | Partially |
| E-paper native       | Yes    | No          | No             | No        |
| Color + B&W modes    | Yes    | No          | No             | No        |
| Self-hosted          | Yes    | Yes         | Yes            | No        |
| Ambient/art-first    | Yes    | No          | No             | No        |
| Pi 5 optimized       | Yes    | Partial     | Yes            | No        |
| Non-technical setup  | Goal   | No          | No             | Yes       |
| Free                 | Yes    | Yes         | Yes            | Freemium  |

Hearth's gap: **self-hosted + e-paper-native + beautiful + accessible to non-developers**.

---

## Open Questions

These need to be decided before or early in development:

- **E-paper hardware module:** Which specific display and driver board? This determines the SPI interface, color palette (4-color vs. 7-color ACeP), resolution, and which C# library or native bindings to use.
- **Physical input:** Does the frame have any physical controls (button, tap sensor, PIR motion sensor)? The "frame tap" concept mentioned in Phase 2 tasks requires this to be defined.
- **Art in Daily Mode:** Current spec says no art in Daily Mode — the layout is full-frame structured content. Is this correct, or should there be a small art pane?

---

## The Name

_Hearth_ is right. A hearth is the center of a home — warm, always present, functional but also symbolic. It doesn't demand attention. It rewards it.
