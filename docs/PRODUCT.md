# Hearth — Product Vision

## The Core Idea

Most home dashboards are designed like control panels — dense, utilitarian, always demanding attention. Hearth is the opposite. It's designed to _recede_. Like a fireplace, it's always on, always present, but it earns its place in the room by being beautiful and calm rather than noisy and cluttered.

**The pitch:** A living picture frame that knows your home.

---

## Technical Constraints

- **Language:** C#/.NET
- **Architecture:** Microservice-oriented — each concern (weather, art, plants, tasks, display) is its own service
- **Hardware:** Split into two devices with independent power budgets — a **host** (anywhere on the LAN, runs the full Docker Compose stack) and a **frame client** (a Raspberry Pi Zero 2 W mounted behind a Pimoroni Inky Impression 13.3" color e-paper display inside a physical frame, running inkypull, a small standalone Python client on Pimoroni's `inky` library, that only fetches a pre-rendered bitmap, pushes it to the panel, and relays button presses). See `HARDWARE.md` for the parts and the reasoning behind the split
- **Web access:** Accessible via browser on desktop, tablet, and phone — but the e-paper frame is the primary experience, not a secondary one
- **Local-first:** Core features require no cloud accounts. All state is stored on the host. External APIs (weather, Spotify, calendar) are optional enhancements.

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

The tradeoff is refresh rate — a full color e-paper refresh takes tens of seconds and flickers while it runs. Hearth embraces this constraint rather than fighting it. Updates are **intentional and scheduled**, not continuous. The display isn't a live feed; it's a thoughtful snapshot.

### Color vs. Black & White Mode

The display supports two rendering modes, switchable on demand or by schedule:

**Color Mode** — uses the full Spectra 6 palette of the e-paper display. Rich art, color-coded weather, visual plant indicators. Visually warm and expressive.

**Black & White Mode** — renders in pure B&W. Crisper, lower visual noise. On Spectra 6 this is an aesthetic choice, not a speed one — the panel runs the same full refresh either way. Better for focused contexts (working from home, nighttime, minimal preference). Has an intentional "printed broadsheet" aesthetic that works well with certain art styles and typography.

Switching modes is a first-class feature, not a setting buried in config. The right mode depends on the room, the time of day, and the person.

### Ambient Mode (default)

The display shows a full-frame image — a curated photo, your own image, or a generated piece — with a minimal overlay anchored to one corner:

- Current temperature + weather icon
- Date, and when the frame was last drawn (small, unobtrusive — on e-paper it's an "Updated" time rather than a clock, since the panel only redraws when the picture changes)
- A subtle plant indicator if anything is overdue for watering (a single wilting icon, not a list)

Nothing else. The art is the display. Information is peripheral.

### Daily Mode (scheduled or on-demand)

The display shifts to a structured layout with distinct zones:

- **Top (fixed):** a compact masthead — a date block; a small edition line (issue number, season, days to the next solstice/equinox) over the day of week; and a sun path showing today's daylight with the current temperature
- **Two columns (configurable):** up to three widgets each, chosen from Today's Arc, Today (tasks), Coming Up (next events), Week Ahead, Countdowns, Almanac, Moon Phase, Daily Quote and Birds Nearby. Default: Countdowns and Today on the left, Coming Up on the right (the masthead's sun path covers what Today's Arc showed)
- **Bottom (fixed):** 5-day weather forecast — an icon and a low–high range bar per day, on one shared scale

Only widgets that stay correct between scheduled refreshes belong on the frame — no live departures, no now-playing (it changes every song and would usually be wrong).

Typography is large enough to read from across the room (3–4 meters). No small print. A text size slider in Settings → Frame makes it larger still (up to 150%) to fill a big panel or read from further away; lists show fewer items to make room. The column layout is configured in Settings → Frame the same way as the dashboard — visibility toggles plus drag-and-drop between columns with an adjustable split — and each widget trims its list to fit the space it has.

### Photo, Countdown and Night Modes

- **Photo** — one photo you choose, or a small album of your uploads (one per refresh), full-bleed with the full Ambient card, just the caption, or nothing at all. Any upload can be pinned straight from the photo collection with **Show on frame**: it stays up until you switch it off (no expiry, and the schedule never replaces it); pinning another photo moves the pin, and deleting the pinned photo clears it.
- **Countdown** — one countdown as a poster: the number of days, huge, then the title and date. You pick the countdown, or it shows the next one coming up.
- **Night** — a quiet page for overnight: tonight's moon, the next sunrise and the temperature.
- **Note** — your own words: house rules for a party, a welcome, a reminder. A heading and up to 12 lines, kept as typed (a blank line adds space). The text sizes itself to the frame: a few words fill it, a long list shrinks to fit, and each line stays on one line unless that would make the text much smaller. **Show this note** in Settings → Frame puts it up until you change it.

### Choosing the mode

What the frame shows is decided in one place, shared by every device:

1. **Your choice** — set in Settings → Frame for an hour, until midnight, or until you change it; a pinned photo; or Button B on the frame, which steps to the next mode until the schedule's next change.
2. **The schedule** — time-of-day rules on chosen days (e.g. Daily 7–9am on weekdays, Night from 11pm to 6am). The first matching rule wins.
3. **The default mode** — Ambient unless you change it.

Ideas for later modes: **Guest** (a Wi-Fi QR code and house notes), a full **Week/Month** calendar, **Gallery** art (public-domain paintings with a museum placard), and **Occasion** cards on birthdays and holidays.

---

## UX Flows

### First Run

1. User powers on the host and the frame. Hearth starts automatically.
2. The frame shows a welcome screen (drawn locally by the frame client, since nothing is rendered yet) with the local network URL (e.g. `hearth.local`).
3. On their phone or laptop, the user visits the URL and is walked through a short setup: give the household a name, pick an art source (Unsplash / local folder), set their home location and time zone (Settings → Home: this device's location, a city search, or typed coordinates — used for weather, birds, daylight and the frame's clock).
4. The display refreshes with a real layout. Setup is done.

Optional integrations (Spotify, Google Calendar, etc.) are surfaced in a separate "Connections" screen — not during first run. The frame should look good before any accounts are connected.

### Day-to-Day Phone Interaction

The web UI (phone browser, no app install) has four screens:

1. **Home** — live preview of what's on the frame, button to trigger an immediate refresh, mode toggle (Color / B&W, and the frame mode)
2. **Tasks** — add/complete household tasks and grocery list items; changes trigger a display refresh
3. **Plants** — see plant list, mark as watered, add new plants
4. **Settings** — refresh schedule, art source, integrations, display mode schedule

The `/settings` route is implemented and currently houses the theme picker, the household's home location and time zone (shared by every device and service; `.env` values remain a fallback), the font theme picker (typography presets: typeface, weight, and size), ambient mode configuration (photo source — Unsplash, local uploads, or both mixed; day and night cycling cadence; a motion toggle for slow pan/zoom, off for e-ink; categories including a seasonal one; attribution toggle; and local photo captions), widget visibility and layout, integration connections (Spotify, Google Calendar), subscribed train/transit stops with per-line filtering, the e-paper frame's mode (what it shows now, a manual choice with an expiry, and the time-of-day schedule), Photo mode's album and Countdown mode's countdown, how often Ambient and Photo change photo on the panel, and its appearance (an ink-safe colour theme, typeface, text size, orientation and which way a portrait frame is turned, panel size) with a live preview. Additional settings will move here as features are built out.

All screens are usable one-handed on a phone. No dense forms.

The frame's four buttons cover the quick actions without a phone: refresh now, Ambient ↔ Daily, and Color ↔ B&W (see `HARDWARE.md`).

### Display Mode Transitions

When a mode change is triggered (by schedule or phone):

1. The Display Service fetches fresh data from all relevant services
2. It renders the new layout to a bitmap
3. It pushes the frame to the e-paper hardware (full refresh — flicker is expected and acceptable)
4. The previous image is gone; the new one persists until the next scheduled refresh

There are no animations or partial-refresh transitions between Ambient and Daily. The flicker is the transition.

### Art Rotation

- In Ambient Mode, art rotates on a configurable interval (default: every 30 minutes, aligned with the refresh schedule)
- Each image is fetched, cropped to the panel's 4:3 aspect, resized to 1600×1200, and dithered before being cached
- Favor images that survive a 6-color dither: high contrast, saturated, simple compositions. Smooth gradients, low light, and skin tones break up into visible dither noise
- Because each change is a full refresh, rotation shouldn't go faster than roughly every 15–30 minutes
- In Daily Mode, no art is shown — the layout is full-frame structured content

---

## Feature Set

### Phase 1 — The Frame

- Art display: Unsplash integration, your own photos, or a local image folder — dithered and optimized for e-paper rendering
- Weather: current conditions + today's forecast + 5-day (Open-Meteo or similar free API, no key required); includes sunrise and sunset times for the current day
- Date in every mode; a live clock in the browser, an "Updated" time on the e-paper frame (which only redraws when its picture changes)
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

- Music: show what's playing on Spotify (track, artist) on the web dashboard — deliberately not on the frame, which refreshes too rarely to keep it current
- Household calendar: pull from Google/Apple Calendar, show next 2–3 events
- Birds nearby: recent bird sightings around the home from eBird — the frame quietly reflects the living world outside the window; rare sightings gently highlighted
- Nearby transit: upcoming departures for user-subscribed bus/train stops via Transitland, with per-line filtering so only the routes that matter to the household show up
- Presence-aware refresh: an optional sensor in the frame lets Hearth refresh when nobody's watching (see Open Questions)
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
5. **It's self-hosted but not hard.** One command to start. Runs entirely on a small home server, with a Pi Zero 2 W in the frame. No subscriptions, no cloud lock-in.
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
| Raspberry Pi friendly | Yes   | Partial     | Yes            | No        |
| Non-technical setup  | Goal   | No          | No             | Yes       |
| Free                 | Yes    | Yes         | Yes            | Freemium  |

Hearth's gap: **self-hosted + e-paper-native + beautiful + accessible to non-developers**.

---

## Open Questions

These need to be decided before or early in development:

- **Art in Daily Mode:** Current spec says no art in Daily Mode — the layout is full-frame structured content. Is this correct, or should there be a small art pane?
- **Presence-aware refresh:** A full refresh flickers for tens of seconds, so the best time to refresh is when nobody's looking. Leading idea: while the room is empty, rotate Ambient photos freely; when someone is present, hold the current image (or switch to Daily once, with a minimum dwell time) and suppress further refreshes; with the lights off, skip refreshes entirely. Needs an optional sensor in the frame (mmWave radar preferred — it detects people sitting still and needs no hole) reporting to the Display service, which owns the policy. See `HARDWARE.md`.

### Resolved

- **E-paper hardware module:** Pimoroni Inky Impression 13.3" — E Ink Spectra 6, 6-color, 1600×1200 — driven by a Raspberry Pi Zero 2 W (a Pi 4 Model B for now) running inkypull, our own standalone Python client on Pimoroni's `inky` library, rather than a C# library. See `HARDWARE.md`.
- **Physical input:** The Inky Impression's four built-in buttons, in v1 — refresh, Ambient ↔ Daily, Color ↔ B&W, plus one reserved. The phone remains the primary remote; a "frame tap" stays a v2 idea. See `HARDWARE.md`.

---

## The Name

_Hearth_ is right. A hearth is the center of a home — warm, always present, functional but also symbolic. It doesn't demand attention. It rewards it.
