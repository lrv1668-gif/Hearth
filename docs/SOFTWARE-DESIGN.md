# Software Design

## Overview

Hearth is a calm, self-hosted home dashboard. The architecture is a set of small, independently deployable services behind a single reverse proxy, consumed by a SvelteKit frontend.

## Architecture Overview

```
Browser
  └── Caddy :80
        ├── /tasks*    → Tasks      :8081
        ├── /weather*  → Weather    :8082
        ├── /spotify*  → Spotify    :8083
        ├── /photos*   → Photos     :8084
        ├── /rss*      → Rss        :8085
        ├── /quote*    → Quote      :8086
        ├── /calendar* → Calendar   :8087
        ├── /birds*    → Birds      :8088
        ├── /almanac*  → Almanac    :8089
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

#### Kiosk mode

Kiosk mode can be activated in two ways: toggle **Settings → Frame → Kiosk Mode** (persisted to `localStorage`), or append `?kiosk=1` to the URL (useful for hardcoding the e-paper display URL). Either condition is sufficient. In kiosk mode:

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
| `settings`     | `SettingsStore.svelte.ts`   | `localStorage`  | Ambient cadence, photo categories, attribution flag, RSS feeds config, kiosk mode, subscribed train stops + per-stop line filters |
| `tasks`        | `TaskStore.svelte.ts`       | Server (SQLite) | Task list, CRUD operations                                            |
| `calendar`     | `CalendarStore.svelte.ts`   | Server (SQLite) | Calendar items (events + Google Tasks) and Google connection status   |
| `nowPlaying`   | `SpotifyStore.svelte.ts`    | Server (SQLite) | Spotify now-playing state; polled every 5 s by `DashboardFooter`     |
| `rssStore`     | `RssFeedStore.svelte.ts`    | Server (SQLite) | RSS article list, loading state                                       |
| `weatherStore` | `WeatherStore.svelte.ts`    | Server (SQLite) | Current conditions + 7-day forecast; loaded once per page visit      |
| `dailyQuote`   | `DailyQuoteStore.svelte.ts` | Server (SQLite) | Daily quote; loaded once per day                                      |
| `birds`        | `BirdsStore.svelte.ts`      | Server (SQLite) | Nearby bird sightings; loaded on widget mount                         |
| `trains`       | `TrainsStore.svelte.ts`     | Server (SQLite) | Departures for subscribed stops; loaded on widget mount               |
| `kioskStore`   | `KioskStore.svelte.ts`      | URL param       | True when `settings.kioskMode` is on or `?kiosk=1` is in the URL    |

### Mixed task/event lists

`api.ts` exports an `Item` discriminated union shared by `Calendar.svelte`, `DayOverflowModal.svelte`, and `UpcomingTasksWidget.svelte`:

```typescript
export type Item =
    | { kind: 'task';  data: Task }
    | { kind: 'event'; data: CalendarItem };
```

### Key utility functions (`utils.ts`)

- `formatTime(time)` — Converts `"HH:MM"` (24h) to `"4PM"` / `"4:30PM"` format
- `eventDateKey(event)` — Returns `"YYYY-MM-DD"` in local time; slices all-day strings directly to avoid the UTC midnight shift `new Date("YYYY-MM-DD")` causes in negative UTC offset timezones
- `stripHtml(html)` — Strips HTML tags (replacing with a space) and collapses whitespace, for plain-text previews of rich content (e.g. calendar event descriptions)
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
| `trains`   | 8091 | Implemented | Transitland departures for subscribed stops, cached for 45 seconds   |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `<service>.db`). The SQLite dependency (`Microsoft.Data.Sqlite`) is isolated to the `Data` shared library — individual service projects depend only on the `Data.Abstractions` interface, keeping them portable and testable.

## Shared Libraries

Three shared projects live under `src/` and are referenced by service projects via `ProjectReference`. They are not deployed independently — they compile into each service that uses them.

| Project             | Responsibility                                                                                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Data.Abstractions` | `IDatabase` interface and `DbCommandExtensions`; depends only on `System.Data.Common` (BCL) — no SQLite or other NuGet packages |
| `Data`              | `Database` — the concrete SQLite implementation of `IDatabase` — plus `AddSqliteDatabase(key, defaultDbFileName)`, an `IServiceCollection` extension that reads `DB_PATH` and registers the keyed singleton. The only project that references `Microsoft.Data.Sqlite`. |
| `ServiceDefaults`   | Framework-only (no SQLite) library referenced by **all 10** services, including the 1 with no database (Almanac). Provides `AddHearthWebDefaults()` (CORS `AllowAnyOrigin/Method/Header` + snake_case JSON), the shared `HearthJson.SnakeCaseLower` options instance, `ConfigRequirement` (`RequireOrFail` / `WarnIfMissing`) for validating required env vars, and `AddHearthDataProtection(appName)` for services that need to encrypt values at rest (keys persisted to `<DB_PATH dir>/keys`). |

Service projects follow this pattern:

- Reference `Data.Abstractions` for the `IDatabase` type used in stores and handlers
- Reference `Data` for the 9 SQLite-backed services, calling `services.AddSqliteDatabase("key", "service.db")` once in `ServiceCollectionExtensions.cs`
- Reference `ServiceDefaults` and call `services.AddHearthWebDefaults()` once in `ServiceCollectionExtensions.cs`
- Never reference `Microsoft.Data.Sqlite` directly

JSON responses use `JsonNamingPolicy.SnakeCaseLower` (via `HearthJson.SnakeCaseLower` / `AddHearthWebDefaults()`) so property names match frontend conventions (e.g. `created_at`, `due_date`).

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

The `weather` service fetches current conditions and a 7-day forecast from [Open-Meteo](https://open-meteo.com/) (no API key required) and caches the result in SQLite to avoid redundant fetches.

### Endpoints

| Method | Path                | Description                                     |
| ------ | -------------------- | ------------------------------------------------|
| `GET`  | `/weather/current`  | Returns current conditions; uses cache if fresh |
| `GET`  | `/weather/forecast` | Returns 7-day forecast; uses cache if fresh     |

Both endpoints return `503` with `{ "error": "location not configured" }` if `LATITUDE` or `LONGITUDE` are missing, and log a `LogError` pointing to `.env.example`.

The forecast endpoint returns a `ForecastDay[]` where each day includes `sunrise` and `sunset` as ISO strings. `WeatherWidget.svelte` displays today's sunrise/sunset from `forecast[0]`.

`/weather/current` also returns nullable `uv_index` (from the same forecast call) and `us_aqi` (US Air Quality Index, fetched in parallel from Open-Meteo's separate keyless endpoint at `air-quality-api.open-meteo.com`). An air-quality fetch failure degrades to `null` rather than failing the request; both values are cached in `current_json` under the same 30-minute TTL. `WeatherWidget.svelte` hides the UV/AQI metrics when they are `null`.

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
| `FRONTEND_URL`           | No       | URL to redirect to after OAuth completes; defaults to `/` on the service itself — set to the Caddy entry point (e.g. `http://localhost`) in Docker |

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

`/ambient` (`routes/ambient/+page.svelte`) fetches a new photo from `/photos/random` on mount and then on each cadence interval. Settings are read from `SettingsStore`:

- **`cadenceSeconds`** — interval between photo advances (2m / 5m / 10m / 30m / 1hr / 2hr)
- **`photoCategories`** — array of topics joined as a comma-separated `query` param
- **`showAttribution`** — toggles the photographer credit bar at the bottom of the display

Photos transition with a 1.5-second crossfade. Clicking anywhere or pressing any key exits back to `/`.

## Quote Service

The `quote` service fetches the daily quote from the ZenQuotes API and caches it in SQLite until the next UTC day.

### Endpoints

| Method | Path     | Description                                                    |
| ------ | -------- | -----------------------------------------------------------------|
| `GET`  | `/quote` | Returns the current day's quote as `{ q: string, a: string }`  |

If the cached quote is from a prior UTC day (or absent), the service re-fetches before responding. On fetch failure, the endpoint returns the last cached quote; if no quote has ever been fetched, it returns 503.

### Caching

| Field  | Value                                          |
| ------ | ------------------------------------------------|
| TTL    | Until the next UTC midnight                     |
| Source | `https://zenquotes.io/api/today`               |

### Environment variables

| Variable                | Required | Description                                      |
| ------------------------ | -------- | ---------------------------------------------------|
| `DB_PATH`               | No       | Path to SQLite cache file (default: `quote.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8086` in Docker                            |

### Frontend integration

`DailyQuoteWidget.svelte` loads the quote on mount via `DailyQuoteStore.svelte.ts`.

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

`NewsFeedWidget.svelte` loads articles on mount via `RssFeedStore.svelte.ts`. Feed URLs and the article count are controlled by the settings persisted in `SettingsStore.svelte.ts`, configurable in Settings → RSS Feeds. Clicking an article opens the link in a new tab.

## Calendar Service

The `calendar` service integrates with Google Calendar and Google Tasks via OAuth 2.0, surfacing events (read-only) and tasks (completable via checkbox) for the next 14 days. It's built behind an `ICalendarProvider` interface so additional providers (Outlook, etc.) can be added without changing the aggregation endpoint.

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

The `birds` service fetches recent and notable bird observations near the configured coordinates from the eBird API v2, merges them (one sighting per species, most recent wins, notable species flagged), and caches the result in SQLite for 60 minutes. Distance from home is computed with the haversine formula and served in miles.

### Endpoints

| Method | Path            | Description                                                                        |
| ------ | ---------------- | -------------------------------------------------------------------------------------|
| `GET`  | `/birds/recent` | Returns `BirdSighting[]`; `503` when env vars missing, `502` when eBird is unreachable |

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

`BirdsWidget.svelte` loads sightings on mount via `BirdsStore.svelte.ts`.

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

## Trains Service

The `trains` service fetches upcoming departures for user-subscribed transit stops from the [Transitland v2 API](https://www.transit.land/documentation/how-to-use-transitland-apis) and caches each stop's result in SQLite with a 45-second TTL. Unlike the other external-API services, there's no location configuration — the user picks specific stops (Transitland Onestop IDs) to subscribe to from the Settings page.

### Endpoints

| Method | Path                 | Description                                                                        |
| ------ | --------------------- | -------------------------------------------------------------------------------------|
| `GET`  | `/trains/departures` | Accepts one or more repeated `stop` query params (Transitland Onestop IDs); returns `StopDepartures[]` |

Behavior:

- No `stop` params → returns `200` with `[]` immediately; no config check or fetch.
- `TRANSITLAND_API_KEY` missing → `503` with `{ "error": "trains not configured" }`, logged via `LogError`.
- Each requested stop is refetched from Transitland only if its cached row is stale (> 45s). A fetch failure for one stop is logged and that stop's group is served from stale cache (or omitted entirely if it was never successfully cached) — it never fails the whole request. **Unlike Birds, a Transitland outage never surfaces as a `502`** — the endpoint always returns `200` and degrades gracefully.
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
| Key          | `stop_key` (one row per subscribed stop; `INSERT OR REPLACE` upsert)         |
| Invalidation | None explicit — TTL expiry only; a failed refetch leaves the previous cached value in place |

`fetched_at` is parsed with `DateTimeStyles.RoundtripKind` to preserve UTC — a plain `DateTime.TryParse` would convert the stored `"...Z"` timestamp to local time and skew the staleness check by the machine's UTC offset.

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

`TrainsStore.svelte.ts` loads departures for every stop key in `settings.trainStops` via `fetchTrainDepartures()`, which builds repeated `?stop=` query params and returns `[]` on any non-OK response. `TrainsWidget.svelte` groups departures by line, shows up to 3 upcoming times per line (e.g. "5, 20, 38 min"), clusters bus vs. train/rail lines (whichever has the soonest departure first), and applies each stop's `lineFilter`. `TrainStopSettings.svelte` is the Settings-page UI for subscribing to stops (`subscribeToTrainStop` / `unsubscribeFromTrainStop`) and configuring per-line filters.

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
/trains/*           →  trains:8091
/                   →  frontend:3000
```

The Caddy config is the same in development and production — the only difference is that `frontend:3000` points to the Vite dev server in dev and the Node production server in prod.

## Infrastructure

**Docker Compose** orchestrates all services. The compose setup is split into two files:

| File                          | Purpose                                                                                                       |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------|
| `docker-compose.yml`          | Production: multi-stage builds, static frontend bundle                                                       |
| `docker-compose.override.yml` | Development: auto-merged by Compose; swaps the frontend for the Vite dev server and configures file watching |

### Development workflow

```bash
docker compose watch
```

`docker-compose.override.yml` swaps the frontend for `frontend/Dockerfile.dev` (Vite dev server). File changes under `frontend/src/` and `frontend/static/` sync into the container via HMR; changes to `package.json`, `svelte.config.js`, or `vite.config.ts` trigger a rebuild. HMR WebSocket goes through Caddy on port 80 via `HMR_CLIENT_PORT=80`.

### Production build

```bash
docker compose -f docker-compose.yml up --build
```
