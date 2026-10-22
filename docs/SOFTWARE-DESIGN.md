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
        └── /*         → Frontend   :3000
```

Every service is an independently deployable **ASP.NET Core 10 Minimal API** process with its own SQLite database — except Almanac, which is stateless.

## Frontend

### SvelteKit 2 + Svelte 5 + Tailwind CSS v4 + Lucide Svelte

- Lightweight runtime, well-suited for an always-on ambient display
- Svelte 5 runes syntax (`$state`, `$effect`, `$props`, `$derived`) for fine-grained reactivity
- Tailwind v4 for utility-first styling with a muted, theme-switchable palette
- [Lucide Svelte](https://lucide.dev/guide/packages/lucide-svelte) for icons (`@lucide/svelte`)
- API calls are centralised in `frontend/src/lib/api.ts`; all paths are relative (e.g. `/tasks`)
- In development, Vite proxies each API path (`/tasks`, `/weather`, `/spotify`, `/photos`, `/rss`, `/quote`, `/calendar`, `/birds`, `/almanac`, `/trains`) to its service's `$<NAME>_URL` env var; in production, Caddy handles the same routing

### Routes

| Route       | Description                                                                               |
| ----------- | ----------------------------------------------------------------------------------------- |
| `/`         | Dashboard — configurable widget columns (tasks, weather, countdowns, moon phase, news, quote) |
| `/calendar` | Calendar view with month grid and per-day task overflow modal                             |
| `/ambient`  | Fullscreen photo slideshow; click or any keypress returns to `/`                          |
| `/settings` | Collapsible sections: theme picker, ambient photo cadence, categories, widget visibility  |
| `/frame`    | E-paper render target — panel-sized canvas (default 1600×1200), no nav (see below)        |

The dashboard, nav, and footer all switch between mobile and desktop layouts at one breakpoint: Tailwind's `lg` (1024px). Below it the widget columns stack into one, the bottom tab bar replaces the masthead, and the kiosk footer is hidden (Now Playing moves to the top of the dashboard). The calendar switches earlier, at `md` (768px): below it day cells show dots instead of titles, and tapping a day lists its items under the grid in `DayItemList.svelte` — the same rows the desktop `DayOverflowModal.svelte` shows.

#### Frame route

`/frame` is the page the Display service's renderer screenshots and dithers for the Inky panel (`HARDWARE.md` step 1). It is sized to the panel — the shared panel size (default 1600×1200, the Inky Impression 13.3"), with width and height swapped in portrait — and never scrolls, renders outside the app chrome (`+layout.svelte` skips `Nav` for `/frame*`), and is client-rendered (`ssr = false`). Its look and layout come from the shared frame settings in the Display service (see "Display Service"), so every client — and the panel's headless renderer — draws the same frame; URL params override them per request, and anything neither sets uses the defaults below:

| Param        | Values                 | Default    |
| ------------ | ---------------------- | ---------- |
| `mode`       | `ambient`, `daily`     | `ambient`  |
| `palette`    | `color`, `bw`          | `color`    |
| `theme`      | any frame theme id (below) | `almanac` |
| `font`       | any font preset id     | `inter`    |
| `left`, `right` | comma-separated frame widget ids (Daily) | `countdowns,today` / `coming-up` |
| `split`      | left column width %, 30–70 | `50`    |
| `orientation` | `landscape`, `portrait` | `landscape` |
| `width`, `height` | the panel's native size in px, 200–4000, width ≥ height (portrait swaps them) | the shared panel size (`1600` × `1200`) |
| `position`   | Ambient card: `top-left`, `top-middle`, `top-right`, `bottom-left`, `bottom-middle`, `bottom-right` | `bottom-left` |
| `source`     | Ambient photos: `unsplash`, `local`, `both` | the shared photo source |
| `categories` | Ambient Unsplash categories, comma-separated | the shared categories (`nature` if none) |

- **Ambient** (`FrameAmbient.svelte`): full-bleed photo with a solid plate — a compact date block, time and temperature, and one sentence of weather — inset 64px at one of six positions (a corner, or the middle of the top or bottom edge). The position is the frame root's `data-position`, so the Settings preview moves the plate in place without reloading the photo. The date block keeps the theme's `--block` ink unless the photo just around the plate is close to it; then it takes whichever of the theme's other pure inks stands out most there. The route draws the photo cover-cropped into a 1/8-scale canvas once (CORS-mode load), and `FrameAmbient` scores the theme's inks against the pixels ringing the plate in OKLab (`pickBlockInk` / `plateRect` in `lib/frame.ts`), re-picking when the preview changes `data-position` or `data-theme`. Unreadable pixels fall back to `--block`.
- **Daily** (`FrameDaily.svelte`), laid out as today's edition of an almanac: a compact masthead (`FrameDateBlock` — the day of the month on a solid `--block` square; `FrameEditionLine` — a small-caps line with the day-of-year issue number, the season and days to its next marker, from the Almanac service — over the weekday in the display face; `FrameSunPath` — today's daylight as a low dome with the elapsed part filled, the sun as an accent disc, the temperature under the dome, sunrise and sunset beside its base and the daylight length below a hatched horizon) and a 5-day forecast band (`FrameForecast` — low–high range bars on one shared scale over a hatched track), with two configurable widget columns between them (`left`/`right`, up to 3 each, at `split`%). Frame widgets are registered in `constants/frameWidgets.ts`. Moon Phase, Daily Quote, Birds Nearby and Almanac are shared views in `components/widgets/views/` (`MoonPhaseView`, `DailyQuoteView`, `BirdSightingsView`, `AlmanacView`) that the dashboard widgets wrap with loading and polling and the frame renders directly with a `lines` budget; they size off `type-*`/`icon-*` tokens and em units, so the frame's larger tokens scale them, and the frame route sets `--stroke: 3px` and restyles their `.meter` bar as an outlined pure-ink bar. Today's Arc reuses `DayArcWidget`. Today, Coming Up, Week Ahead and Countdowns are frame-specific designs in `components/frame/widgets/` (the dashboard versions are interactive), sharing their selection logic through `lib/frame.ts` (e.g. both countdown widgets use `upcomingCountdowns`). Trains and now-playing are deliberately excluded — they go stale between scheduled refreshes. In portrait the two columns stay side by side at `split`% (about 500px each at an even split; Coming Up's rail moves the day and time above each title when its column is under 640px, and the countdown stamps fit fewer per row), and the masthead keeps the edition line and weekday under the date block, beside the sun path.
- **Visual language:** one 3px line weight for rules, check boxes, leaders and icon strokes (`setLucideProps({ strokeWidth: 3, absoluteStrokeWidth: true })` in the route); widget kickers (tracked caps led by a `--block` square) instead of underlined headings; large shapes in the pure `--block` ink, which never dithers; tints only in the forecast band; and one texture, `.hatch` (vertical 3px pure-ink lines every 9px). Display numerals use `--display-family` with the frame-only `.type-date` / `.type-hero` / `.type-stamp` sizes. Today is a ledger (dotted leader to the due time), Coming Up a rail (day and time in the margin, a pin per event), Countdowns a row of stamps (the soonest filled with `--accent`).
- **Fitting the space:** FrameDaily measures each column's height in lines of body text (via a hidden probe, so font presets count) and `allocateLines()` in `lib/frame.ts` splits it: every widget pays for its heading and gaps, fixed-height widgets (arc, moon, the countdown stamps) take what they need, and the rest is shared evenly. Each widget trims its list to its share — and, as a last resort, clips inside its own box rather than pushing a neighbour off the panel. Selection logic (`todayTasks`, `nextEvents`, `weekAhead`, `upcomingCountdowns`) and the masthead's small derivations (`sunProgress`, `weatherSentence`, `forecastBars`, `dayOfYear`) live in `lib/frame.ts` too.
- Portrait is for a frame hung on its side; the Ambient photo is requested in the matching orientation, and the Display service rotates the capture to the panel's native landscape.
- Type and icon sizes are px tuned for the 1600×1200 13.3" panel, set by overriding the `--font-*` / `--icon-*` tokens on the frame root so reused widgets scale with it. Other panel sizes multiply them by `--frame-scale` (how far the 1600×1200 reference must shrink or grow to fit the panel in both directions — `min(width ÷ 1600, height ÷ 1200)`, from `frameCanvas()` in `lib/frame.ts`), and the root also scales Tailwind's `--spacing`, so padding and gaps keep their proportions; 3px line weights stay fixed so they land on whole pixels. Px thresholds measured in JS (Coming Up's narrow rail, the countdown stamps per row, the Ambient plate inset, the sun path) read the scale via `getFrameScale()`. Transitions and animations are disabled.
- The root's `data-ready` attribute becomes `"true"` once data, fonts, and (ambient) the photo have settled, or after 15 s regardless, so one hung upstream can't stall the frame — screenshot after that. The route loads only what its layout needs (the root layout skips its own loads on `/frame*`) and never polls; the renderer reloads it per frame.
- Ambient mode credits the Unsplash photographer in the corner card, per Unsplash's API guidelines.
- **Settings → Frame** has the same cards as Settings → Dashboard: Visibility (`WidgetVisibilityList`) and Layout (`WidgetLayoutEditor` — drag between columns, resizable split, locked header/footer strips). Both sections use these two shared components. A Preview card (`FramePreview`) shows the real `/frame` page scaled down — pinned beside the options when the settings area is at least 56rem wide (container query), otherwise placed right after the look card; theme, font and palette changes are applied to the loaded frame in place, and it only reloads for layout, orientation or mode changes.

#### Kiosk mode

Kiosk mode can be activated in two ways: toggle **Settings → Frame → Kiosk Mode** (persisted to `localStorage`), or append `?kiosk=1` to the URL (useful for a browser-based wall display such as a tablet or monitor — the e-paper frame doesn't load a URL, it pulls a host-rendered bitmap; see `HARDWARE.md`). Either condition is sufficient. In kiosk mode:

- The desktop navigation header (`Nav.svelte`) is hidden.
- The `DashboardHeader` strip (live clock + date + current weather) is shown above the widget columns.
- The `DashboardFooter` (now-playing + refresh time) is shown below the widget columns.

The persistent setting is the recommended choice for a dedicated wall display. The URL param is useful when the setting cannot be changed (e.g. a shared device or a hardcoded display URL). The standard URL with no param and the setting off shows the nav and hides the dashboard header/footer chrome, which suits regular browser access.

### State Management

| Store          | File                        | Persisted to    | Responsibility                                                        |
| -------------- | --------------------------- | --------------- | --------------------------------------------------------------------- |
| `theme`        | `ThemeStore.svelte.ts`      | `localStorage`  | Active theme ID; writes `dataset.theme` on change                     |
| `fontTheme`    | `FontThemeStore.svelte.ts`  | `localStorage`  | Active font theme ID; writes `dataset.font` on change                 |
| `fontSize`     | `FontSizeStore.svelte.ts`   | `localStorage`  | User size multiplier (0.9–1.3); sets `--font-user-scale` inline on `<html>` |
| `settings`     | `SettingsStore.svelte.ts`   | `localStorage`  | `/ambient` slideshow cadence (day/night) and motion, RSS feeds config, kiosk mode, subscribed train stops + per-stop line filters |
| `frameSettings` | `FrameSettingsStore.svelte.ts` | Server (SQLite) | E-paper frame theme, font, orientation, panel size, layout, split and Ambient card position, plus the Ambient photo source and categories shared by the frame and `/ambient` — one shared copy in the Display service; Settings → Frame loads it and saves each change (debounced) |
| `tasks`        | `TaskStore.svelte.ts`       | Server (SQLite) | Task list, CRUD operations                                            |
| `calendar`     | `CalendarStore.svelte.ts`   | Server (SQLite) | Calendar items (events + Google Tasks) and Google connection status   |
| `nowPlaying`   | `SpotifyStore.svelte.ts`    | Server (SQLite) | Spotify now-playing state; polled every 5 s by `DashboardFooter`     |
| `rssStore`     | `RssFeedStore.svelte.ts`    | Server (SQLite) | RSS article list; refreshed every 30 min by `NewsFeedWidget`          |
| `weatherStore` | `WeatherStore.svelte.ts`    | Server (SQLite) | Current conditions + 7-day forecast; refreshed every 30 min by the dashboard page |
| `dailyQuote`   | `DailyQuoteStore.svelte.ts` | Server (memory) | Daily quote; re-checked hourly by `DailyQuoteWidget`                  |
| `birds`        | `BirdsStore.svelte.ts`      | Server (SQLite) | Nearby bird sightings; refreshed hourly by `BirdsWidget`              |
| `almanac`      | `AlmanacStore.svelte.ts`    | Server (computed) | Season/daylight/frost facts; loaded by `AlmanacWidget` and the frame |
| `trains`       | `TrainsStore.svelte.ts`     | Server (SQLite) | Departures for subscribed stops; refreshed every 60 s by `TrainsWidget` |
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

Each theme exposes the same semantic token set: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, and `color-scheme`. Components reference tokens only — never hardcoded colors.

Themes are defined in two places that must be kept in sync: `frontend/src/themes.css` (CSS variables) and `frontend/src/lib/constants/themes.ts` (switcher metadata).

Contrast floor: in every theme `--text-2` is at least 4.5:1 against `--bg` (readable secondary text) and `--text-3` at least 3:1 (icons and non-text marks only); `--text-4` is decorative. `src/lib/themes.test.ts` enforces this.

**Frame themes.** `/frame` has its own themes, chosen with `?theme=` (Color mode) and picked in Settings → Frame, which also sets the frame's typeface (`?font=`, any font preset), orientation and panel size (typed, or a preset for the common Inky Impression panels) and shows a live, scaled-down preview of the real `/frame` page plus its URL. They are `[data-theme='epaper-<id>']` blocks in `themes.css` with picker metadata in `constants/frameThemes.ts`, kept out of the dashboard theme picker. Colours are built from the panel's measured Spectra 6 inks, white-balanced so the browser preview looks like the panel:

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

B&W mode (`?palette=bw`) ignores the theme's colours: `epaper-bw` (black on white), or `epaper-bw-night` for dark themes. Fills are two-ink mixes of similar lightness so they dither evenly; text, icons and lines are always a pure ink (`themes.test.ts` enforces both). Text, icons and thin lines are always a pure ink. The Display service's dithering strategy (ordered for Daily, error diffusion for Ambient) is in `HARDWARE.md`. Every frame block also sets `--block` / `--on-block`: the theme's boldest pure ink for large solid shapes (date block, elapsed daylight, today's rail pins) and the ink type takes on it (≥4.5:1, tested). They also set `--arc-night` / `--arc-day` / `--arc-golden` / `--arc-outline`, which `DayArcWidget` reads with fallbacks to the regular tokens, so the ribbon stays solid-coloured on the panel without changing its look elsewhere.

### Font themes

Named typography presets, orthogonal to color themes. Each preset bundles a font family (self-hosted `@fontsource-variable` packages), four semantic weights (`--weight-regular/medium/semibold/bold` — Tailwind's `font-medium/semibold/bold` resolve to these vars via the `@theme` block in `app.css`), and a size multiplier (`--font-scale`, folded into every `--font-*`/`--icon-*` clamp in `app.css`).

Defined in two places (both must be updated together):

1. `src/fonts.css` — `@fontsource` imports plus one `[data-font="id"] { ... }` block per preset (source of truth for stacks, weights, scale). Selectors stay bare `[data-font]` — `FontThemePicker.svelte` sets `data-font` on its preview buttons so the same blocks style the previews.
2. `src/lib/constants/fontThemes.ts` — `fontThemes` array (picker metadata only: `id`, `label`, `tag`)

Applied as `data-font` on `<html>` by `FontThemeStore.svelte.ts` (localStorage key `hearth-font`).

Separately, a user size slider in Settings (`FontSizeSlider.svelte`) sets `--font-user-scale` (0.9–1.3) as an inline style on `<html>` via `FontSizeStore.svelte.ts` (localStorage key `hearth-font-size`). `app.css` composes both into `--scale: calc(var(--font-scale) * var(--font-user-scale))`, which every `--font-*`/`--icon-*` clamp multiplies by.

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
| `display`  | 8090 | Partial     | Shared e-paper frame settings and the dithered frame preview (via the `renderer` sidecar); the panel endpoint is planned (`HARDWARE.md`) |
| `trains`   | 8091 | Implemented | Transitland departures for subscribed stops, cached for 45 seconds   |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `<service>.db`). The SQLite dependency (`Microsoft.Data.Sqlite`) is isolated to the `Data` shared library — individual service projects depend only on the `Data.Abstractions` interface, keeping them portable and testable.

## Shared Libraries

Three shared projects live under `src/` and are referenced by service projects via `ProjectReference`. They are not deployed independently — they compile into each service that uses them.

| Project             | Responsibility                                                                                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Data.Abstractions` | `IDatabase` interface and `DbCommandExtensions`; depends only on `System.Data.Common` (BCL) — no SQLite or other NuGet packages |
| `Data`              | `Database` — the concrete SQLite implementation of `IDatabase` — plus `AddSqliteDatabase(key, defaultDbFileName)`, an `IServiceCollection` extension that reads `DB_PATH` and registers the keyed singleton, and `SqliteDistributedCache` / `AddSqliteDistributedCache(key)` — an `IDistributedCache` over that same database, used as FusionCache's L2. The only project that references `Microsoft.Data.Sqlite`. |
| `ServiceDefaults`   | ASP.NET Core + FusionCache (no SQLite) library referenced by **all 10** services, including the 1 with no database (Almanac). Provides `AddHearthWebDefaults()` (CORS `AllowAnyOrigin/Method/Header` + snake_case JSON), the shared `HearthJson.SnakeCaseLower` options instance, `ConfigRequirement` (`RequireOrFail` / `WarnIfMissing`) for validating required env vars, `AddHearthDataProtection(appName)` for services that need to encrypt values at rest (keys persisted to `<DB_PATH dir>/keys`), and `AddHearthCache()` (FusionCache — see below). |

Service projects follow this pattern:

- Reference `Data.Abstractions` for the `IDatabase` type used in stores and handlers
- Reference `Data` for the 9 SQLite-backed services, calling `services.AddSqliteDatabase("key", "service.db")` once in `ServiceCollectionExtensions.cs`
- Reference `ServiceDefaults` and call `services.AddHearthWebDefaults()` once in `ServiceCollectionExtensions.cs`
- Never reference `Microsoft.Data.Sqlite` directly

JSON responses use `JsonNamingPolicy.SnakeCaseLower` (via `HearthJson.SnakeCaseLower` / `AddHearthWebDefaults()`) so property names match frontend conventions (e.g. `created_at`, `due_date`).

### Upstream response caching (FusionCache)

Services that proxy a third-party API with a simple "fetch, cache, serve" shape — Weather, Birds, Trains, Quote — cache through [FusionCache](https://github.com/ZiggyCreatures/FusionCache) (`IFusionCache.GetOrSetAsync`) instead of hand-written cache tables. `ServiceDefaults.AddHearthCache()` registers it with:

- **Fail-safe** on: when a refresh fails, the last good value keeps being served (up to the entry's `FailSafeMaxDuration`) instead of an error. Endpoints only return `502` when nothing was ever cached.
- **SQLite L2** when the service registered one via `AddSqliteDistributedCache(key)` (a `distributed_cache` table in the service's own `DB_PATH` file), so cached values survive container restarts. Without one (Quote), the cache is memory-only.
- `HearthJson.SnakeCaseLower` serialization for the L2 payloads.

Each service sets its own `FusionCacheEntryOptions` (duration + fail-safe window) next to its endpoint. RSS and Calendar keep their own relational SQLite caches (articles per feed, items per provider); Photos keeps its in-memory `IMemoryCache` batch.

### Required env var validation

Endpoints that depend on config (API keys, coordinates, OAuth secrets) validate with `ConfigRequirement`:

- `config.RequireOrFail(logger, respond, "VAR1", "VAR2")` — logs the missing vars, and if any are missing, returns the `IResult` built by the caller's `respond` factory (each service keeps its own response shape — e.g. Weather/Birds/Spotify/Trains return `Results.Json(new { error = ... }, statusCode: 503)`, Calendar returns `Results.Problem(...)`)
- `config.WarnIfMissing(logger, message, "VAR1", "VAR2")` — logs only, never fails the request; used by Almanac, where missing coordinates degrade the response (`daylight: null`) rather than failing it

## Tasks Service

The `tasks` service handles full CRUD for household tasks with optional due dates, times, recurrence, and countdown events.

### Endpoints

| Method   | Path          | Description                                                                                 |
| -------- | ------------- | --------------------------------------------------------------------------------------------|
| `GET`    | `/tasks`      | List tasks: all done tasks + undone tasks due within 1 year + undone tasks with no due date |
| `POST`   | `/tasks`      | Create a task; pre-generates all recurring instances up to 1 year ahead                     |
| `PUT`    | `/tasks/{id}` | Update done status, title, due date, due time, description, or assignee                     |
| `DELETE` | `/tasks/{id}` | Delete a task; `?series=true` deletes all instances of the recurring series                 |

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

**Deleting a series:** `DELETE /tasks/{id}?series=true` looks up the `series_id` of the given task and deletes all rows sharing that `series_id`.

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

**Algorithm (`frontend/src/lib/constants/moonphase.ts`):**

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

Both endpoints return `503` with `{ "error": "location not configured" }` if `LATITUDE` or `LONGITUDE` are missing, and log a `LogError` pointing to `.env.example`.

The forecast endpoint returns a `ForecastDay[]` where each day includes `sunrise` and `sunset` as ISO strings. `WeatherWidget.svelte` displays today's sunrise/sunset from `forecast[0]`.

`/weather/current` also returns nullable `uv_index` (from the same forecast call) and `us_aqi` (US Air Quality Index, fetched in parallel from Open-Meteo's separate keyless endpoint at `air-quality-api.open-meteo.com`). An air-quality fetch failure degrades to `null` rather than failing the request; both values are cached in the same snapshot under the same 30-minute TTL. `WeatherWidget.svelte` hides the UV/AQI metrics when they are `null`.

### Environment variables

| Variable                | Required | Description                                        |
| ------------------------ | -------- | ---------------------------------------------------|
| `LATITUDE`              | Yes      | Decimal latitude (e.g. `40.7128`)                  |
| `LONGITUDE`             | Yes      | Decimal longitude (e.g. `-74.0060`)                |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `weather.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8082` in Docker                            |

Place these in `src/Weather/.env`. Docker Compose loads the file via `env_file`; for local `dotnet run`, `DotNetEnv` loads the same file before `CreateBuilder`.

## Spotify Service

The `spotify` service handles OAuth 2.0 authorization with Spotify and exposes now-playing data. It stores a single token row in SQLite — only one Spotify account is linked at a time. Tokens are encrypted at rest (via `ServiceDefaults.AddHearthDataProtection`) before being persisted, and auto-refreshed on each `now-playing` request via `AuthorizationCodeAuthenticator`. A row that fails to decrypt (e.g. written before encryption was introduced) is treated as absent, forcing re-auth.

### Endpoints

| Method   | Path                   | Description                                                                              |
| -------- | ----------------------- | -------------------------------------------------------------------------------------------|
| `GET`    | `/spotify/auth`        | Begins OAuth flow — redirects the browser to Spotify's authorization page               |
| `GET`    | `/spotify/callback`    | OAuth callback — exchanges the code for tokens, saves them, redirects to `FRONTEND_URL`  |
| `GET`    | `/spotify/now-playing` | Returns the current track, or 204 if nothing is playing, or 401 if unauthenticated       |
| `GET`    | `/spotify/status`      | Returns `{ authenticated: bool }`                                                        |
| `DELETE` | `/spotify/auth`        | Clears the stored tokens, effectively disconnecting Spotify                             |

### Environment variables

Stored in `src/Spotify/.env` (loaded by `DotNetEnv`; also referenced via `env_file` in `docker-compose.yml`):

| Variable                 | Required | Description                                                                                                                                        |
| ------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------|
| `SPOTIFY_CLIENT_ID`      | Yes      | Spotify app client ID                                                                                                                              |
| `SPOTIFY_CLIENT_SECRET`  | Yes      | Spotify app client secret                                                                                                                          |
| `SPOTIFY_REDIRECT_URI`   | Yes      | Must match a URI registered in the Spotify app dashboard (e.g. `http://127.0.0.1:8083/spotify/callback`)                                           |
| `FRONTEND_URL`           | No       | URL to redirect to after OAuth completes; defaults to `/` on the service itself — set from `FRONTEND_URL` in the root `.env` (default `http://localhost`) in Docker |

### Frontend integration

`NowPlaying.svelte` polls `/spotify/now-playing` every 5 seconds via `SpotifyStore.ts`. The store value drives three UI states:

- `undefined` — 401 response → shows "Connect Spotify" link pointing to `/spotify/auth`
- `null` — 204 response → shows "Nothing playing · Disconnect" button
- `NowPlaying` — 200 response → shows track card with album art, progress bar, and a hover-revealed disconnect button

Clicking disconnect calls `DELETE /spotify/auth` then immediately re-polls, which returns 401 and flips the store back to `undefined`.

## Photos Service

The `photos` service fetches portrait or landscape photos from the [Unsplash API](https://unsplash.com/developers) and caches a batch of 20 in SQLite for 24 hours. The frontend `/ambient` route rotates through these photos at a user-configured cadence.

### Endpoints

| Method   | Path                    | Description                                                                                  |
| -------- | ------------------------ | ----------------------------------------------------------------------------------------------|
| `GET`    | `/photos/random`        | Returns one random `PhotoResponse` from cache; refetches if cache is stale or query changed  |
| `GET`    | `/photos/sources`       | Returns the list of available photo source names                                             |
| `GET`    | `/photos/uploads`       | Returns the list of user-uploaded photos                                                      |
| `POST`   | `/photos/uploads`       | Uploads a photo (multipart/form-data; max 200 MB)                                            |
| `DELETE` | `/photos/uploads/{id}`  | Deletes a user-uploaded photo by ID                                                           |

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

Stored in `src/Photos/.env`:

| Variable                | Required | Description                                       |
| ------------------------ | -------- | ---------------------------------------------------|
| `UNSPLASH_ACCESS_KEY`   | Yes      | Unsplash API access key (free tier: 50 req/hr)    |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `photos.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8084` in Docker                            |

### Frontend integration

Ambient photos are chosen once for every display: the **photo source** (Unsplash, your uploads, or both) and **categories** live in the shared frame settings (`FrameSettingsStore` → Display service), set in Settings → Ambient. Both the frame's Ambient mode and the browser `/ambient` slideshow use them through `lib/photos.ts` (`resolveSource` — "both" picks an upload half the time when there are any; `photoQuery` — categories joined into the `query` param, `nature` when none; `uploadToPhoto`).

`/ambient` (`routes/ambient/+page.svelte`) loads the shared settings, then fetches a new photo on mount and on each cadence interval (shuffling through uploads for "My photos"). Its playback stays per browser in `SettingsStore`:

- **`cadenceSeconds`** / **`nightCadenceSeconds`** — interval between photo advances (2m … 2hr), optionally slower 10pm–6am
- **`ambientMotion`** — slow Ken Burns pan/zoom

The Unsplash photographer credit is always shown (Unsplash's API guidelines require it); an upload shows its caption instead. The frame renders one photo per refresh via `pickPhoto`; its rotation schedule will belong to the Display service.

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

The `rss` service fetches articles from one or more user-configured RSS/Atom feed URLs and caches them in SQLite to avoid redundant fetches.

### Endpoints

| Method | Path            | Description                                                                                |
| ------ | ---------------- | ----------------------------------------------------------------------------------------------|
| `GET`  | `/rss/articles` | Accepts one or more `url` query params and a `count` (default 10); returns a `RssFeedGroup[]` sorted by publish date |

Every supplied `url` is validated by `FeedUrlValidator` before any fetch (SSRF protection): only absolute `http`/`https` URLs are accepted, and the host — after DNS resolution when it is not an IP literal — must resolve exclusively to publicly routable addresses. Loopback, RFC 1918 private, link-local (including the `169.254.169.254` cloud metadata IP), CGNAT, multicast, and the IPv6 equivalents (`::1`, `fc00::/7`, `fe80::/10`, IPv4-mapped) are rejected. Any invalid or disallowed URL causes the endpoint to return `400 Bad Request` with an `error` message.

If a feed's cache is stale (older than 30 minutes) or empty, the service re-fetches from the upstream URL before responding. On fetch failure, the endpoint returns whatever is in the cache (possibly empty).

### Caching

| Field  | Value                                            |
| ------ | --------------------------------------------------|
| TTL    | 30 minutes per feed URL                          |
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
| `GET`    | `/calendar/google/callback`                | Validate state, exchange code for tokens, redirect to frontend           |
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

Stored in `src/Calendar/.env`.

### Frontend integration

`Calendar.svelte`, `DayOverflowModal.svelte`, and `UpcomingTasksWidget.svelte` consume `CalendarStore.svelte.ts`, which loads Google connection status and items on layout mount.

## Birds Service

The `birds` service fetches recent and notable bird observations near the configured coordinates from the eBird API v2, merges them (one sighting per species, most recent wins, notable species flagged), and caches the result via FusionCache (SQLite L2) for 60 minutes, falling back to the last good sightings for up to 24 hours if eBird is down. Distance from home is computed with the haversine formula and served in miles.

### Endpoints

| Method | Path            | Description                                                                        |
| ------ | ---------------- | -------------------------------------------------------------------------------------|
| `GET`  | `/birds/recent` | Returns `BirdSighting[]`; `503` when env vars missing, `502` when eBird is unreachable and nothing is cached |

### Environment variables

| Variable            | Required | Description                                     |
| --------------------- | -------- | ---------------------------------------------------|
| `EBIRD_API_KEY`      | Yes      | Free key from https://ebird.org/api/keygen        |
| `LATITUDE`           | Yes      | Decimal latitude                                  |
| `LONGITUDE`          | Yes      | Decimal longitude                                 |
| `BIRDS_RADIUS_KM`    | No       | Search radius in km (default: 15)                 |
| `DB_PATH`            | No       | Path to SQLite cache file (default: `birds.db`)   |
| `ASPNETCORE_HTTP_PORTS` | —     | Set to `8088` in Docker                            |

Stored in `src/Birds/.env`.

### Frontend integration

`BirdsWidget.svelte` loads sightings on mount via `BirdsStore.svelte.ts` and refreshes hourly; a failed refresh keeps the last sightings on screen.

## Almanac Service

The `almanac` service computes seasonal facts entirely locally — no external API, no SQLite (it's stateless, so it has no `Data`/`Data.Abstractions` reference and no Docker volume). The response always contains the pinned **season** section plus at most **two rotating slots**, filled in priority order (daylight → timely frost → note); unfilled or bumped sections are `null`:

- **Season** (always present) — name, Early/Mid/Late label, day-of-season, progress, and countdown to the next equinox/solstice, from a hardcoded table of solstice/equinox UTC instants (2024–2040) in `SeasonCalculator`. Hemisphere derived from the sign of `LATITUDE` (northern when unset).
- **Daylight** — trend (min/day gained or lost over the past week), drift vs. the most recent solstice, and the single next wall-clock milestone ("Last 8 pm sunset · Aug 13") from the NOAA sunrise/sunset algorithm in `SolarCalculator`. `null` when coordinates are unset.
- **Frost** — countdown to the next of the user's typical first/last frost dates. Only claims a slot when ≤ 42 days away (bumping the note); `null` when unset or not yet timely.
- **Note** — curated phenology/in-season sentence keyed by half-month (`PhenologyData`, temperate Northern Hemisphere; `null` for southern installs).

Unlike Weather, missing coordinates do **not** produce a `503` — the endpoint logs an error at startup and returns `200` with `daylight: null`, because season and note are date-only.

### Endpoints

| Method | Path        | Description                                                   |
| ------ | ------------ | -----------------------------------------------------------------|
| `GET`  | `/almanac`  | Returns `{ season, daylight \| null, frost \| null, note \| null }` |

### Environment variables (all optional)

| Variable                | Description                                                  |
| ------------------------ | ----------------------------------------------------------------|
| `LATITUDE`              | Decimal latitude                                              |
| `LONGITUDE`             | Decimal longitude                                              |
| `TZ`                    | IANA zone for wall-clock milestones; defaults to system zone  |
| `FIRST_FROST` / `LAST_FROST` | `MM-DD`                                                  |
| `ASPNETCORE_HTTP_PORTS` | Set to `8089` in Docker                                        |

Stored in `src/Almanac/.env`.

## Display Service

`src/Display` (port 8090) holds the e-paper frame's shared settings — the frame is one physical display, so its look is a single source of truth rather than a per-browser preference. It also serves the frame dithered to the panel's six inks, captured and dithered by the `renderer` sidecar (below); the panel endpoint, render schedule and button/presence endpoints are still planned (`HARDWARE.md` step 2).

| Endpoint                 | Description |
| ------------------------ | ----------- |
| `GET /display/settings`  | The saved `FrameSettings` (`theme`, `font`, `orientation`, `left`, `right`, `split`, `ambient_position`, `photo_source`, `photo_categories`, `width`, `height`), or `204` if nothing has been saved — clients then use their defaults |
| `PUT /display/settings`  | Replaces them. `400` unless orientation is `landscape`/`portrait`, each column has at most 3 widgets with none repeated, `split` is 30–70 `ambient_position` is one of the six positions (omitted → `bottom-left`), and `width`/`height` (the panel's native size; omitted → 1600×1200) are 200–4000 with width ≥ height |
| `GET /display/preview`   | `/frame`'s query params plus `look=screen\|paper` (and optional `kernel=`, an epdoptimize error-diffusion kernel) → the frame as a PNG in the six inks, upright at the panel size (`width`×`height`, swapped in portrait; default 1600×1200). Daily is ordered-dithered, Ambient error-diffused; `paper` repaints the inks as they look on the panel. `400` for an unknown `kernel`, `503` without `RENDERER_URL` or while the renderer is busy, `504` if the frame never got ready, `502` on any other render failure |

`FrameSettingsStore` keeps one row (`id = 1`) in `display.db` (`DB_PATH`). Theme, font and widget ids are the frontend's; `FrameSettingsStore.svelte.ts` drops ids it doesn't know when it reads them. On first load with nothing saved, the frontend uploads any frame settings an older `localStorage` snapshot held (parked under `hearth-frame-legacy` by `SettingsStore`), once.

| Variable                | Required | Description |
| ----------------------- | -------- | ----------- |
| `DB_PATH`               | No       | Path to the SQLite file (default: `display.db`) |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8090` in Docker |
| `RENDERER_URL`          | For preview | The renderer sidecar (`http://renderer:3000` in Compose) |

### Renderer sidecar

`renderer/` is a small Node container (not a .NET service — Playwright needs Node and Chromium's system libraries, which the chiseled image lacks) built on the official multi-arch Playwright image. `GET /render?path=/frame?…&width=&height=&dither=ordered|errorDiffusion|none&kernel=&look=screen|paper` loads the frame from `FRAME_BASE_URL` (`http://caddy` — Caddy's `:80` site serves the frontend and the API routes the page calls), waits for `[data-ready="true"]` (40 s for load and ready together, then `504`; an error page fails at once with `502`), screenshots it (with subpixel text antialiasing off, so glyph edges don't dither into stray inks) and dithers it (`dither.mjs`, see `HARDWARE.md` step 2 for why each method). The browser draws in the root `.env`'s `TZ` (IANA zone; UTC when unset or invalid, with an error logged), since the frame's date and clock come from the browser. One render runs at a time with at most one waiting (`503` beyond that), and a render whose client has disconnected is dropped. An unknown `kernel` is a `400` (epdoptimize would otherwise quietly use Floyd–Steinberg). It only accepts `/frame` paths and has no Caddy route. Tests: `cd renderer && npm test`.

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

Stored in `src/Trains/.env`.

### Frontend integration

`TrainsStore.svelte.ts` loads departures for every stop key in `settings.trainStops` via `api.trains.departures()`, which builds repeated `?stop=` query params and returns `null` on any non-OK response (the store then keeps the last good board). The widget refreshes every 60 seconds. `TrainsWidget.svelte` groups departures by line, shows up to 3 upcoming times per line (e.g. "5, 20, 38 min"), clusters bus vs. train/rail lines (whichever has the soonest departure first), and applies each stop's `lineFilter`. `TrainStopSettings.svelte` is the Settings-page UI for subscribing to stops (`subscribeToTrainStop` / `unsubscribeFromTrainStop`) and configuring per-line filters.

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
/                   →  frontend:3000
```

The routes live in a `(hearth)` snippet that two sites import:

- **`:80`** serves plain HTTP on any hostname. `auto_https disable_redirects` stops Caddy from redirecting the HTTPS hostname to HTTPS, so LAN clients like the frame keep working over HTTP.
- **`{$HEARTH_HOST:localhost}`** serves HTTPS with `tls internal`, using a certificate from Caddy's own local CA. `HEARTH_HOST` comes from the root `.env`. Browsers warn until that CA is trusted. Its root certificate is at `/data/caddy/pki/authorities/local/root.crt` in the `caddy-data` volume.

The Caddy config is the same in development and production — the only difference is that `frontend:3000` points to the Vite dev server in dev and the Node production server in prod.

## Infrastructure

**Docker Compose** orchestrates all services. The compose setup is split into two files:

| File                          | Purpose                                                                                                       |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------|
| `docker-compose.yml`          | Production: multi-stage builds, static frontend bundle                                                       |
| `docker-compose.override.yml` | Development: auto-merged by Compose; swaps the frontend for the Vite dev server and configures file watching |

### Images

- **Backend:** one shared `src/Dockerfile` builds every service, selected by the `SERVICE` build arg set in each service's `build.args` in `docker-compose.yml`. The SDK stage runs on the build machine's own arch (`--platform=$BUILDPLATFORM`) and cross-publishes for the target architecture only (`-a $TARGETARCH`), so arm64 images build at native speed on amd64 CI onto `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`: no shell or package manager, and `-extra` adds the ICU + tzdata that `TimeZoneInfo` needs. The apphost is renamed to `/app/service` so a single exec-form `ENTRYPOINT` works without a shell.
- **Frontend:** `frontend/Dockerfile` (adapter-node build on `$BUILDPLATFORM` — its output is plain JS — then `npm ci --omit=dev` from the lockfile as the target arch) and `frontend/Dockerfile.dev` (Vite dev server).
- **Non-root:** backend containers run as the image's `app` user (UID 1654), the frontend as `node`. Each backend image creates `/data` owned by UID 1654, so new named volumes inherit that ownership. Volumes created before the switch to non-root (October 2026) are root-owned and need a one-time fix, or SQLite fails with "unable to open database file". Run this per volume with the stack stopped:

  ```bash
  docker run --rm -v hearth_tasks-data:/data alpine chown -R 1654:1654 /data
  ```

- **`.dockerignore`:** `src/.dockerignore` keeps `.env` secrets and local `*.db` files out of build contexts; `frontend/.dockerignore` keeps host `node_modules`/build output out, since `COPY . .` would otherwise merge macOS native binaries over the container's.
- **Runtime:** every service has `restart: unless-stopped` and rotated `json-file` logs (3 × 10 MB), via the `x-defaults` anchor. The frontend and Caddy have healthchecks (busybox `wget`), and Caddy waits for the frontend to be healthy. The .NET services have no healthcheck: chiseled images have no probe tool, and starting a .NET probe process per service each interval costs too much on a low-power host. Caddy retries upstreams, and the restart policy covers crashes.

### Development workflow

```bash
docker compose watch
```

`docker-compose.override.yml` swaps the frontend for `frontend/Dockerfile.dev` (Vite dev server). File changes under `frontend/src/` and `frontend/static/` sync into the container via HMR; changes to `package.json`, `svelte.config.js`, or `vite.config.ts` trigger a rebuild. HMR WebSocket goes through Caddy on port 80 via `HMR_CLIENT_PORT=80`.

### Production build

```bash
docker compose -f docker-compose.yml up --build
```

Every built service in `docker-compose.yml` also has an `image:` of `${HEARTH_IMAGE_PREFIX:-hearth}-<service>:${HEARTH_TAG:-latest}`. With no root `.env` that is just the name Compose would pick anyway, so local builds are unchanged.

### Deploying to a host

Deployed hosts never build: they pull images that CI publishes.

1. **CI publishes.** The `publish-images` job in `.github/workflows/ci.yml` runs on pushes to `main` once the backend and frontend jobs pass. It builds every service for `linux/amd64` and `linux/arm64` and pushes `ghcr.io/<owner>/hearth-<service>` tagged `latest` and `sha-<short>`. GHCR packages start out private.
2. **The host pulls.** Use a 64-bit OS, since only amd64 and arm64 images are published. Install Docker and add your user to the `docker` group (`sudo usermod -aG docker <user>`). Clone the repo as that user (to `/opt/hearth`, or adjust the path in `deploy/hearth-update.service`) and set `User=` in that unit to the same user, copy `.env.example` to `.env` and fill it in, and copy each `src/<Service>/.env` over by hand. Secrets never enter an image (`.dockerignore` excludes `.env`) or git. If the repo or packages are private, run `docker login ghcr.io` once as that user with a personal access token that has `read:packages`, and give `git` a read-only deploy key.
3. **It keeps itself current.** `deploy/update.sh` runs `git pull --ff-only`, then `docker compose -f docker-compose.yml pull`, then `up -d --no-build --remove-orphans`, and prunes dangling images. Compose only recreates containers whose image or config changed. Caddy is restarted only when the pull changed the `Caddyfile`, because `up` doesn't watch bind mounts. Install the timer to run it every 5 minutes:

   ```bash
   sudo cp deploy/hearth-update.service deploy/hearth-update.timer /etc/systemd/system/
   sudo systemctl enable --now hearth-update.timer
   ```

   To roll back, set `HEARTH_TAG=sha-<short>` in `.env` and run `deploy/update.sh`.

`FRONTEND_URL` (where Spotify and Calendar redirect after OAuth) is read from the root `.env` and defaults to `http://localhost`. On a host, also update the redirect URIs in those services' `.env` files and register each one with its provider:

- **Spotify** accepts `https://<HEARTH_HOST>/spotify/callback`, served by Caddy's HTTPS site.
- **Google** rejects any host without a public top-level domain, so `.local` fails even over HTTPS. Without a real domain, set `GOOGLE_REDIRECT_URI=http://localhost:8087/calendar/google/callback` on the host and register that URI. Then sign in once from a laptop with a tunnel open: `ssh -N -L 8087:localhost:80 <user>@<host>`, then open `http://localhost:8087/calendar/google/auth`. The refresh token is stored encrypted in the calendar volume, so the tunnel isn't needed again unless you disconnect.

### Project site (GitHub Pages)

`site/` is a static showcase (`index.html` tour, `build.html` parts/setup/status, one shared `assets/style.css`, self-hosted Fraunces + Inter woff2) with no build step. `.github/workflows/pages.yml` uploads the folder as-is and deploys it to GitHub Pages on pushes to `main` that touch `site/**`; CI's `paths-ignore` skips the .NET/frontend jobs for site-only changes. It's served at the custom domain `hearth.lucas-vertrees.me` (a CNAME in the domain's Vercel DNS pointing to `lrv1668-gif.github.io`; the domain is set in the repo's Pages settings, so no `CNAME` file is needed with an Actions deploy). Keep every link and asset path relative so the site also works under the `lrv1668-gif.github.io/Hearth/` fallback path; only the absolute `og:image` URLs name the domain. Screenshots in `site/assets/img/` are captured from a running stack with personal endpoints (tasks, calendar, birds, trains, RSS, Spotify, uploaded photos) replaced by demo data — never commit captures of real household data. Keep the site's status table in `build.html` in step with what's actually built.
