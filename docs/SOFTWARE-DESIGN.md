# Software Design

## Overview

Hearth is a calm, self-hosted home dashboard. The architecture is a set of small, independently deployable services behind a single reverse proxy, consumed by a SvelteKit frontend.

## Architecture Overview

```
Browser
  └── Caddy :80 + :443
        ├── /tasks*    → Tasks      :8081
        ├── /weather*  → Weather    :8082
        ├── /spotify*  → Spotify    :8083
        ├── /photos*   → Photos     :8084
        ├── /rss*      → Rss        :8085
        ├── /quote*    → Quote      :8086
        ├── /calendar* → Calendar   :8087
        ├── /birds*    → Birds      :8088
        ├── /almanac*  → Almanac    :8089
        ├── /display*  → Display    :8090
        ├── /trains*   → Trains     :8091
        ├── /sports*   → Sports     :8094
        ├── /logs*     → Dozzle     :8080  (container logs, opt-in, own login)
        └── /*         → Frontend   :3000
```

Every service is an independently deployable **ASP.NET Core 10 Minimal API** process with its own SQLite database — except Almanac, which is stateless.

## Frontend

### SvelteKit 2 + Svelte 5 + Tailwind CSS v4 + Lucide Svelte

- Lightweight runtime, well-suited for an always-on ambient display
- Svelte 5 runes syntax (`$state`, `$effect`, `$props`, `$derived`) for fine-grained reactivity
- Tailwind v4 for utility-first styling with a muted, theme-switchable palette
- [Lucide Svelte](https://lucide.dev/guide/packages/lucide-svelte) for icons (`@lucide/svelte`)
- API calls are centralised in `apps/frontend/src/lib/api.ts`; all paths are relative (e.g. `/tasks`)
- In development, Vite proxies each API path (`/tasks`, `/weather`, `/spotify`, `/photos`, `/rss`, `/quote`, `/calendar`, `/birds`, `/almanac`, `/trains`, `/sports`) to its service's `$<NAME>_URL` env var; in production, Caddy handles the same routing

### Shared UI components

Form controls and other repeated pieces live in `apps/frontend/src/lib/components/ui/` — use them rather than restyling native elements per screen:

| Component | Use |
| --- | --- |
| `Button` (+ `buttonClass()` in `button.ts`) | `primary` (accent fill, one per group), `secondary` (outlined pill, default), `link`, `ghost`; renders an `<a>` when given `href`; defaults to `type="button"` |
| `IconButton` | Icon-only button with a required `label`; `reveal` hides it until its `group` row is hovered (always shown on touch) |
| `TextInput`, `TextArea`, `Select` (+ `fieldClass()` in `field.ts`) | The one bordered field style, `sm`/`md`; a raw element with `fieldClass()` when a handler rewrites `el.value` in place |
| `Field` | Small-caps label above a control |
| `Chip`, `ChoiceChips` | Pick-one chips — accent fill while chosen, `aria-pressed` |
| `SegmentedControl` | Two to four short options as one control |
| `DayPicker` | Any-number-of days of the week |
| `Toggle`, `ToggleRow` | A switch; the row names and describes it by its title and description, and supports `disabled` |
| `PickerCard` | Large option card in a preview grid (typeface, frame theme) |
| `Disclosure` | A native `<details>` fold: a summary row (usually the current choice) that opens to the full options — the theme, type and frame-colour pickers and the frame's set-once panel size |
| `CardColumns` | Two independent stacks of settings cards on wide screens (left above right when narrow) — use it rather than a two-column grid, whose rows leave a gap beside a short card |
| `Modal` | `<dialog class="sheet">` with a titled header, close button and scrolling body mounted only while open |
| `SizeSlider` | A text-size range between a small and large "A", showing the size as a percent (the dashboard's size and the frame's text size) |
| `ListRow`, `ConnectionRow` | A settings list row with remove/extra actions; an outside service with its status and buttons |
| `FileUploadButton`, `InlineMessage` | A file picker styled as a secondary button; an error (`--danger`, `role="alert"`) or muted note |

Outside `ui/`, `TaskCheckbox` is the done checkbox for tasks and calendar tasks (`box` or `dot`, `role="checkbox"`), and `PhotoUploadButton` validates (`checkPhotoFiles` in `lib/photos.ts`) and uploads photos for both the Ambient settings and the photo collection. Small-caps group headings use the `.eyebrow` class from `app.css`.

### Routes

| Route       | Description                                                                               |
| ----------- | ----------------------------------------------------------------------------------------- |
| `/`         | Dashboard — configurable widget columns (tasks, weather, countdowns, moon phase, news, quote) |
| `/calendar` | Calendar view with month grid and per-day task overflow modal                             |
| `/ambient`  | Fullscreen photo slideshow; click or any keypress returns to `/`                          |
| `/settings` | Sections (`?section=`): theme and type pickers, kiosk mode, home location and time zone, ambient photo cadence, categories, widget layout and visibility, service health and a link to the logs (Diagnostics) — a sidebar on wide screens (hidden with ⌘B / Ctrl+B), a drill-in list on phones |
| `/frame`    | E-paper render target — panel-sized canvas (default 1600×1200), no nav (see below)        |

The dashboard, nav, and footer all switch between mobile and desktop layouts at one breakpoint: Tailwind's `lg` (1024px). Below it the widget columns stack into one (left column first, then right), the bottom tab bar replaces the masthead, the kiosk footer is hidden (Now Playing moves to the top of the dashboard), a one-line date and current conditions open the dashboard outside kiosk mode, and `ScrollFadeList` lists (news, birds, trains) clip with a Show more/less toggle instead of scrolling inside the page. Settings opens on a section list there, with a back link from each section, in place of the sidebar. On wide screens the sidebar lists the active section's cards under it (`SettingsSubnav.svelte`) and highlights the one in view; it reads them off the page — every titled card rendered through the page's `card` snippet is listed — so a new card needs no nav code, and a new section is one entry in `constants/settingsSections.ts` plus its `{:else if}` block. The calendar switches earlier, at `md` (768px): below it day cells show marks instead of titles (squares for tasks, circles for events), a horizontal swipe on the grid changes month, and tapping a day lists its items under the grid in `DayItemList.svelte` — the same rows the desktop `DayOverflowModal.svelte` shows — with an Add task button in place of the header's New Task. Below `sm` (640px) every modal (`ui/Modal.svelte`, a `<dialog class="sheet">`) becomes a bottom sheet (`app.css`), and the agenda puts each time above its title.

#### Frame route

`/frame` is the page the Display service's renderer screenshots and dithers for the Inky panel (`HARDWARE.md` step 1). It is sized to the panel — the shared panel size (default 1600×1200, the Inky Impression 13.3"), with width and height swapped in portrait — and never scrolls, renders outside the app chrome (`+layout.svelte` skips `Nav` for `/frame*`), and is client-rendered (`ssr = false`). Its look and layout come from the shared frame settings in the Display service (see "Display Service"), so every client — and the panel's headless renderer — draws the same frame; URL params override them per request, and anything neither sets uses the defaults below:

| Param        | Values                 | Default    |
| ------------ | ---------------------- | ---------- |
| `mode`       | `ambient`, `daily`, `photo`, `countdown`, `night`, `note` | what `GET /display/mode` resolves now, else `ambient` |
| `palette`    | `color`, `bw`          | `color`    |
| `theme`      | any frame theme id (below) | `almanac` |
| `font`       | any font preset id     | `inter`    |
| `text`       | text size %, 100–150 (reading type and icons) | `100`  |
| `left`, `right` | comma-separated frame widget ids (Daily) | `countdowns,today` / `coming-up` |
| `split`      | left column width %, 30–70 | `50`    |
| `template`   | Daily layout template: `columns`, `full`, `halves`, `feature`, `banner`, `quad` | `columns` |
| `slots`      | comma-separated frame widget ids, one per template slot in order, empty for an empty slot (`week,,quote`) | the saved slots |
| `orientation` | `landscape`, `portrait` | `landscape` |
| `width`, `height` | the panel's native size in px, 200–4000, width ≥ height (portrait swaps them) | the shared panel size (`1600` × `1200`) |
| `position`   | Ambient card: `top-left`, `top-middle`, `top-right`, `bottom-left`, `bottom-middle`, `bottom-right` | `bottom-left` |
| `photo`      | Ambient/Photo's photo as JSON (`parsePhotoParam`: same-origin or Unsplash CDN URLs only) | the photo Display holds (`GET /display/photo`) |

- **Ambient** (`FrameAmbient.svelte`): full-bleed photo with a solid plate — a compact date block, the temperature, one sentence of weather and an "Updated h:mm" stamp (the render's time; the panel holds a frame for up to the photo interval, so it shows no clock) — inset 64px at one of six positions (a corner, or the middle of the top or bottom edge). The position is the frame root's `data-position`, so the Settings preview moves the plate in place without reloading the photo. The date block keeps the theme's `--block` ink unless the photo just around the plate is close to it; then it takes whichever of the theme's other pure inks stands out most there. The route draws the photo cover-cropped into a 1/8-scale canvas once (CORS-mode load), and `FrameAmbient` scores the theme's inks against the pixels ringing the plate in OKLab (`pickBlockInk` / `plateRect` in `lib/frame.ts`), re-picking when the preview changes `data-position` or `data-theme`. Unreadable pixels fall back to `--block`.
- **Photo** reuses `FrameAmbient`: the upload pinned with the photo collection's "Show on frame" switch (the resolved mode's `photo_id`), else a random pick from the frame settings' `photo_ids` album (chosen in `PhotoModePicker`: one photo, several, or saved photo groups), else Ambient's photo when none of them is still uploaded. Display's `FramePhotos` makes and holds that pick (see "Panel render"); `/frame` only shows it. `photo_card` keeps the full plate, only an upload's caption, or nothing.
- **Countdown** (`components/frame/modes/FrameCountdownHero.svelte`): one countdown as a poster — the day count at `.type-poster` (400px) in the display face, a `--block` rule, the title and date. `heroCountdown()` shows the frame settings' `countdown_id` while it's ahead, else the next one coming up.
- **Night** (`components/frame/modes/FrameNight.svelte`): a large moon in tonight's phase (drawn like `MoonPhaseView`, from `calcMoonPhase`/`moonPath`), its name and illumination, and one line with the next sunrise (`nextSunrise()`) and the temperature. Meant for overnight schedule rules.
- **Note** (`components/frame/modes/FrameNote.svelte`): the frame settings' `note_title` in the display face over a `--block` rule, then `note_body` one paragraph per typed line (blank lines become gaps). Sized by measuring: `fitFontSize()` in `lib/frame.ts` binary-searches the largest px size (22–320 × `--frame-scale`) at which it fits the panel, once with lines kept whole (`nowrap`) and once wrapping, and keeps the unwrapped size unless it's under 60% of the wrapped one. It refits after `document.fonts.ready` and when the preview swaps `data-font`, so sizes are inline px rather than `.type-*` classes.
- **Daily** (`FrameDaily.svelte`), laid out as today's edition of an almanac: a compact masthead (`FrameDateBlock` — the day of the month on a solid `--block` square; `FrameEditionLine` — a small-caps line with the day-of-year issue number, the season and days to its next marker, from the Almanac service — over the weekday in the display face; `FrameSunPath` — today's daylight as a low dome with the elapsed part filled, the sun as an accent disc, the temperature under the dome, sunrise and sunset beside its base and the daylight length below a hatched horizon) and a 5-day forecast band (`FrameForecast` — a conditions line with the current wind, today's peak UV and the air quality, each with its category word, over low–high range bars on one shared scale over a hatched track; peak rather than current UV because the frame redraws on a schedule), with the configurable widget area between them — a layout template's sized slots, or two free columns (`left`/`right`, up to 3 each, at `split`%) under the `columns` template (see Layout templates below). Frame widgets are registered in `constants/frameWidgets.ts`. Moon Phase, Daily Quote, Birds Nearby and Almanac are shared views in `components/widgets/views/` (`MoonPhaseView`, `DailyQuoteView`, `BirdSightingsView`, `AlmanacView`) that the dashboard widgets wrap with loading and polling and the frame renders directly with a `lines` budget; they size off `type-*`/`icon-*` tokens and em units, so the frame's larger tokens scale them, and the frame route sets `--stroke: 3px` and restyles their `.meter` bar as an outlined pure-ink bar. Today's Arc reuses `DayArcWidget`. Today, Coming Up, Week Ahead and Countdowns are frame-specific designs in `components/frame/widgets/` (the dashboard versions are interactive), as is Last Played (the latest Spotify track with its album art, when it played, and this month's top artist — the dashboard has live now-playing instead), sharing their selection logic through `lib/frame.ts` (e.g. both countdown widgets use `upcomingCountdowns`). Trains, now-playing and live scores are deliberately excluded — they go stale between scheduled refreshes; Last Played stands in for now-playing, and Last Result & Next Game (`FrameSports.svelte`) shows a game in progress as "Playing now" with no score. In portrait the free columns stay side by side at `split`% (about 500px each at an even split; Coming Up's rail moves the day and time above each title when its column is under 640px, and the countdown stamps fit fewer per row), and the masthead keeps the edition line and weekday under the date block, beside the sun path.
- **Layout templates** (`constants/frameTemplates.ts`): the widget area as a fixed arrangement of sized slots, the model of TRMNL's mashups and iOS widget families. `frameTemplates` lists each template's slot sizes — S (about a quarter of the area), M (a half) or L (all of it) — and its `grid-template-areas` per orientation: `full` (L), `halves` (M M, stacked in portrait), `feature` (M beside S over S), `banner` (M over S S) and `quad` (four S); `columns` has no slots and keeps the free columns. `frameWidgetSizes` lists the sizes each widget is designed for (a `Record` over every frame widget id, so a new widget must declare them); a widget only goes in a slot of one of its sizes (`fitsSlot`, `widgetsForSize`), so every combination is one that was designed. Every widget takes S, M and L except Week Ahead (M/L — seven day rows don't read in a quarter). Slots are saved by position with `''` for an empty one; `normalizeSlots` pads or trims to the template and empties unknown, repeated or wrong-size ids, and `moveToTemplate` carries the placed widgets to a newly picked template, pickiest (fewest sizes) first. FrameDaily lays the slots on a grid of `minmax(0,1fr)` tracks, so a slot's size comes from the template, never its content. Each slot measures its own height for a line budget (`slotLines`: the slot's lines less the kicker), and its body under the kicker is a CSS size container (`container-type: size`), so widgets size to it with `cqw`/`cqh` units and switch wide/tall arrangements with `@container (aspect-ratio …)` rules.
  - **Size variants:** a list widget keeps its design and fits more items (Today steps its type and boxes up in L); others change shape. S and the free columns use the compact designs; M and L route to slot designs in `components/frame/widgets/`: Coming Up → `FrameAgenda` (days under display-face headings, today's in `--accent`, times in a margin; `fitAgenda()` in `lib/frame.ts` starts a day only if its heading and one row fit, and a slot over 1.8× as wide as tall flows into two columns), Moon Phase → `FrameMoon` (the disc sized to the slot, the name beside it or below in a tall slot; L adds the next four major phases from `upcomingPhases()`), Daily Quote → `FrameQuote` (fitted with `FitText`), Countdowns → a poster of the soonest (its day count fitted to the slot in `--accent`, the rest beside or below — a dotted-leader list in M, stamps in L; S is one stamp with its title), Last Played's album art grows to the slot in L (none in S). Moon Phase and Daily Quote's dashboard views stay as they were; the slot designs are frame-only.
  - **`FitText`** (`components/frame/FitText.svelte`) fits its content to its box with `fitFontSize()` (as Note mode does), between a `min` and `max` in reference px scaled by `--frame-scale`, refitting on resize and after `document.fonts.ready`.
  - **`FrameEmpty`** is the shared empty state: a quiet body line in S slots and the free columns, a centred italic display-face line in M and L so an empty slot reads as designed.
- **Visual language:** one 3px line weight for rules, check boxes, leaders and icon strokes (`setLucideProps({ strokeWidth: 3, absoluteStrokeWidth: true })` in the route); widget kickers (tracked caps led by a `--block` square) instead of underlined headings; large shapes in the pure `--block` ink, which never dithers; tints only in the forecast band; and one texture, `.hatch` (vertical 3px pure-ink lines every 9px). Display numerals use `--display-family` with the frame-only `.type-date` / `.type-hero` / `.type-stamp` sizes. Today is a ledger (dotted leader to the due time), Coming Up a rail (day and time in the margin, a pin per event), Countdowns a row of stamps (the soonest filled with `--accent`).
- **Fitting the space:** In the free columns, FrameDaily measures each column's height in lines of body text (via a hidden probe, so font presets count) and `allocateLines()` in `lib/frame.ts` splits it: every widget pays for its heading and gaps, fixed-height widgets (arc, moon, the countdown stamps, Last Played's album square) take what they need, and the rest is shared evenly. Each widget trims its list to its share — and, as a last resort, clips inside its own box rather than pushing a neighbour off the panel. Selection logic (`todayTasks`, `nextEvents`, `weekAhead`, `upcomingCountdowns`) and the masthead's small derivations (`sunProgress`, `weatherSentence`, `forecastBars`, `dayOfYear`) live in `lib/frame.ts` too.
- Portrait is for a frame hung on its side; the Ambient photo is requested in the matching orientation, and the Display service rotates the capture to the panel's native landscape.
- Type and icon sizes are px tuned for the 1600×1200 13.3" panel, set by overriding the `--font-*` / `--icon-*` tokens on the frame root so reused widgets scale with it. The shared text size (`text_size`, Settings → Frame's Text size slider) multiplies the reading sizes (title down to caption) and icons by `--frame-text` (100–150%, never below the tuned floor), and the display faces (weekday, date, hero, stamp, poster) by `--frame-display-text` — the same value in every mode but Daily, whose masthead and countdown stamps are sized to their rows. `FrameSunPath`'s labels keep the tuned size too, since they sit in the dome's fixed geometry. In Daily, larger text just fits fewer list items — the line budget is measured. Note already fills the panel, so there the text size moves a kept-lines note from its unwrapped size towards its wrapped size in proportion (none at 100%, all the way at 150%), trading unbroken lines for bigger, wrapped text a step at a time. Other panel sizes multiply them by `--frame-scale` (how far the 1600×1200 reference must shrink or grow to fit the panel in both directions — `min(width ÷ 1600, height ÷ 1200)`, from `frameCanvas()` in `lib/frame.ts`), and the root also scales Tailwind's `--spacing`, so padding and gaps keep their proportions; 3px line weights stay fixed so they land on whole pixels. Px thresholds measured in JS (Coming Up's narrow rail, the countdown stamps per row, the Ambient plate inset, the sun path) read the scale via `getFrameScale()`. Transitions and animations are disabled.
- The root's `data-ready` attribute becomes `"true"` once data, fonts, and (ambient) the photo have settled, or after 15 s regardless, so one hung upstream can't stall the frame — screenshot after that. The route loads only what its layout needs (the root layout skips its own loads on `/frame*`) and never polls; the renderer reloads it per frame.
- Ambient mode credits the Unsplash photographer in the corner card, per Unsplash's API guidelines.
- **Settings → Frame**'s Layout card (`FrameTemplateEditor`) picks a template from thumbnails drawn in the current orientation, then a widget per slot from a `Select` that lists only the widgets fitting that slot's size; picking one already placed moves it. The Columns template shows the same Widgets editor as Settings → Dashboard: the shared `WidgetLayoutEditor` — drag between columns, resizable split, locked header/footer strips, a hide button on each placed widget and a Hidden tray of chips that adds one back (disabled while a frame column limit leaves no room). Photo mode's photo picker opens in a sheet from a strip of the picked thumbnails, so it doesn't grow with the upload count. Settings → Dashboard and → Ambient lay their cards out two-up once the content area is 56rem wide (container query), and the theme pickers size their columns to the card, not the screen. A Preview card (`FramePreview`) shows the real `/frame` page scaled down — pinned beside the options when the settings area is at least 56rem wide (container query), scaled to fit the window's height as well as the card's width, with an expand button that opens it full size in a dialog; otherwise opened in that dialog (a sheet on phones) from a floating Preview button and mounted only while open. Every preview shares its mode, palette, view and photo — the one Display holds for the panel (`GET /display/photo`, asked again after each save lands and every minute), and the frame URL folds into a `Disclosure` under it; the colour theme and the set-once panel size and ambient card position fold into `Disclosure`s in the look card; theme, font, text size and palette changes are applied to the loaded frame in place, and it only reloads for layout, orientation or mode changes.

#### Kiosk mode

Kiosk mode can be activated in two ways: toggle **Settings → Display → Kiosk mode** (persisted to `localStorage`), or append `?kiosk=1` to the URL (useful for a browser-based wall display such as a tablet or monitor — the e-paper frame doesn't load a URL, it pulls a host-rendered bitmap; see `HARDWARE.md`). Either condition is sufficient. In kiosk mode:

- The desktop navigation header (`Nav.svelte`) is hidden.
- The `DashboardHeader` strip (live clock + date + current weather) is shown above the widget columns.
- The `DashboardFooter` (now-playing + refresh time) is shown below the widget columns.

The persistent setting is the recommended choice for a dedicated wall display. The URL param is useful when the setting cannot be changed (e.g. a shared device or a hardcoded display URL). The standard URL with no param and the setting off shows the nav and hides the dashboard header/footer chrome, which suits regular browser access.

#### Keyboard shortcuts

Shortcuts are bound with [tinykeys](https://github.com/jamiebuilds/tinykeys), whose `$mod` is ⌘ on a Mac and Ctrl elsewhere and whose sequences give the "G then …" chords. `lib/shortcuts.ts` holds the shared `ignore` filters and the list the **?** sheet shows. `KeyboardShortcuts.svelte`, mounted in the root layout (so never on `/frame`, and idle on `/ambient`, where any key exits), handles **G** then **D/C/A/S** to go to the Dashboard, Calendar, Ambient or Settings, which helps in kiosk mode with no nav bar. **?**, or the keyboard button at the end of the desktop nav, opens a sheet listing the shortcuts for the current page. Settings adds **Mod+B** to hide or show the wide-screen sidebar (remembered per browser), **1–7** to jump to a section and **J/K** to step through sections. Single-key shortcuts do nothing while you type in a field (tinykeys' default filter) or while a dialog is open. tinykeys also requires the exact modifiers, so a held Ctrl, Alt or Meta leaves them to the browser.

### State Management

| Store          | File                        | Persisted to    | Responsibility                                                        |
| -------------- | --------------------------- | --------------- | --------------------------------------------------------------------- |
| `theme`        | `ThemeStore.svelte.ts`      | `localStorage`  | Active theme ID; writes `dataset.theme` on change                     |
| `fontTheme`    | `FontThemeStore.svelte.ts`  | `localStorage`  | Active font theme ID; writes `dataset.font` on change                 |
| `fontSize`     | `FontSizeStore.svelte.ts`   | `localStorage`  | User size multiplier (0.9–1.3); sets `--font-user-scale` inline on `<html>` |
| `settings`     | `SettingsStore.svelte.ts`   | `localStorage`  | `/ambient` slideshow cadence (day/night) and motion, RSS feeds config, kiosk mode, subscribed train stops + per-stop line filters |
| `homeLocation` | `HomeLocationStore.svelte.ts` | Server (SQLite) | The household's location and time zone — one shared copy in the Display service; Settings → Home loads it and saves the form on Save (the zone defaults to this browser's, unsaved) |
| `modeStore` | `ModeStore.svelte.ts` | Server (SQLite) | The frame's resolved mode, its schedule and override (including the "Show on frame" pinned photo) — Settings → Frame's Mode card and the photo collection's pin switch |
| `frameSettings` | `FrameSettingsStore.svelte.ts` | Server (SQLite) | E-paper frame theme, font, orientation, panel size, layout, split, Daily template and slots, Ambient card position, Photo mode's album and card and Countdown mode's countdown, plus the Ambient photo source and categories shared by the frame and `/ambient` — one shared copy in the Display service; Settings → Frame loads it and saves each change (debounced) |
| `tasks`        | `TaskStore.svelte.ts`       | Server (SQLite) | Task list, CRUD operations                                            |
| `calendar`     | `CalendarStore.svelte.ts`   | Server (SQLite) | Calendar items (events + Google Tasks) and Google connection status   |
| `nowPlaying`   | `SpotifyStore.svelte.ts`    | Server (SQLite) | Spotify now-playing state; polled every 5 s by `DashboardFooter`     |
| `lastPlayed`   | `LastPlayedStore.svelte.ts` | Server (SQLite) | Spotify listening history and top artist; loaded once per render by the frame's Last Played card |
| `rssStore`     | `RssFeedStore.svelte.ts`    | Server (SQLite) | RSS article list; refreshed every 30 min by `NewsFeedWidget`          |
| `weatherStore` | `WeatherStore.svelte.ts`    | Server (SQLite) | Current conditions + 7-day forecast; refreshed every 30 min by the dashboard page |
| `dailyQuote`   | `DailyQuoteStore.svelte.ts` | Server (memory) | Daily quote; re-checked hourly by `DailyQuoteWidget`                  |
| `birds`        | `BirdsStore.svelte.ts`      | Server (SQLite) | Nearby bird sightings; refreshed hourly by `BirdsWidget`              |
| `almanac`      | `AlmanacStore.svelte.ts`    | Server (computed) | Season/daylight/frost facts; loaded by `AlmanacWidget` and the frame |
| `trains`       | `TrainsStore.svelte.ts`     | Server (SQLite) | Departures for subscribed stops; refreshed every 60 s by `TrainsWidget` |
| `sportsStore`  | `SportsStore.svelte.ts`     | Server (SQLite) | Followed teams' games; refreshed by `SportsWidget` every 30 s during a game, else every 5 min, and once per render by the frame |
| `kioskStore`   | `KioskStore.svelte.ts`      | URL param       | True when `settings.kioskMode` is on or `?kiosk=1` is in the URL    |

Polled stores keep their last good data when a background refresh fails — the error state only shows when there's nothing to display yet.

### Mixed task/event lists

`api.ts` exports an `Item` discriminated union shared by `Calendar.svelte`, `DayItemList.svelte` (used by `DayOverflowModal.svelte`), and `UpcomingTasksWidget.svelte`:

```typescript
export type Item =
    | { kind: 'task';  data: Task }
    | { kind: 'event'; data: CalendarItem };
```

### Key utility functions (`utils.ts`)

- `formatTime(time)` — Converts `"HH:MM"` (24h) to `"4PM"` / `"4:30PM"` format
- `eventDateKey(event)` — Returns `"YYYY-MM-DD"` in local time; slices all-day strings directly to avoid the UTC midnight shift `new Date("YYYY-MM-DD")` causes in negative UTC offset timezones
- `stripHtml(html)` — Strips HTML tags (replacing with a space) and collapses whitespace, for plain-text previews of rich content (e.g. calendar event descriptions)
- `poll(fn, ms)` — Calls `fn` every `ms` and returns the cleanup; widgets do `onMount(() => { loadPromise = load(); return poll(load, ms); })`. `SECOND` / `MINUTE` constants for readable intervals
- `providerLabel(provider)` — Maps a calendar provider key to its display label (e.g. `"google"` → `"Google Calendar"`)

### Themes

Eighteen built-in themes are applied via CSS custom properties on `[data-theme]`. The active theme is persisted in `localStorage` via `ThemeStore.svelte.ts` and selected from the `/settings` page.

| Theme        | Style                        |
| ------------ | ----------------------------- |
| `stone`      | Dark warm neutrals            |
| `linen`      | Warm cream light mode         |
| `forest`     | Dark mossy green              |
| `dusk`       | Deep indigo dark mode         |
| `ash`        | Pure monochrome dark          |
| `chalk`      | Pure monochrome light         |
| `terracotta` | Sandy earth tones light mode  |
| `tide`       | Oceanic blue-greens           |
| `slate`      | Cool grey dark mode           |
| `blush`      | Warm pink light mode          |
| `frost`      | Crisp cool whites             |
| `smoke`      | Muted grey dark mode          |
| `sage`       | Soft green light mode         |
| `sky`        | Airy light blue               |
| `plum`       | Deep plum jewel-tone dark mode |
| `olive`      | Muted olive light mode        |
| `amber`      | Warm amber light mode         |
| `ember`      | Dark ember glow                |

Each theme exposes the same semantic token set: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, `--danger` (picker themes only), and `color-scheme`. Components reference tokens only — never hardcoded colors. Hierarchy comes from size, weight and the `--text-1`…`--text-4` steps first. Icons sit one step quieter than the data they label. `--accent` is each picker theme's one chromatic hue, kept sparing: it marks now/today, chosen or active state, primary actions and widget titles, and is never used for errors (`--danger`) or bulk text. `themes.test.ts` holds `--accent` and `--danger` to 4.5:1 on `--bg`, `--surface` and `--surface-hi`, and `--accent-fg` to 4.5:1 on `--accent` and `--accent-hi`.

Themes are defined in two places that must be kept in sync: `apps/frontend/src/themes.css` (CSS variables) and `apps/frontend/src/lib/constants/themes.ts` (switcher metadata).

Contrast floor: in every theme `--text-2` is at least 4.5:1 against `--bg` (readable secondary text) and `--text-3` at least 3:1 (icons and non-text marks only); `--text-4` is decorative. `apps/frontend/src/lib/themes.test.ts` enforces this.

**Frame themes.** `/frame` has its own themes, chosen with `?theme=` (Color mode) and picked in Settings → Frame, which also sets the frame's typeface (`?font=`, any font preset), text size (`?text=`, 100–150%), orientation and panel size (typed, or a preset for the common Inky Impression panels) and shows a live, scaled-down preview of the real `/frame` page plus its URL. They are `[data-theme='epaper-<id>']` blocks in `themes.css` with picker metadata in `constants/frameThemes.ts`, kept out of the dashboard theme picker. Colours are built from the panel's measured Spectra 6 inks, white-balanced so the browser preview looks like the panel:

| Frame theme   | Look                                                      |
| ------------- | --------------------------------------------------------- |
| `almanac`     | Default — black and indigo type, brick red, cream/butter/amber fills |
| `gallery`     | Near-monochrome, like a framed print — colour only in the Day Arc and "now" dot |
| `garden`      | Green ink, butter and amber fills                         |
| `coast`       | Indigo on sand (cream/butter), yellow golden hours, no red |
| `harbor`      | Blue and red ink only, no tints (nothing dithered)        |
| `terracotta`  | Black type, amber band, red rules, olive Day Arc          |
| `letterpress` | Black type, red rules and marks, a cream band             |
| `marigold`    | Black type and blocks, a butter band, yellow daylight     |
| `postbox`     | Red blocks and rules, blue type and "now" mark, no tints  |
| `night`       | Dark — white and yellow on black, navy band               |
| `ember`       | Dark — yellow and red on black, no fills                  |
| `moss`        | Dark — white and green on black, yellow "now" mark        |
| `lantern`     | Dark — white type, yellow rules and blocks, no fills      |
| `roundtable`  | Dark — white and gold type, red rules and blocks, no fills |

B&W mode (`?palette=bw`; without it `/frame` uses Display's saved palette) ignores the theme's colours: `epaper-bw` (black on white), or `epaper-bw-night` for dark themes. Fills are two-ink mixes of similar lightness so they dither evenly; text, icons and lines are always a pure ink (`themes.test.ts` enforces both). Text, icons and thin lines are always a pure ink. The Display service's dithering strategy (ordered for Daily, error diffusion for Ambient) is in `HARDWARE.md`. Every frame block also sets `--block` / `--on-block`: the theme's boldest pure ink for large solid shapes (date block, elapsed daylight, today's rail pins) and the ink type takes on it (≥4.5:1, tested). They also set `--arc-night` / `--arc-day` / `--arc-golden` / `--arc-outline`, which `DayArcWidget` reads with fallbacks to the regular tokens, so the ribbon stays solid-coloured on the panel without changing its look elsewhere.

### Font themes

Named typography presets, orthogonal to color themes. Each preset bundles a font family (self-hosted `@fontsource-variable` packages), four semantic weights (`--weight-regular/medium/semibold/bold` — Tailwind's `font-medium/semibold/bold` resolve to these vars via the `@theme` block in `app.css`), and a size multiplier (`--font-scale`, folded into every `--font-*`/`--icon-*` clamp in `app.css`).

Defined in two places (both must be updated together):

1. `apps/frontend/src/fonts.css` — `@fontsource` imports plus one `[data-font="id"] { ... }` block per preset (source of truth for stacks, weights, scale). Selectors stay bare `[data-font]` — `FontThemePicker.svelte` sets `data-font` on its preview buttons so the same blocks style the previews.
2. `apps/frontend/src/lib/constants/fontThemes.ts` — `fontThemes` array (picker metadata only: `id`, `label`, `tag`)

Applied as `data-font` on `<html>` by `FontThemeStore.svelte.ts` (localStorage key `hearth-font`).

Separately, a user size slider in Settings (the shared `SizeSlider`) sets `--font-user-scale` (0.9–1.3) as an inline style on `<html>` via `FontSizeStore.svelte.ts` (localStorage key `hearth-font-size`). `app.css` composes both into `--scale: calc(var(--font-scale) * var(--font-user-scale))`, which every `--font-*`/`--icon-*` clamp multiplies by.

Current font themes: `inter` (default), `system`, `nunito`, `source-serif`, `space-grotesk`, `roboto-slab`, `lora`, `manrope`, `fraunces`

### Typography Scale

A fluid type scale is defined in `app.css` using `clamp()`, exposed as `type-display`, `type-title`, `type-subtitle`, `type-body`, `type-label`, `type-caption` utility classes. Icon sizes follow the same pattern: `icon-lg`, `icon-md`, `icon-sm`, `icon-xs`. Components use these classes — never raw Tailwind `text-*` sizes.

## Backend Services

Each domain is a small, self-contained **ASP.NET Core 10 Minimal API** service backed by **SQLite** (except Almanac, which is stateless).

| Service    | Port | Status      | Responsibility                                                       |
| ---------- | ---- | ----------- | ---------------------------------------------------------------------|
| `tasks`    | 8081 | Implemented | Task CRUD with due dates, recurrence, and countdown events           |
| `weather`  | 8082 | Implemented | Polls Open-Meteo, caches current + forecast                          |
| `spotify`  | 8083 | Implemented | Spotify OAuth + now-playing                                          |
| `photos`   | 8084 | Implemented | Fetches Unsplash photos, caches batch for 24 hours                   |
| `rss`      | 8085 | Implemented | Fetches RSS/Atom feed articles, caches for 30 minutes                |
| `quote`    | 8086 | Implemented | Fetches daily quote from ZenQuotes, caches until next day            |
| `calendar` | 8087 | Implemented | Google Calendar + Google Tasks OAuth, aggregated events/tasks        |
| `birds`    | 8088 | Implemented | Nearby eBird sightings, cached for 60 minutes                        |
| `almanac`  | 8089 | Implemented | Season/daylight/frost facts, computed locally (no external API, no DB) |
| `display`  | 8090 | Partial     | Shared e-paper frame settings, the dithered frame preview and the scheduled panel render at `GET /display/frame` (via the `renderer` sidecar); the Color ↔ B&W palette (`/display/palette`); presence endpoints are planned (`HARDWARE.md`) |
| `trains`   | 8091 | Implemented | Transitland departures for subscribed stops, cached for 45 seconds   |
| `sports`   | 8094 | Implemented | Followed teams' live games, results and fixtures from ESPN, cached for 30 seconds while live |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `<service>.db`). The SQLite dependency (`Microsoft.Data.Sqlite`) is isolated to the `Data` shared library — individual service projects depend only on the `Data.Abstractions` interface, keeping them portable and testable.

## Shared Libraries

Three shared projects live under `apps/services/` and are referenced by service projects via `ProjectReference`. They are not deployed independently — they compile into each service that uses them.

| Project             | Responsibility                                                                                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Data.Abstractions` | `IDatabase` interface and `DbCommandExtensions`; depends only on `System.Data.Common` (BCL) — no SQLite or other NuGet packages |
| `Data`              | `Database` — the concrete SQLite implementation of `IDatabase` — plus `AddSqliteDatabase(key, defaultDbFileName)`, an `IServiceCollection` extension that reads `DB_PATH` and registers the keyed singleton, and `SqliteDistributedCache` / `AddSqliteDistributedCache(key)` — an `IDistributedCache` over that same database, used as FusionCache's L2. The only project that references `Microsoft.Data.Sqlite`. |
| `ServiceDefaults`   | ASP.NET Core + FusionCache (no SQLite) library referenced by **all 11** services, including the 1 with no database (Almanac). Provides `AddHearthWebDefaults()` (snake_case JSON; deliberately no CORS — every service is reached same-origin through Caddy or the Vite proxy, so a cross-origin policy would only let other sites read and change household data), the shared `HearthJson.SnakeCaseLower` options instance, `ConfigRequirement` (`RequireOrFail` / `WarnIfMissing`) for validating required env vars, `AddHearthDataProtection(appName)` for services that need to encrypt values at rest (keys persisted to `<DB_PATH dir>/keys`), `AddHearthCache()` (FusionCache — see below), `AddHearthHomeLocation()` (the shared home location — see below), and the health checks behind Settings → Diagnostics (`MapHearthHealth` — see below). |

Service projects follow this pattern:

- Reference `Data.Abstractions` for the `IDatabase` type used in stores and handlers
- Reference `Data` for the 9 SQLite-backed services, calling `services.AddSqliteDatabase("key", "service.db")` once in `ServiceCollectionExtensions.cs`
- Reference `ServiceDefaults` and call `services.AddHearthWebDefaults()` once in `ServiceCollectionExtensions.cs`, add the service's health checks with `services.AddHealthChecks()…`, and call `app.MapHearthHealth("<prefix>")` in `WebApplicationExtensions.cs`
- Never reference `Microsoft.Data.Sqlite` directly

JSON responses use `JsonNamingPolicy.SnakeCaseLower` (via `HearthJson.SnakeCaseLower` / `AddHearthWebDefaults()`) so property names match frontend conventions (e.g. `created_at`, `due_date`).

### Upstream response caching (FusionCache)

Services that proxy a third-party API with a simple "fetch, cache, serve" shape — Weather, Birds, Trains, Quote — cache through [FusionCache](https://github.com/ZiggyCreatures/FusionCache) (`IFusionCache.GetOrSetAsync`) instead of hand-written cache tables. `ServiceDefaults.AddHearthCache()` registers it with:

- **Fail-safe** on: when a refresh fails, the last good value keeps being served (up to the entry's `FailSafeMaxDuration`) instead of an error. Endpoints only return `502` when nothing was ever cached.
- **SQLite L2** when the service registered one via `AddSqliteDistributedCache(key)` (a `distributed_cache` table in the service's own `DB_PATH` file), so cached values survive container restarts. Without one (Quote), the cache is memory-only.
- `HearthJson.SnakeCaseLower` serialization for the L2 payloads.

Each service sets its own `FusionCacheEntryOptions` (duration + fail-safe window) next to its endpoint. Calendar keeps its own relational SQLite cache (items per provider); Photos keeps its in-memory `IMemoryCache` batch.

### Home location and time zone

The household's location (`latitude`, `longitude`, optional `name`) and IANA `time_zone` are set in Settings → Home and stored once, by the Display service (`GET`/`PUT /display/home`). `ServiceDefaults` holds the shared `HomeSettings` record and `HomeLocationClient`, which Weather, Birds and Almanac register with `AddHearthHomeLocation()` (it needs `AddHearthCache()`):

- `GetAsync()` reads `DISPLAY_URL/display/home` (3 s timeout), cached through FusionCache for 1 minute with fail-safe, so a saved change reaches every service within a minute and a restarting Display keeps the last answer.
- It merges in the env-var fallbacks: saved coordinates, else `LATITUDE`/`LONGITUDE`; saved zone, else `TZ`. A Display that's unset (`204`), unreachable with nothing cached, or not configured (no `DISPLAY_URL`) leaves just the env vars — so installs configured through `.env` keep working.
- Weather and Birds key their upstream caches by coordinates (and Birds by radius), so a new location doesn't serve the old place's data.

Display passes the saved zone to the renderer as `tz=` on each preview, so the frame's date and clock render in it; the renderer's own `TZ` is the fallback.

### Required env var validation

Endpoints that depend on config (API keys, coordinates, OAuth secrets) validate with `ConfigRequirement`:

- `config.RequireOrFail(logger, respond, "VAR1", "VAR2")` — logs the missing vars, and if any are missing, returns the `IResult` built by the caller's `respond` factory (each service keeps its own response shape — e.g. Weather/Birds/Spotify/Trains return `Results.Json(new { error = ... }, statusCode: 503)`, Calendar returns `Results.Problem(...)`)
- `config.WarnIfMissing(logger, message, "VAR1", "VAR2")` — logs only, never fails the request; used by Almanac, where missing coordinates degrade the response (`daylight: null`) rather than failing it

### Health checks (Settings → Diagnostics)

Every service answers `GET /<prefix>/health` (`app.MapHearthHealth(prefix)`) with ASP.NET Core's built-in health checks, written as snake_case JSON: `{ status, checks: [{ name, status, description }] }`, where a status is `healthy`, `degraded` or `unhealthy`. It is always `200` — a service that answers with a problem is different from one that doesn't answer, which the page shows as Not responding. `AddHearthWebDefaults()` registers an empty set; each service adds what matters to it in `ServiceCollectionExtensions.cs` with the `IHealthChecksBuilder` extensions in `ServiceDefaults/HearthHealth.cs`:

- `AddDatabase(key)` — `SELECT 1` against the keyed `IDatabase`; Unhealthy when it fails.
- `AddRequiredConfig("VAR", …)` — Degraded, naming the unset env vars (never their values). Integrations are optional, so a household that never set up trains sees a warning, not a failure.
- `AddHomeLocation()` — Degraded when neither Settings → Home nor the env vars give coordinates (Weather, Birds, Almanac).
- `upstream` — added by `AddHearthCache()`: `UpstreamFailures` listens to FusionCache's factory error/success events and is Degraded while a key's refresh has failed and not succeeded since (within the last hour), naming the source by the key's prefix (`weather`, `birds`, …). Fail-safe hides these from the widgets, so this is where a dead API key or an outage shows.
- Display's `frame` check (`FrameHealthCheck`) — when the panel last rendered and the frame last checked in; Degraded while the last render failed.

The frontend lists the services in `lib/constants/services.ts`, fetches each report through `api.health.service(id)` every 30 seconds, and turns it into a row with `summarizeHealth()` in `lib/diagnostics.ts`: Running, Not set up (only `config` is degraded — shown muted and left out of the "need attention" count, since integrations are optional), Needs attention, Not working, or Not responding. Calendar's health route needs its own Caddy and Vite proxy entry, because `/calendar` is a frontend page.

The same endpoints drive Docker's container health checks, so `docker ps` and Dozzle show healthy or unhealthy per service. The images are chiseled (no shell, `curl` or `wget`), so each service binary is its own probe: Compose runs `/app/service --health /<prefix>/health`, and `HealthProbe.ExitIfProbe(args)` — the first line of every `Program.cs` — calls the endpoint on the container's own port (`ASPNETCORE_HTTP_PORTS`) and exits 0 for healthy or degraded, 1 for unhealthy or no answer, printing the problems for `docker inspect`. Degraded counts as healthy, so an integration that isn't set up never marks a container unhealthy. The timing is the shared `x-probe` block in `infra/docker-compose.yml` (every 60 s, 2 s while starting). Dozzle uses its own `/dozzle healthcheck`. Nothing `depends_on` these health states, so a failing service never holds up Caddy.

### Logs (Dozzle)

The `dozzle` container ([Dozzle](https://dozzle.dev), `amir20/dozzle`) streams live, searchable logs for every container on the host at `/logs` (`DOZZLE_BASE=/logs`; Caddy's `handle /logs*` keeps the prefix). It reads the Docker socket (read-only mount, which still exposes the whole Docker API) and has container actions and shells off (Dozzle's defaults). So it's **opt-in**: the service is in the `logs` Compose profile, and only runs when `infra/.env` sets `COMPOSE_PROFILES=logs`. Compose reads that itself, so `docker compose up` and `infra/deploy/update.sh` need no flag. The health checks stay on regardless — they cost nothing until asked and expose no logs. Settings → Diagnostics' Logs card (`LogsSettings.svelte`) asks `api.logs.running()` whether `/logs/` answers (Caddy 502s when Dozzle isn't running) and shows Open logs, or how to turn them on. Hearth has no login of its own, so Dozzle uses its simple file auth (`DOZZLE_AUTH_PROVIDER=simple`): `infra/dozzle/users.yml` holds a bcrypt hash, written by `infra/deploy/dozzle-user.sh <name>`, which prompts for the password. The `infra/dozzle/` folder is bind-mounted as `/data` (Dozzle saves user settings there too) and gitignored apart from `.gitkeep`. Nothing depends on Dozzle, so without a login only `/logs` is down. The frame's Raspberry Pi is a separate machine: its client's logs stay in its own journal (`journalctl`).

## Tasks Service

The `tasks` service handles full CRUD for household tasks with optional due dates, times, recurrence, and countdown events.

### Endpoints

| Method   | Path          | Description                                                                                 |
| -------- | ------------- | --------------------------------------------------------------------------------------------|
| `GET`    | `/tasks`      | List tasks: all done tasks + undone tasks due within 1 year + undone tasks with no due date |
| `POST`   | `/tasks`      | Create a task; pre-generates all recurring instances up to 1 year ahead                     |
| `PUT`    | `/tasks/{id}` | Replace done status, title, due date, due time, description, and assignee — a null clears it |
| `DELETE` | `/tasks/{id}` | Delete a task; `?series=true` deletes it and every later instance of its recurring series   |

### Recurrence Model

Recurring tasks use a **pre-generation** approach: when a task with a recurrence rule is created, all instances up to one year ahead are inserted as individual rows at creation time. Each instance row stores the full recurrence definition and shares a `series_id` (equal to the `id` of the first instance in the series).

| Field                 | Type                             | Description                                                                             |
| --------------------- | --------------------------------- | ----------------------------------------------------------------------------------------|
| `recurrence_unit`     | `"day"` \| `"week"` \| `"month"` | Interval unit                                                                           |
| `recurrence_interval` | integer                          | Number of units between occurrences                                                     |
| `recurrence_days`     | comma-separated string           | Weekday names for weekly rules (e.g. `"Mon,Wed,Fri"`)                                   |
| `recurrence_end_date` | datetime                         | Optional last date for the series; no instances are generated beyond this date          |
| `series_id`           | integer                          | Groups instances; equals the `id` of the first instance; `NULL` for non-recurring tasks |

**Rolling horizon:** `GET /tasks` lazily extends any series whose last undone instance falls within 30 days, generating new rows up to `recurrence_end_date` (or 1 year if no end date is set). No background job is required.

**Marking done:** Marking an instance done simply sets `done = 1` on that row. Future instances already exist and remain unaffected.

**Deleting a series:** `DELETE /tasks/{id}?series=true` ("All future") deletes the given task and every row of its `series_id` due on or after it, keeping earlier instances as history, then sets the series' `recurrence_end_date` to the day before so the horizon extension doesn't regrow it.

**Supported intervals:**

| UI label  | `recurrence_unit` | `recurrence_interval` |
| --------- | ------------------ | ---------------------- |
| Daily     | `day`               | 1                       |
| Weekly    | `week`              | 1                       |
| Bi-weekly | `week`              | 2                       |
| Monthly   | `month`             | 1                       |
| Yearly    | `month`             | 12                      |

Weekly recurrences may additionally specify `recurrence_days` (e.g. `"Mon,Wed,Fri"`) to pin to specific weekdays.

### Countdown Events

Tasks with `is_countdown = 1` are one-off events tracked by time remaining rather than completion. They appear in a dedicated **Countdowns** widget on the dashboard page showing the 5 nearest upcoming events sorted by days remaining, and are filtered out of the main upcoming-tasks list.

The `is_countdown` flag is set at creation time and cannot be changed after the fact. Countdown tasks and recurrence are mutually exclusive — the UI hides repeat options when "Event countdown" is checked.

## Moon Phase Widget

The moon phase display is entirely front-end computed — no backend service is required.

**Algorithm (`apps/frontend/src/lib/constants/moonphase.ts`):**

- Anchor: `KNOWN_NEW_MOON = 2000-01-06T18:14:00Z`
- Period: `SYNODIC_PERIOD = 29.53059` days
- Phase fraction: `(daysSince % SYNODIC_PERIOD) / SYNODIC_PERIOD`
- Illumination: `(1 − cos(2π × phase)) / 2`
- Phase is mapped to one of eight named phases (New Moon, Waxing Crescent, First Quarter, Waxing Gibbous, Full Moon, Waning Gibbous, Last Quarter, Waning Crescent)

`MoonPhaseWidget.svelte` renders a custom SVG visualization, the phase name, illumination percentage, and days until the next major phase (new, first quarter, full, last quarter).

### Environment variables

| Variable                | Default    | Description                      |
| ------------------------ | ---------- | ---------------------------------|
| `DB_PATH`               | `tasks.db` | Path to the SQLite database file |
| `ASPNETCORE_HTTP_PORTS` | —          | Set to `8081` in Docker          |

## Weather Service

The `weather` service fetches current conditions and a 7-day forecast from [Open-Meteo](https://open-meteo.com/) (no API key required) and caches the result (one `WeatherSnapshot` entry: current + forecast) via FusionCache for 30 minutes, falling back to the last good snapshot for up to 6 hours if Open-Meteo is down.

### Endpoints

| Method | Path                | Description                                     |
| ------ | -------------------- | ------------------------------------------------|
| `GET`  | `/weather/current`  | Returns current conditions; uses cache if fresh |
| `GET`  | `/weather/forecast` | Returns 7-day forecast; uses cache if fresh     |

Both endpoints return `503` with `{ "error": "location not configured" }` when there's no home location — none saved in Settings → Home and no `LATITUDE`/`LONGITUDE` — and log a `LogError`. The snapshot is cached per location (`weather:<lat>,<lon>`).

The forecast endpoint returns a `ForecastDay[]` where each day includes `sunrise` and `sunset` as ISO strings. `WeatherWidget.svelte` displays today's sunrise/sunset from `forecast[0]`.

`/weather/current` also returns nullable `uv_index` (from the same forecast call) and `us_aqi` (US Air Quality Index, fetched in parallel from Open-Meteo's separate keyless endpoint at `air-quality-api.open-meteo.com`). An air-quality fetch failure degrades to `null` rather than failing the request; both values are cached in the same snapshot under the same 30-minute TTL. `WeatherWidget.svelte` hides the UV/AQI metrics when they are `null`.

Each forecast day also carries `uv_index_max` (the day's peak, from Open-Meteo's daily `uv_index_max`; `null` where Open-Meteo has a gap). Readings come with plain-language bands from `WeatherCategories`: `uv_category` and `aqi_category` on `/weather/current`, `uv_max_category` on each forecast day — WHO UV bands (Low ≤2, Moderate ≤5, High ≤7, Very high ≤10, Extreme) on the rounded index, and EPA US AQI bands (Good ≤50 through Hazardous >300). They're computed properties on the records, never stored, so a cached snapshot's category always matches its value. The frame's forecast band shows them; the dashboard widget puts them in the UV/AQI tooltips.

### Environment variables

| Variable                | Required | Description                                        |
| ------------------------ | -------- | ---------------------------------------------------|
| `LATITUDE`              | No       | Fallback decimal latitude (e.g. `40.7128`) when none is saved in Settings → Home |
| `LONGITUDE`             | No       | Fallback decimal longitude (e.g. `-74.0060`)       |
| `DISPLAY_URL`           | No       | The Display service, for the saved home location (`http://display:8090` in Compose) |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `weather.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8082` in Docker                            |

Place these in `apps/services/Weather/.env`. Docker Compose loads the file via `env_file`; for local `dotnet run`, `DotNetEnv` loads the same file before `CreateBuilder`.

## Spotify Service

The `spotify` service handles OAuth 2.0 authorization with Spotify and exposes now-playing data and the listening history behind the frame's Last Played card. It stores a single token row in SQLite — only one Spotify account is linked at a time. Tokens are encrypted at rest (via `ServiceDefaults.AddHearthDataProtection`) before being persisted, and auto-refreshed on each `now-playing` request via `AuthorizationCodeAuthenticator`. A row that fails to decrypt (e.g. written before encryption was introduced) is treated as absent, forcing re-auth. The login asks for `SpotifyScopes.Required` (currently playing, playback state, recently played, top artists), and the row keeps the scope string Spotify granted (`scope`, carried over when a refresh response omits it). A login saved with fewer — including every one saved before the column existed, where it is NULL — reports `needs_reconnect`, and Settings → Connections shows a Reconnect button.

### Endpoints

| Method   | Path                   | Description                                                                              |
| -------- | ----------------------- | -------------------------------------------------------------------------------------------|
| `GET`    | `/spotify/auth`        | Begins OAuth flow — redirects the browser to Spotify's authorization page               |
| `GET`    | `/spotify/callback`    | OAuth callback — exchanges the code for tokens, saves them, redirects to `FRONTEND_URL`  |
| `GET`    | `/spotify/now-playing` | Returns the current track, or 204 if nothing is playing, or 401 if unauthenticated       |
| `GET`    | `/spotify/recent`      | Last Played: `{ track: { title, artist, album_name, album_art_url, played_at } \| null, top_artist }` — the latest track in the history (the ≥300px album image) and the top artist over the last ~4 weeks. 204 with neither, 401 unauthenticated, 403 when the login lacks the history scopes, 502 when Spotify can't be reached |
| `GET`    | `/spotify/status`      | Returns `{ authenticated: bool, needs_reconnect: bool }`                                 |
| `DELETE` | `/spotify/auth`        | Clears the stored tokens, effectively disconnecting Spotify                             |

### Environment variables

Stored in `apps/services/Spotify/.env` (loaded by `DotNetEnv`; also referenced via `env_file` in `infra/docker-compose.yml`):

| Variable                 | Required | Description                                                                                                                                        |
| ------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------|
| `SPOTIFY_CLIENT_ID`      | Yes      | Spotify app client ID                                                                                                                              |
| `SPOTIFY_CLIENT_SECRET`  | Yes      | Spotify app client secret                                                                                                                          |
| `SPOTIFY_REDIRECT_URI`   | Yes      | Must match a URI registered in the Spotify app dashboard (e.g. `http://127.0.0.1:8083/spotify/callback`)                                           |
| `FRONTEND_URL`           | No       | URL to redirect to after OAuth completes; defaults to `/` on the service itself — set from `FRONTEND_URL` in `infra/.env` (default `http://localhost`) in Docker |

### Frontend integration

`NowPlaying.svelte` polls `/spotify/now-playing` every 5 seconds via `SpotifyStore.ts`. The store value drives three UI states:

- `undefined` — 401 response → shows "Connect Spotify" link pointing to `/spotify/auth`
- `null` — 204 response → shows "Nothing playing · Disconnect" button
- `NowPlaying` — 200 response → shows track card with album art, progress bar, and a hover-revealed disconnect button

Clicking disconnect calls `DELETE /spotify/auth` then immediately re-polls, which returns 401 and flips the store back to `undefined`.

The frame's Last Played card (`FrameLastPlayed.svelte`, frame-only) reads `/spotify/recent` through `LastPlayedStore.svelte.ts`, whose loader also decodes the album art so the screenshot waits for it. `LastPlayedService` caches the history for an hour and the top artist for 12 hours (fail-safe on; a failed top-artist lookup only drops that line): Display re-renders Daily every 15 minutes and any changed pixel redraws the whole panel, so following every track would flash it through a listening session. `/spotify/callback` and `DELETE /spotify/auth` drop both cache entries. `playedLabel()` in `lib/music.ts` words the time so it stays true until the next render — "this morning", "last night", "Tuesday evening", then a date — never "2 hours ago".

## Photos Service

The `photos` service fetches portrait or landscape photos from the [Unsplash API](https://unsplash.com/developers) and caches a batch of 20 in SQLite for 24 hours. The frontend `/ambient` route rotates through these photos at a user-configured cadence.

### Endpoints

| Method   | Path                    | Description                                                                                  |
| -------- | ------------------------ | ----------------------------------------------------------------------------------------------|
| `GET`    | `/photos/random`        | Returns one random `PhotoResponse` from cache; refetches if cache is stale or query changed  |
| `GET`    | `/photos/sources`       | Returns the list of available photo source names                                             |
| `GET`    | `/photos/uploads`       | Returns the list of user-uploaded photos                                                      |
| `POST`   | `/photos/uploads`       | Uploads a photo (multipart/form-data; max 200 MB). Stored upright (EXIF orientation applied) as WebP, at most 2560 px plus a 400 px thumb |
| `DELETE` | `/photos/uploads/{id}`  | Deletes a user-uploaded photo by ID (and drops it from every group)                           |
| `GET`    | `/photos/groups`        | Lists the photo groups (`id`, `name`, `photo_ids`) — only uploads that still exist             |
| `POST`   | `/photos/groups`        | Creates a group from `{ name, photo_ids }` (name 1–40 characters; at most 50 groups; unknown ids dropped) |
| `PUT`    | `/photos/groups/{id}`   | Replaces a group's name and photos and returns it; `404` if there's no such group             |
| `DELETE` | `/photos/groups/{id}`   | Deletes a group (its photos stay)                                                             |

Photo groups are named sets of uploads ("Winter", "Family", …) that Settings → Frame's Photo mode picker (`PhotoModePicker.svelte`) uses to pick several photos in one tap. They're kept in `groups.json` beside `captions.json` under `DATA_PATH` (`PhotoGroupStore`); both files are written atomically (`JsonFile`), and one that can't be read is set aside as `<name>.bad` rather than stopping the service. Picking a group copies its photos into the frame settings' `photo_ids`; the picker also picks one photo at a time or several (shift-click adds a run), with select all/none.

Query param `query` (default: `nature`) is forwarded to the Unsplash random photo endpoint. The cache is keyed by query — changing categories busts the cache.

### Response shape

```json
{
  "id": "abc123",
  "url": "https://images.unsplash.com/...",
  "description": "A misty forest at dawn",
  "photographer_name": "Jane Smith",
  "unsplash_link": "https://unsplash.com/photos/abc123"
}
```

### Caching

| Field      | Value                                                             |
| ----------- | ------------------------------------------------------------------|
| Batch size | 20 photos per fetch                                               |
| TTL        | 24 hours                                                          |
| Cache bust | Query string changes (i.e. user changes photo categories)         |
| Fallback   | Returns 502 if Unsplash is unreachable and no valid cache exists  |

### Environment variables

Stored in `apps/services/Photos/.env`:

| Variable                | Required | Description                                       |
| ------------------------ | -------- | ---------------------------------------------------|
| `UNSPLASH_ACCESS_KEY`   | Yes      | Unsplash API access key (free tier: 50 req/hr)    |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `photos.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8084` in Docker                            |

### Frontend integration

Ambient photos are chosen once for every display: the **photo source** (Unsplash, your uploads, or both) and **categories** live in the shared frame settings (`FrameSettingsStore` → Display service), set in Settings → Ambient. The frame's Ambient mode picks with them in the Display service (`FramePhotos`), the browser `/ambient` slideshow through `lib/photos.ts` — the same rules in both (`resolveSource` — "both" picks an upload half the time when there are any; `photoQuery` — categories joined into the `query` param, `nature` when none; `uploadToPhoto`).

`/ambient` (`routes/ambient/+page.svelte`) loads the shared settings, opens on the frame's current Ambient photo (`GET /display/photo?mode=ambient`, so it starts on the picture the panel shows; its own pick if Display has none), then fetches a new photo on each cadence interval (shuffling through uploads for "My photos"). Its playback stays per browser in `SettingsStore`:

- **`cadenceSeconds`** / **`nightCadenceSeconds`** — interval between photo advances (2m … 2hr), optionally slower 10pm–6am
- **`ambientMotion`** — slow Ken Burns pan/zoom

The Unsplash photographer credit is always shown (Unsplash's API guidelines require it); an upload shows its caption instead. The frame's photo is held by the Display service (`FramePhotos`, see "Panel render"), so the panel, Settings' preview and `/frame` in a browser show the same one; none of them picks its own.

Photos transition with a 1.5-second crossfade. Clicking anywhere or pressing any key exits back to `/`.

## Quote Service

The `quote` service fetches the daily quote from the ZenQuotes API and caches it in memory (FusionCache, no L2) until the next UTC day.

### Endpoints

| Method | Path     | Description                                                    |
| ------ | -------- | -----------------------------------------------------------------|
| `GET`  | `/quote` | Returns the current day's quote as `{ q: string, a: string }`  |

If the cached quote is from a prior UTC day (or absent), the service re-fetches before responding. On fetch failure, fail-safe returns the last cached quote (up to 24 hours old); if no quote has been fetched since the service started, it returns 503.

### Caching

| Field  | Value                                          |
| ------ | ------------------------------------------------|
| TTL    | Until the next UTC midnight                     |
| Source | `https://zenquotes.io/api/today`               |

### Environment variables

| Variable                | Required | Description                                      |
| ------------------------ | -------- | ---------------------------------------------------|
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8086` in Docker                            |

### Frontend integration

`DailyQuoteWidget.svelte` loads the quote on mount via `DailyQuoteStore.svelte.ts` and re-checks hourly.

## RSS Service

The `rss` service fetches articles from one or more user-configured RSS/Atom feed URLs and caches them per feed with FusionCache (SQLite L2, so the cache survives restarts).

### Endpoints

| Method | Path            | Description                                                                                |
| ------ | ---------------- | ----------------------------------------------------------------------------------------------|
| `GET`  | `/rss/articles` | Accepts one or more `url` query params and a `count` (default 10); returns a `RssFeedGroup[]` sorted by publish date |

Every supplied `url` is validated by `FeedUrlValidator` before any fetch (SSRF protection): only absolute `http`/`https` URLs are accepted, and the host — after DNS resolution when it is not an IP literal — must resolve exclusively to publicly routable addresses. Loopback, RFC 1918 private, link-local (including the `169.254.169.254` cloud metadata IP), CGNAT, multicast, and the IPv6 equivalents (`::1`, `fc00::/7`, `fe80::/10`, IPv4-mapped) are rejected. Any invalid or disallowed URL causes the endpoint to return `400 Bad Request` with an `error` message.

Feeds are fetched in parallel (`RssFeeds`, 15 s timeout each). A feed is re-fetched once its 30-minute entry expires, by one request at a time however many screens ask. When a refresh fails, fail-safe keeps serving the last good copy for up to a day and retries every 5 minutes; a feed that has never been fetched successfully is left out of the response.

### Caching

| Field  | Value                                            |
| ------ | --------------------------------------------------|
| TTL    | 30 minutes per feed URL; fail-safe up to 1 day   |
| Format | Atom 1.0 (XDocument parsing; RSS 2.0 fallback)   |

### Environment variables

| Variable                | Required | Description                                     |
| ------------------------ | -------- | ---------------------------------------------------|
| `DB_PATH`               | No       | Path to SQLite cache file (default: `rss.db`)   |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8085` in Docker                            |

### Frontend integration

`NewsFeedWidget.svelte` loads articles on mount via `RssFeedStore.svelte.ts` (`api.rss.articles()`) and refreshes every 30 minutes. Feed URLs and the article count are controlled by the settings persisted in `SettingsStore.svelte.ts`, configurable in Settings → RSS Feeds. Clicking an article opens the link in a new tab.

## Calendar Service

The `calendar` service integrates with Google Calendar and Google Tasks via OAuth 2.0, surfacing events (read-only) and tasks (completable via checkbox) from the start of yesterday (UTC — so the viewer's whole local day is covered) through the next 60 days. It's built behind an `ICalendarProvider` interface so additional providers (Outlook, etc.) can be added without changing the aggregation endpoint.

### Extensibility interface

```csharp
public interface ICalendarProvider
{
    string ProviderKey { get; }          // "google", "outlook", etc.
    bool IsAuthenticated { get; }        // sync SQLite row-presence check
    string GetAuthUrl(string state);
    Task HandleCallbackAsync(string code, CancellationToken ct = default);
    void Disconnect();                   // clears token + items cache
    Task<IEnumerable<CalendarItem>> GetItemsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task SetTaskCompletedAsync(
        string taskListId, string taskId, bool completed, CancellationToken ct = default);
}
```

`/calendar/items` injects `IEnumerable<ICalendarProvider>` and fans out across all authenticated providers. Adding a second provider means implementing `ICalendarProvider` and registering it with `services.AddSingleton<ICalendarProvider>(...)`.

### Endpoints

| Method   | Path                                       | Description                                                              |
| -------- | ------------------------------------------- | ---------------------------------------------------------------------------|
| `GET`    | `/calendar/google/auth`                    | Validate env vars, generate CSRF state, redirect to Google               |
| `GET`    | `/calendar/google/callback`                | Validate state, exchange code for tokens, redirect to frontend. If Google refuses (consent declined, or `invalid_client` for a wrong client secret), logs it and redirects to `/settings?section=connections&calendar_error=<code>`, which the Calendar card explains |
| `GET`    | `/calendar/google/status`                  | `{ authenticated: bool }`                                                 |
| `DELETE` | `/calendar/google/auth`                    | Clear token + items cache (disconnect)                                   |
| `GET`    | `/calendar/items`                          | Aggregated `CalendarItem[]` from all authenticated providers (5-min cache) |
| `PATCH`  | `/calendar/google/tasks/{listId}/{taskId}` | Toggle Google Task completion, invalidate cache                          |

### SQLite schema

`access_token`/`refresh_token` are encrypted at rest (via `ServiceDefaults.AddHearthDataProtection`) before being written — the columns stay `TEXT` but hold protected payloads, not plaintext. A row that fails to decrypt is treated as absent, forcing re-auth.

```sql
-- One row per provider
CREATE TABLE IF NOT EXISTS calendar_tokens (
    provider      TEXT PRIMARY KEY,
    access_token  TEXT NOT NULL,
    refresh_token TEXT NOT NULL,
    expires_at    TEXT NOT NULL   -- ISO 8601
);

-- Cached item JSON per provider, 5-minute TTL
CREATE TABLE IF NOT EXISTS calendar_items_cache (
    provider    TEXT PRIMARY KEY,
    items_json  TEXT NOT NULL,
    cached_at   TEXT NOT NULL     -- ISO 8601
);
```

### OAuth details

- Scopes: `calendar.readonly` + `tasks` (write scope required for task toggle)
- `access_type=offline` + `prompt=consent` force a refresh token on every authorization
- Token refresh: manual — checked 30 seconds before expiry using `UserCredential.RefreshTokenAsync`
- If the refresh response omits a refresh token, falls back to the stored value
- `GoogleCredential.FromAccessToken` builds service clients after refresh

### Caching

`GetItemsAsync` fetches Google Calendar events and Google Tasks in parallel, merges them, and caches the result for 5 minutes per provider. Toggling a task (`SetTaskCompletedAsync`) invalidates the cache so the next `/calendar/items` fetch reflects the change.

### `CalendarItem` record

```csharp
public record CalendarItem(
    string Kind,           // "event" | "task"
    string Id,
    string Title,
    string? Description,
    string? Location,
    string? Start,         // ISO 8601 with offset, "YYYY-MM-DD" for all-day/tasks, null for undated tasks
    string? End,           // null for tasks
    bool IsAllDay,
    string? CalendarName,
    string Provider,       // "google"
    bool? IsCompleted,     // null for events; true/false for tasks
    string? TaskListId,    // null for events; required for toggle endpoint
    string? HtmlLink       // direct URL to view in provider (event page or tasks.google.com)
);
```

### Environment variables

| Variable                | Required | Description                                                                                |
| ------------------------ | -------- | ---------------------------------------------------------------------------------------------|
| `GOOGLE_CLIENT_ID`      | Yes      | Google OAuth client ID                                                                     |
| `GOOGLE_CLIENT_SECRET`  | Yes      | Google OAuth client secret                                                                  |
| `GOOGLE_REDIRECT_URI`   | Yes      | Must match a URI registered in the Google Cloud Console (e.g. `http://127.0.0.1:8087/calendar/google/callback`) |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `calendar.db`)                                          |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8087` in Docker                                                                     |

Stored in `apps/services/Calendar/.env`.

### Frontend integration

`Calendar.svelte`, `DayOverflowModal.svelte`, and `UpcomingTasksWidget.svelte` consume `CalendarStore.svelte.ts`, which loads Google connection status and items on layout mount.

## Birds Service

The `birds` service fetches recent and notable bird observations near the configured coordinates from the eBird API v2, merges them (one sighting per species, most recent wins, notable species flagged), and caches the result via FusionCache (SQLite L2) for 60 minutes, falling back to the last good sightings for up to 24 hours if eBird is down. Distance from home is computed with the haversine formula and served in miles.

### Endpoints

| Method | Path            | Description                                                                        |
| ------ | ---------------- | -------------------------------------------------------------------------------------|
| `GET`  | `/birds/recent` | Returns `BirdSighting[]`; `503` without `EBIRD_API_KEY` or a home location, `502` when eBird is unreachable and nothing is cached |

### Environment variables

| Variable            | Required | Description                                     |
| --------------------- | -------- | ---------------------------------------------------|
| `EBIRD_API_KEY`      | Yes      | Free key from https://ebird.org/api/keygen        |
| `LATITUDE`           | No       | Fallback decimal latitude when none is saved in Settings → Home |
| `LONGITUDE`          | No       | Fallback decimal longitude                        |
| `DISPLAY_URL`        | No       | The Display service, for the saved home location  |
| `BIRDS_RADIUS_KM`    | No       | Search radius in km (default: 15)                 |
| `DB_PATH`            | No       | Path to SQLite cache file (default: `birds.db`)   |
| `ASPNETCORE_HTTP_PORTS` | —     | Set to `8088` in Docker                            |

Stored in `apps/services/Birds/.env`.

### Frontend integration

`BirdsWidget.svelte` loads sightings on mount via `BirdsStore.svelte.ts` and refreshes hourly; a failed refresh keeps the last sightings on screen.

## Almanac Service

The `almanac` service computes seasonal facts entirely locally — no external API, no SQLite (it's stateless, so it has no `Data`/`Data.Abstractions` reference and no Docker volume). The response always contains the pinned **season** section plus at most **two rotating slots**, filled in priority order (daylight → timely frost → note); unfilled or bumped sections are `null`:

- **Season** (always present) — name, Early/Mid/Late label, day-of-season, progress, and countdown to the next equinox/solstice, from a hardcoded table of solstice/equinox UTC instants (2024–2040) in `SeasonCalculator`. Hemisphere derived from the sign of the home latitude (northern when unset).
- **Daylight** — trend (min/day gained or lost over the past week), drift vs. the most recent solstice, and the single next wall-clock milestone ("Last 8 pm sunset · Aug 13") from the NOAA sunrise/sunset algorithm in `SolarCalculator`. `null` when coordinates are unset.
- **Frost** — countdown to the next of the user's typical first/last frost dates. Only claims a slot when ≤ 42 days away (bumping the note); `null` when unset or not yet timely.
- **Note** — curated phenology/in-season sentence keyed by half-month (`PhenologyData`, temperate Northern Hemisphere; `null` for southern installs).

The location and zone are the shared home location (see Shared Libraries → Home location and time zone), read per request. Unlike Weather, missing coordinates do **not** produce a `503` — the endpoint logs an error and returns `200` with `daylight: null`, because season and note are date-only.

### Endpoints

| Method | Path        | Description                                                   |
| ------ | ------------ | -----------------------------------------------------------------|
| `GET`  | `/almanac`  | Returns `{ season, daylight \| null, frost \| null, note \| null }` |

### Environment variables (all optional)

| Variable                | Description                                                  |
| ------------------------ | ----------------------------------------------------------------|
| `LATITUDE`              | Fallback decimal latitude when none is saved in Settings → Home |
| `LONGITUDE`             | Fallback decimal longitude                                     |
| `TZ`                    | Fallback IANA zone for wall-clock milestones; else the system zone |
| `DISPLAY_URL`           | The Display service, for the saved home location               |
| `FIRST_FROST` / `LAST_FROST` | `MM-DD`                                                  |
| `ASPNETCORE_HTTP_PORTS` | Set to `8089` in Docker                                        |

Stored in `apps/services/Almanac/.env`.

## Display Service

`apps/services/Display` (port 8090) holds the household's home location and time zone and the e-paper frame's shared settings — the frame is one physical display, so its look is a single source of truth rather than a per-browser preference. It also serves the frame dithered to the panel's six inks, captured and dithered by the `renderer` sidecar (below): on demand for Settings' preview, and on a schedule for the panel (`GET /display/frame`, see "Panel render" below); and the frame's Color ↔ B&W palette. The presence endpoints are still planned (`HARDWARE.md` step 2).

| Endpoint                 | Description |
| ------------------------ | ----------- |
| `GET /display/settings`  | The saved `FrameSettings` (`theme`, `font`, `orientation`, `left`, `right`, `split`, `template`, `slots`, `ambient_position`, `photo_source`, `photo_categories`, `width`, `height`, `photo_ids`, `photo_card`, `countdown_id`, `note_title`, `note_body`, `photo_interval_minutes`, `portrait_rotation`, `text_size`, `dithering`, `quiet_start`, `quiet_end`), or `204` if nothing has been saved — clients then use their defaults |
| `PUT /display/settings`  | Replaces them. `400` unless orientation is `landscape`/`portrait`, each column has at most 3 widgets with none repeated, `split` is 30–70, `template` is set (≤ 40 characters; omitted → `columns`) with at most 6 `slots` and no widget id repeated among them (`""` marks an empty slot), `ambient_position` is one of the six positions (omitted → `bottom-left`), and `width`/`height` (the panel's native size; omitted → 1600×1200) are 200–4000 with width ≥ height; also at most 500 `photo_ids`, `photo_card` one of `full`/`caption`/`none`, `note_title` ≤ 80 characters and `note_body` ≤ 600 characters over at most 12 lines, `photo_interval_minutes` 15–1440 (omitted → 60), `portrait_rotation` 90 or 270 (omitted → 90) `text_size` 100–150 (omitted → 100), `dithering` `hearth` or `frame` (omitted → `hearth`), and `quiet_start`/`quiet_end` both `HH:mm` and different, or both omitted. Saving re-renders the panel frame |
| `GET /display/home`      | The saved `HomeSettings` (`latitude`, `longitude`, `name`, `time_zone`), or `204` if nothing has been saved — services then use their env vars |
| `PUT /display/home`      | Replaces them. `400` unless latitude is −90–90 and longitude −180–180 (both or neither), `name` is ≤ 100 characters, and `time_zone` (optional) is an IANA zone the host knows |
| `GET /display/places?q=` | City search for Settings → Home via Open-Meteo's keyless geocoding API → `Place[]` (`name`, `label`, `latitude`, `longitude`, `time_zone`), cached 7 days (FusionCache, memory-only). `400` unless `q` is 2–100 characters, `502` when the geocoder fails |
| `GET /display/mode`      | What the frame shows now → `ResolvedMode` (`mode`, `reason`: `override`/`schedule`/`default`, `until` — when that may next change, or null — and `photo_id` for a pinned photo). A live override wins, then the first schedule rule whose window holds the household's time (Settings → Home zone, else `TZ`), then the schedule's default. `/frame` asks this when its URL has no `mode` |
| `GET /display/mode/schedule` | The `ModeSchedule` (`default_mode`, `rules`: `mode`, `days` `sun`–`sat`, `start`/`end` `HH:mm`, and optionally `rotate` — up to 5 more modes — with `rotate_minutes`); `ambient` with no rules until saved. A rule ending before it starts runs past midnight (it belongs to the day it starts); equal times mean all day. A rotating rule shows `mode`, then each `rotate` mode, stepping every `rotate_minutes` from its window's start (midnight for all-day rules) and wrapping round; `until` is the next step (or the window's end). Inside the quiet hours it keeps the step it was on when they began — the window's first if it began during them — until they end |
| `PUT /display/mode/schedule` | Replaces it. `400` for an unknown mode, a rule with no or unknown days, bad times, more than 20 rules, or a `rotate` with an unknown mode, more than 5 modes, or no `rotate_minutes` in 5–1440 |
| `PUT /display/mode/override` | `ModeOverride` (`mode`, optional `until`, optional `photo_id` — photo mode only). No `until` holds until cleared; "Show on frame" pins a photo this way. `400` for an unknown mode or an `until` already past |
| `DELETE /display/mode/override` | Back to the schedule. With `?photo_id=`, only if that photo is the one pinned — the frontend calls it after deleting an upload. `204` |
| `POST /display/mode/next` | Steps to the next mode (`ambient` → `daily` → `photo` → `countdown` → `night` → `note` → …) as an override until the schedule's next change — a rule's window starting or ending, or a rotating rule's next step, so the rotation picks up again → the new `ResolvedMode`. For the frame's Button B |
| `GET /display/palette`   | The frame's Color ↔ B&W mode → `{ palette }`, `color` or `bw`; `color` until saved. `/frame` uses it when its URL has no `palette` |
| `PUT /display/palette`   | `{ palette }` — sets it (Settings → Frame → Look → Colour). `400` unless `color` or `bw` |
| `POST /display/palette/toggle` | Flips it → the new `{ palette }`. For the frame's Button C |
| `GET /display/preview`   | `/frame`'s query params plus `look=screen\|paper` (and optional `kernel=`, an epdoptimize error-diffusion kernel, and `dither=ordered\|errorDiffusion\|none` to override the mode's method — `none` returns the raw screenshot, e.g. to let inky dither it for comparison) → the frame as a PNG in the six inks, upright at the panel size (`width`×`height`, swapped in portrait; default 1600×1200). Daily is ordered-dithered, Ambient error-diffused — to black and white alone when `/frame`'s `palette=bw`; `paper` repaints the inks as they look on the panel. Rendered in the saved home time zone (`tz=`), else the renderer's `TZ`. `400` for an unknown `kernel` or `dither`, `503` without `RENDERER_URL` or while the renderer is busy, `504` if the frame never got ready, `502` on any other render failure |
| `GET /display/frame`     | The panel's frame — the last scheduled render (see "Panel render"), a PNG in the six inks at the panel's native landscape size, with an `ETag` (a hash of the pixels), `Last-Modified` and `Cache-Control: no-cache`; a matching `If-None-Match` gets `304`. If a change is being rendered (a mode, override, schedule or settings change, or `/refresh`) it waits for it, up to 100 s, so the poll after a button press gets the new frame. `Retry-After` (seconds) says when to check next — the next scheduled render plus 30 s for it to finish, the server-directed refresh TRMNL's `refresh_rate` does — so a client needn't guess an interval; it can come sooner (a Settings change, a button), and during quiet hours it's hours away, so a client should cap it. `?wait=<seconds>` (up to 900) with an `If-None-Match` that matches the current frame long-polls: the request is held until the pixels change (`200` with the new frame, seconds after a Settings or mode change) or the wait runs out (`304`; also on a host shutdown); a stale or missing `If-None-Match` answers at once. `wait` counts from the request, so it includes the render wait above — keep it under the client's own request timeout. `503` without `RENDERER_URL`, or (with `Retry-After: 60`) before anything has rendered |
| `GET /display/photo?mode=ambient\|photo` | The photo that mode shows — the one the panel has or would have (`FramePhotos`) → a `Photo` (`id`, `url`, `thumb_url`, `description`, `photographer_name`, `unsplash_link`, `source`). Settings' preview and `/frame` without `?photo=` show it, so every view matches. A read that finds the panel's photo gone (its upload deleted) re-renders the panel to match. `204` when there's none to show, `503` without `PHOTOS_URL`, `400` for another mode |
| `GET /display/frame/status` | `{ mode, rendered_at, checked_at, next_render_at, etag, last_error, last_poll }` — what the panel frame shows, when its pixels last changed and were last checked, when the next render is due, the last render error, and the frame client's last `GET /display/frame` (`{ at, address, user_agent, etag }` — `address` from Caddy's `X-Forwarded-For`, `etag` the picture it was left holding, null on a `503`; `FramePolls`, in memory only, so null until the first poll after a restart). Settings → Frame → Mode shows it as "The frame checked in 3 min ago and has the latest picture", warning after an hour without one |
| `POST /display/frame/refresh` | Re-renders the panel frame now with the next Ambient/Photo photo (e.g. a long press on Button A) → `202` |

The schedule, settings, palette and override endpoints (`PUT /display/settings`, `PUT /display/home`, `PUT /display/mode/schedule`, `PUT`/`DELETE /display/mode/override`, `POST /display/mode/next`, `PUT /display/palette`, `POST /display/palette/toggle`) each invalidate the panel frame so it re-renders.

The mode ids live in `Records/FrameModes.cs` and `apps/frontend/src/lib/constants/frameModes.ts` — keep them in step. `ModeResolver` is a pure function of the schedule, override, time, zone and quiet hours (a rotation's step is worked out from the clock, not stored, so restarts and edits keep it in step); `ModeStore` keeps the schedule and override in their own one-row tables, and `PaletteStore` the palette — apart from the frame settings, which the frontend saves whole, so Button C's flip isn't put back by an open Settings tab. `DitherFor` ordered-dithers the flat modes (Daily, Countdown, Night, Note) and error-diffuses the photo ones (Ambient, Photo).

`FrameSettingsStore` and `HomeSettingsStore` each keep one row (`id = 1`) in `display.db` (`DB_PATH`) — separate tables, because the frontend saves the frame settings whole on every change. Theme, font and widget ids are the frontend's; `FrameSettingsStore.svelte.ts` drops ids it doesn't know when it reads them. On first load with nothing saved, the frontend uploads any frame settings an older `localStorage` snapshot held (parked under `hearth-frame-legacy` by `SettingsStore`), once.

| Variable                | Required | Description |
| ----------------------- | -------- | ----------- |
| `DB_PATH`               | No       | Path to the SQLite file (default: `display.db`) |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8090` in Docker |
| `RENDERER_URL`          | For preview | The renderer sidecar (`http://renderer:3000` in Compose) |
| `PHOTOS_URL`            | For Ambient and Photo | The Photos service (`http://photos:8084` in Compose), which `FramePhotos` picks the frame's photo from |
| `TZ`                    | No       | Fallback zone for the mode schedule when none is saved in Settings → Home (`infra/.env`, shared with the renderer); UTC when neither is set |

#### Panel render

`FrameRenderService` (a `BackgroundService`) renders the frame for the panel so `GET /display/frame` never captures per request — a Spectra 6 refresh flashes for ~30 s, so the frame should only change when its content does. Each render captures `/frame?mode=<resolved>` (plus `&palette=bw` in B&W) at the saved panel size and orientation, dithered for that mode (`DitherFor`) — or not at all when `dithering` is `frame` (Settings → Frame → Dithering "On the frame"), so inkypull hands the raw screenshot to `inky`; B&W is always dithered here, since `inky` would scatter its six inks through the greys — in the home time zone, and — in portrait — turned onto the panel's native landscape by `portrait_rotation` (clockwise degrees; the renderer's `rotate=`). A render is due when nothing is cached, the resolved mode or pinned photo differs from the cached frame's, the mode's `until` passes, 15 minutes after the last render in the flat modes (Daily, Countdown, Night, Note — which also picks up task and calendar changes), or `photo_interval_minutes` after it in Ambient and Photo. Ambient and Photo capture the photo `FramePhotos` holds, passed as `&photo=<json>`, so the panel shows the one Settings' preview (`GET /display/photo`) does: one held for each mode, picked through the Photos service (`PHOTOS_URL`; `resolveSource`, the category query and Photo's pin → album → Ambient's photo order, as `lib/photos.ts`) and kept in `photos.json` beside `DB_PATH`. It moves on at an interval render of a frame already in that mode, at `POST /display/frame/refresh`, and on coming back to the mode (a schedule boundary, a rotating rule) once it's a photo interval old; a photo-choice change (source, categories, orientation, album, pin) or a held upload being deleted picks again (a read that finds the panel's upload deleted re-renders it), and look changes and retries keep it. If Photos can't be reached, the held photo stays (even for a choice changed since); with nothing held the frame shows "No photo available". Inside the quiet hours (`quiet_start`–`quiet_end` in the home time zone, past midnight when the end is earlier; `QuietHours`) those interval renders wait for the window to end, so the panel holds its picture overnight; a mode change, a pinned photo, the mode's `until` and any invalidation still render. A rotating rule's steps are mode changes, so `ModeResolver` holds them through the quiet hours instead. Invalidations render at once; any made during a render are coalesced into one more. Pixels identical to the cached frame keep its `ETag`, so the panel doesn't redraw; a failed render keeps the last frame and retries a minute later (10 s while nothing is cached, e.g. the renderer is still starting), or straight away if the mode has moved on since; an error reading the settings or schedule is logged and retried rather than stopping the service. `FrameCache` drops a saved frame whose PNG doesn't match its saved `ETag`. `FrameCache` keeps the frame in memory and in `frame.png` + `frame.json` beside `DB_PATH`, so a restart (a host update) serves the same picture and keeps the schedule. Without `RENDERER_URL` the service logs an error and never renders. Ambient's plate shows "Updated h:mm" — when the render ran — rather than a clock that would be wrong between renders. Daily's sun — the disc on `FrameSunPath` and Today's Arc "now" dot (`DayArcWidget hourly`) — is drawn at the nearest whole hour (`nearestHour` in `lib/frame.ts`), so its 15-minute re-renders only change the panel when something else changed or once an hour as the sun moves; dates still use the real time.

### Renderer sidecar

`apps/renderer/` is a small Node container (not a .NET service — Playwright needs Node and Chromium's system libraries, which the chiseled image lacks) built on the official multi-arch Playwright image. `GET /render?path=/frame?…&width=&height=&dither=ordered|errorDiffusion|none&kernel=&palette=color|bw&look=screen|paper&tz=&rotate=0|90|180|270` loads the frame from `FRAME_BASE_URL` (`http://caddy` — Caddy's `:80` site serves the frontend and the API routes the page calls), waits for `[data-ready="true"]` (40 s for load and ready together, then `504`; an error page fails at once with `502`), screenshots it (with subpixel text antialiasing off, so glyph edges don't dither into stray inks) and dithers it (`dither.mjs`, see `HARDWARE.md` step 2 for why each method). The browser draws in the request's `tz` (an IANA zone, checked with `Intl` — `400` if unknown; Display sends the saved home zone), else `infra/.env`'s `TZ` (UTC when unset or invalid, with an error logged), since the frame's date and clock come from the browser (`timezone.mjs`). One render runs at a time with at most one waiting (`503` beyond that), and a render whose client has disconnected is dropped. Each render has a hard 50 s deadline covering every step (`queue.mjs`): one that runs past it — e.g. stuck in a Chromium call with no timeout of its own — fails with `504`, Chromium is killed and relaunched for the next render, and the queue moves on instead of answering `503` until a restart. An unknown `kernel` is a `400` (epdoptimize would otherwise quietly use Floyd–Steinberg). `palette=bw` greys the screenshot by luma and dithers it to black and white alone — a mid-grey is nearest blue and green, so the six-ink set would speckle it. `rotate` turns the result clockwise, pixel for pixel (`rotate.mjs`), after dithering — Display's panel render uses it for portrait frames; the preview stays upright. It only accepts `/frame` paths and has no Caddy route. Tests: `cd apps/renderer && npm test`.

Settings → Frame's preview has a Live / Screen / Paper switch: Live is the `/frame` iframe; Screen and Paper show `/display/preview`, rendered 600 ms after the settings stop changing and only while one of them is selected.

## Trains Service

The `trains` service fetches upcoming departures for user-subscribed transit stops from the [Transitland v2 API](https://www.transit.land/documentation/how-to-use-transitland-apis) and caches each stop's result via FusionCache (SQLite L2) with a 45-second TTL. Unlike the other external-API services, there's no location configuration — the user picks specific stops (Transitland Onestop IDs) to subscribe to from the Settings page.

### Endpoints

| Method | Path                 | Description                                                                        |
| ------ | --------------------- | -------------------------------------------------------------------------------------|
| `GET`  | `/trains/departures` | Accepts one or more repeated `stop` query params (Transitland Onestop IDs); returns `StopDepartures[]` |

Behavior:

- No `stop` params → returns `200` with `[]` immediately; no config check or fetch.
- `TRANSITLAND_API_KEY` missing → `503` with `{ "error": "trains not configured" }`, logged via `LogError`.
- Each requested stop is refetched from Transitland only if its cache entry is older than 45s. A fetch failure for one stop is logged and that stop's group is served from the fail-safe copy (up to 10 minutes old), or omitted entirely if it was never successfully cached — it never fails the whole request. **Unlike Birds, a Transitland outage never surfaces as a `502`** — the endpoint always returns `200` and degrades gracefully.
- Parent stations (no departures of their own) have their child stops' (platforms/gates) departures merged in automatically.
- GTFS `stop_headsign` is preferred over `trip_headsign` when both are present.
- `is_realtime` and `estimated_departure` are only populated when Transitland's `schedule_relationship` is `SCHEDULED` (vs. `STATIC`, which has no live prediction).
- GTFS `route_type` is mapped to a coarse `mode`: `tram`, `subway`, `rail`, `bus`, `ferry`, `cable tram`, `aerial lift`, `funicular`, `trolleybus`, `monorail`, or `transit` as the fallback for unknown/missing route info.

### Response shape

```json
[
  {
    "stop_key": "s-9xj5pvewxk-...",
    "stop_name": "Union Station",
    "departures": [
      {
        "route_short_name": "15",
        "route_long_name": "Federal Blvd",
        "route_type": 3,
        "mode": "bus",
        "headsign": "Downtown",
        "scheduled_departure": "14:32:00",
        "estimated_departure": "14:35:00",
        "is_realtime": true
      }
    ]
  }
]
```

### Caching

| Field        | Value                                                                        |
| ------------- | -------------------------------------------------------------------------------|
| TTL          | 45 seconds per stop                                                          |
| Key          | `trains:{stop_key}` (one entry per subscribed stop)                          |
| Fail-safe    | 10 minutes — older departures are worse than none                            |

### Filtering

Line/route filtering happens entirely client-side — the backend has no route or direction filter params, only `stop`. `TrainStop.lineFilter` (an optional array of `${route_short_name}|${headsign}|${mode}` keys, persisted per-stop in `SettingsStore.svelte.ts`) means: unset shows all lines; an empty array hides every line for that stop (how "Deselect all" works). `TrainStopSettings.svelte` renders the per-line checkboxes and Select All / Deselect All controls.

### Environment variables

| Variable                | Required | Description                                                                    |
| ------------------------ | -------- | -----------------------------------------------------------------------------------|
| `TRANSITLAND_API_KEY`   | Yes      | Transitland v2 API key (free signup) — checked per-request, not at startup     |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `trains.db`)                               |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8091` in Docker                                                          |

Stored in `apps/services/Trains/.env`.

### Frontend integration

`TrainsStore.svelte.ts` loads departures for every stop key in `settings.trainStops` via `api.trains.departures()`, which builds repeated `?stop=` query params and returns `null` on any non-OK response (the store then keeps the last good board). The widget refreshes every 60 seconds. `TrainsWidget.svelte` groups departures by line, shows up to 3 upcoming times per line (e.g. "5, 20, 38 min"), clusters bus vs. train/rail lines (whichever has the soonest departure first), and applies each stop's `lineFilter`. `TrainStopSettings.svelte` is the Settings-page UI for subscribing to stops (`subscribeToTrainStop` / `unsubscribeFromTrainStop`) and configuring per-line filters.

## Sports Service

The `sports` service follows the household's teams through ESPN's public site API (`site.api.espn.com/apis/site/v2/sports/{sport}/{league}/…` — keyless but undocumented, so it can change without notice). The leagues it offers are listed in `Leagues.cs`: NWSL, MLS, Premier League, NFL, NBA, WNBA, MLB and NHL. Followed teams are saved in its SQLite file (`followed_teams`), not per browser, so the frame's headless render follows the same teams as every screen.

### Endpoints

| Method   | Path                                 | Description                                                                 |
| -------- | ------------------------------------ | --------------------------------------------------------------------------- |
| `GET`    | `/sports/leagues`                    | The leagues on offer: `{ key, name, sport, path }[]`                        |
| `GET`    | `/sports/leagues/{league}/teams`     | That league's teams, by name; `404` for an unknown league, `502` if ESPN never answered |
| `GET`    | `/sports/followed`                   | Followed teams, in the order they were followed                             |
| `POST`   | `/sports/followed`                   | `{ league, team_id }` — looks the team up in the league's list and saves its names and logo; returns the followed list (`400` unknown league, `404` unknown team) |
| `DELETE` | `/sports/followed/{league}/{teamId}` | Unfollows a team (`204`)                                                    |
| `GET`    | `/sports/games`                      | `{ teams: TeamGames[], next_poll_seconds }` — each followed team's live game, last result (with its `outcome`) and next game |

Each team's games come from three ESPN calls, merged by event id with the latest copy winning: the team's schedule (results), the same schedule with `?fixture=true` (soccer only — the plain soccer schedule holds no future games) and the league's current scoreboard, which carries the live clock. `EspnFetcher` maps both of ESPN's event shapes — scores are strings on the scoreboard and `{ value }` objects on the schedule, and logos and broadcasts nest differently — into one `Game`: `state` (`pre`, `in`, `post`), `completed` (false for a postponed game), `status` (ESPN's short detail: `72'`, `HT`, `FT`, `Final/OT`…), both competitors with score, shootout score and record, the broadcast networks and a note such as a playoff round. Scores are null before kickoff. `outcome` is `win`, `loss` or `draw` from the followed team's side, with a shootout deciding a level score. One failed feed is logged and drops only its games. ESPN gzips some responses unasked, so the fetcher's handler decompresses automatically.

### Caching

| Entry                                | TTL                                   | Fail-safe |
| ------------------------------------ | ------------------------------------- | --------- |
| `sports:teams:{league}`              | 24 hours                              | 30 days   |
| `sports:schedule:{league}:{team}:…`  | 30 minutes                            | 3 days    |
| `sports:scoreboard:{league}`         | 30 seconds while a game is live, else 5 minutes | 6 hours |

`next_poll_seconds` follows the scoreboard: 30 while any followed team is playing, 300 otherwise. No environment variables beyond `DB_PATH` (default `sports.db`) and `ASPNETCORE_HTTP_PORTS` (`8094`).

### Frontend integration

`SportsStore.svelte.ts` holds the board (`loadSports`) and the followed list (`loadFollowedTeams`, `followTeam`, `unfollowTeam`). `SportsWidget.svelte` (dashboard) shows each team with a live strip — status, score and network — while a game is on, then the last result and the next game; it asks again after `next_poll_seconds`. Settings → Dashboard → Sports (`SportsTeamSettings.svelte`) picks a league, then a team. The frame's Last Result & Next Game card (`FrameSports.svelte`, frame-only) shows each team's abbreviation on a `--block` tab (crests dither badly to six inks), its last result and next game, and a game in progress as "Playing now" with no score, since a live score would be wrong for most of the 15 minutes between redraws. The wording lives in `lib/sports.ts`: "Won 3–2 at Denver", "2–2 (4–3 pens)" for a soccer shootout, `playedLabel` for when it was played ("last night") and `upcomingLabel` for the next game ("Tonight 7:10 PM", "Saturday 3:00 PM").

## Routing

**Caddy** acts as the reverse proxy and TLS terminator. URL-prefix routing maps paths to services:

```text
/tasks/*            →  tasks:8081
/weather/*          →  weather:8082
/spotify/*          →  spotify:8083
/photos/*           →  photos:8084
/rss/*              →  rss:8085
/quote/*            →  quote:8086
/calendar/google/*  →  calendar:8087
/calendar/items*    →  calendar:8087
/birds/*            →  birds:8088
/almanac/*          →  almanac:8089
/display/*          →  display:8090
/trains/*           →  trains:8091
/sports/*           →  sports:8094
/                   →  frontend:3000
```

The routes live in a `(hearth)` snippet that two sites import:

- **`:80`** serves plain HTTP on any hostname. `auto_https disable_redirects` stops Caddy from redirecting the HTTPS hostname to HTTPS, so LAN clients like the frame keep working over HTTP.
- **`{$HEARTH_HOST:localhost}`** serves HTTPS with `tls internal`, using a certificate from Caddy's own local CA. `HEARTH_HOST` comes from `infra/.env`. Browsers warn until that CA is trusted. Its root certificate is at `/data/caddy/pki/authorities/local/root.crt` in the `caddy-data` volume.

The Caddy config is the same in development and production — the only difference is that `frontend:3000` points to the Vite dev server in dev and the Node production server in prod.

## Infrastructure

**Docker Compose** orchestrates all services. The compose setup is split into two files:

| File                          | Purpose                                                                                                       |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------|
| `infra/docker-compose.yml`          | Production: multi-stage builds, static frontend bundle                                                       |
| `infra/docker-compose.override.yml` | Development: auto-merged by Compose; swaps the frontend for the Vite dev server and configures file watching |

### Images

- **Backend:** one shared `apps/services/Dockerfile` builds every service, selected by the `SERVICE` build arg set in each service's `build.args` in `infra/docker-compose.yml`. The SDK stage runs on the build machine's own arch (`--platform=$BUILDPLATFORM`) and cross-publishes for the target architecture only (`-a $TARGETARCH`), so arm64 images build at native speed on amd64 CI onto `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`: no shell or package manager, and `-extra` adds the ICU + tzdata that `TimeZoneInfo` needs. The apphost is renamed to `/app/service` so a single exec-form `ENTRYPOINT` works without a shell.
- **Frontend:** `apps/frontend/Dockerfile` (adapter-node build on `$BUILDPLATFORM` — its output is plain JS — then `npm ci --omit=dev` from the lockfile as the target arch) and `apps/frontend/Dockerfile.dev` (Vite dev server).
- **Non-root:** backend containers run as the image's `app` user (UID 1654), the frontend as `node`. Each backend image creates `/data` owned by UID 1654, so new named volumes inherit that ownership. Volumes created before the switch to non-root (October 2026) are root-owned and need a one-time fix, or SQLite fails with "unable to open database file". Run this per volume with the stack stopped:

  ```bash
  docker run --rm -v hearth_tasks-data:/data alpine chown -R 1654:1654 /data
  ```

- **`.dockerignore`:** `apps/services/.dockerignore` keeps `.env` secrets and local `*.db` files out of build contexts; `apps/frontend/.dockerignore` keeps host `node_modules`/build output out, since `COPY . .` would otherwise merge macOS native binaries over the container's.
- **Runtime:** every service has `restart: unless-stopped` and rotated `json-file` logs (3 × 10 MB), via the `x-defaults` anchor. The frontend and Caddy have healthchecks (busybox `wget`), and Caddy waits for the frontend to be healthy. The .NET services have no healthcheck: chiseled images have no probe tool, and starting a .NET probe process per service each interval costs too much on a low-power host. Caddy retries upstreams, and the restart policy covers crashes.

### Development workflow

```bash
cd infra && docker compose watch
```

`infra/docker-compose.override.yml` swaps the frontend for `apps/frontend/Dockerfile.dev` (Vite dev server). File changes under `apps/frontend/src/` and `apps/frontend/static/` sync into the container via HMR; changes to `package.json`, `svelte.config.js`, or `vite.config.ts` trigger a rebuild. HMR WebSocket goes through Caddy on port 80 via `HMR_CLIENT_PORT=80`.

### Production build

```bash
docker compose -f infra/docker-compose.yml up --build
```

Every built service in `infra/docker-compose.yml` also has an `image:` of `${HEARTH_IMAGE_PREFIX:-hearth}-<service>:${HEARTH_TAG:-latest}`. With no `infra/.env` that is just the name Compose would pick anyway, so local builds are unchanged.

### Deploying to a host

Deployed hosts never build: they pull images that CI publishes.

1. **CI publishes.** The `publish-images` job in `.github/workflows/ci.yml` runs on pushes to `main` once the backend and frontend jobs pass. It builds every service for `linux/amd64` and `linux/arm64` and pushes `ghcr.io/<owner>/hearth-<service>` tagged `latest` and `sha-<short>`. GHCR packages start out private.
2. **The host pulls.** Use a 64-bit OS, since only amd64 and arm64 images are published. Install Docker and add your user to the `docker` group (`sudo usermod -aG docker <user>`). Clone the repo as that user (to `/opt/hearth`, or adjust the path in `infra/deploy/hearth-update.service`) and set `User=` in that unit to the same user, copy `infra/.env.example` to `infra/.env` and fill it in, and copy each `apps/services/<Service>/.env` over by hand. Secrets never enter an image (`.dockerignore` excludes `.env`) or git. If the repo or packages are private, run `docker login ghcr.io` once as that user with a personal access token that has `read:packages`, and give `git` a read-only deploy key.
3. **It keeps itself current.** `infra/deploy/update.sh` runs `git pull --ff-only`, then, from `infra/`, `docker compose -f docker-compose.yml pull`, then `up -d --no-build --remove-orphans`, and prunes dangling images. Compose only recreates containers whose image or config changed. Caddy is restarted only when the pull changed the `infra/Caddyfile`, because `up` doesn't watch bind mounts. Install the timer to run it every 5 minutes:

   ```bash
   sudo cp infra/deploy/hearth-update.service infra/deploy/hearth-update.timer /etc/systemd/system/
   sudo systemctl enable --now hearth-update.timer
   ```

   To roll back, set `HEARTH_TAG=sha-<short>` in `infra/.env` and run `infra/deploy/update.sh`.
4. **Optionally, turn on the logs.** Add `COMPOSE_PROFILES=logs` to `infra/.env`, then run `infra/deploy/dozzle-user.sh <name>` in the checkout. It prompts for a password, writes `infra/dozzle/users.yml` and starts or restarts Dozzle; run it again to change the login. Settings → Diagnostics links to `/logs`. Without the profile, the script saves the login for later and leaves Dozzle off.

**Moving a host set up before the `apps/` + `infra/` layout.** Stop the timer (`sudo systemctl disable --now hearth-update.timer`) *before* the change reaches `main`. Otherwise the old `update.sh` pulls the new layout on its own, then fails: its compose file and the timer's `ExecStart` path are gone, git doesn't move the gitignored secrets, and Caddy's `Caddyfile` bind-mount source has moved, so a restart before the migration below leaves Caddy down. The running containers are otherwise untouched. `infra/docker-compose.yml` pins the project name to `hearth`, the name a checkout in a `hearth` folder already had, so its volumes carry over; if the checkout's folder has another name, add `COMPOSE_PROJECT_NAME=<that name, lowercased>` to `infra/.env` first. Once, in the checkout:

```bash
git pull --ff-only
mv .env infra/.env
for f in src/*/.env; do mv "$f" "apps/services/${f#src/}"; done
[ -d dozzle ] && sudo mv dozzle/* infra/dozzle/  # Dozzle's login and saved settings, if logs are on (Dozzle writes some as root)
sudo sed -i 's#/deploy/update\.sh#/infra/deploy/update.sh#' /etc/systemd/system/hearth-update.service
sudo systemctl daemon-reload && sudo systemctl enable --now hearth-update.timer
infra/deploy/update.sh
```

`FRONTEND_URL` (where Spotify and Calendar redirect after OAuth) is read from `infra/.env` and defaults to `http://localhost`. On a host, also update the redirect URIs in those services' `.env` files and register each one with its provider:

- **Spotify** accepts `https://<HEARTH_HOST>/spotify/callback`, served by Caddy's HTTPS site.
- **Google** rejects any host without a public top-level domain, so `.local` fails even over HTTPS. Without a real domain, set `GOOGLE_REDIRECT_URI=http://localhost:8087/calendar/google/callback` on the host and register that URI. Then sign in once from a laptop with a tunnel open: `ssh -N -L 8087:localhost:80 <user>@<host>`, then open `http://localhost:8087/calendar/google/auth`. The refresh token is stored encrypted in the calendar volume, so the tunnel isn't needed again unless you disconnect.

### Project site (GitHub Pages)

`site/` is a static showcase (`index.html` tour, `build.html` parts/setup/status, one shared `assets/style.css`, self-hosted Fraunces + Inter woff2) with no build step. `.github/workflows/pages.yml` uploads the folder as-is and deploys it to GitHub Pages on pushes to `main` that touch `site/**`; CI skips the .NET/frontend/renderer jobs for site-only changes: on PRs its `changes` job (`dorny/paths-filter`) runs only the stacks a PR touches, and pushes to `main` that touch only docs/site don't trigger CI at all. It's served at the custom domain `hearth.lucas-vertrees.me` (a CNAME in the domain's Vercel DNS pointing to `lrv1668-gif.github.io`; the domain is set in the repo's Pages settings, so no `CNAME` file is needed with an Actions deploy). Keep every link and asset path relative so the site also works under the `lrv1668-gif.github.io/Hearth/` fallback path; only the absolute `og:image` URLs name the domain. Screenshots in `site/assets/img/` are captured from a running stack with personal endpoints (tasks, calendar, birds, trains, RSS, Spotify, uploaded photos) replaced by demo data — never commit captures of real household data. Keep the site's status table in `build.html` in step with what's actually built.
