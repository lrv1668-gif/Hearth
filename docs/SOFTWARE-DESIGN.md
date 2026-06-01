# Software Design

## Overview

Hearth is a calm, self-hosted home dashboard. The architecture is a set of small, independently deployable services behind a single reverse proxy, consumed by a SvelteKit frontend.

## Frontend

### SvelteKit 2 + Svelte 5 + Tailwind CSS v3 + Lucide Svelte

- Lightweight runtime, well-suited for an always-on ambient display
- Svelte 5 runes syntax (`$state`, `$effect`, `$props`, `$derived`) for fine-grained reactivity
- Tailwind v3 for utility-first styling with a muted, theme-switchable palette
- [Lucide Svelte](https://lucide.dev/guide/packages/lucide-svelte) for icons (`@lucide/svelte`)
- API calls are centralised in `frontend/src/lib/api.ts`; all paths are relative (e.g. `/tasks`)
- In development, Vite proxies `/tasks` → `$TASKS_URL`, `/spotify` → `$SPOTIFY_URL`, `/weather` → `$WEATHER_URL`, and `/photos` → `$PHOTOS_URL`; in production, Caddy handles the same routing

### Routes

| Route       | Description                                                                               |
| ----------- | ----------------------------------------------------------------------------------------- |
| `/`         | Schedule — configurable widget columns (tasks, weather, countdowns, moon phase, news, quote) |
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
| `settings`     | `SettingsStore.svelte.ts`   | `localStorage`  | Ambient cadence, photo categories, attribution flag, RSS feeds config, kiosk mode |
| `tasks`        | `TaskStore.svelte.ts`       | Server (SQLite) | Task list, CRUD operations                                            |
| `nowPlaying`   | `SpotifyStore.svelte.ts`    | Server (SQLite) | Spotify now-playing state; polled every 5 s by `DashboardFooter`     |
| `rssStore`     | `RssFeedStore.svelte.ts`    | Server (SQLite) | RSS article list, loading state                                       |
| `weatherStore` | `WeatherStore.svelte.ts`    | Server (SQLite) | Current conditions + 7-day forecast; loaded once per page visit      |
| `kioskStore`   | `KioskStore.svelte.ts`      | URL param       | True when `settings.kioskMode` is on or `?kiosk=1` is in the URL    |

### Themes

Fourteen built-in themes are applied via CSS custom properties on `[data-theme]`. The active theme is persisted in `localStorage` via `ThemeStore.svelte.ts` and selected from the `/settings` page.

| Theme        | Style                        |
| ------------ | ---------------------------- |
| `stone`      | Dark warm neutrals           |
| `linen`      | Warm cream light mode        |
| `forest`     | Dark mossy green             |
| `dusk`       | Deep indigo dark mode        |
| `ash`        | Pure monochrome dark         |
| `chalk`      | Pure monochrome light        |
| `terracotta` | Sandy earth tones light mode |
| `tide`       | Oceanic blue-greens          |
| `slate`      | Cool grey dark mode          |
| `blush`      | Warm pink light mode         |
| `frost`      | Crisp cool whites            |
| `smoke`      | Muted grey dark mode         |
| `sage`       | Soft green light mode        |
| `sky`        | Airy light blue              |

Each theme exposes the same semantic token set: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, and `color-scheme`. Components reference tokens only — never hardcoded colors.

Themes are defined in two places that must be kept in sync: `frontend/src/themes.css` (CSS variables) and `frontend/src/lib/constants/themes.ts` (switcher metadata).

### Typography Scale

A fluid type scale is defined in `app.css` using `clamp()`, exposed as `type-display`, `type-title`, `type-subtitle`, `type-body`, `type-label`, `type-caption` utility classes. Icon sizes follow the same pattern: `icon-lg`, `icon-md`, `icon-sm`, `icon-xs`. Components use these classes — never raw Tailwind `text-*` sizes.

## Backend Services

Each domain is a small, self-contained **ASP.NET Core 10 Minimal API** service backed by **SQLite**.

| Service   | Port | Status      | Responsibility                                             |
| --------- | ---- | ----------- | ---------------------------------------------------------- |
| `tasks`   | 8081 | Implemented | Task CRUD with due dates, recurrence, and countdown events |
| `weather` | 8082 | Implemented | Polls Open-Meteo, caches current + forecast                |
| `spotify` | 8083 | Implemented | Spotify OAuth + now-playing                                |
| `photos`  | 8084 | Implemented | Fetches Unsplash photos, caches batch for 24 hours         |
| `rss`     | 8085 | Implemented | Fetches RSS/Atom feed articles, caches for 30 minutes      |
| `quote`   | 8086 | Implemented | Fetches daily quote from ZenQuotes, caches until next day  |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `<service>.db`). The SQLite dependency (`Microsoft.Data.Sqlite`) is isolated to the `Data` shared library — individual service projects depend only on the `Data.Abstractions` interface, keeping them portable and testable.

## Shared Libraries

Two shared projects live under `services/` and are referenced by service projects via `ProjectReference`. They are not deployed independently — they compile into each service that uses them.

| Project             | Responsibility                                                                                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `Data.Abstractions` | `IDatabase` interface and `DbCommandExtensions`; depends only on `System.Data.Common` (BCL) — no SQLite or other NuGet packages |
| `Data`              | `Database` — the concrete SQLite implementation of `IDatabase`; the only project that references `Microsoft.Data.Sqlite`        |

Service projects follow this pattern:

- Reference `Data.Abstractions` for the `IDatabase` type used in stores and handlers
- Reference `Data` in `Program.cs` only, to register the concrete implementation: `AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath))`
- Never reference `Microsoft.Data.Sqlite` directly

JSON responses use `JsonNamingPolicy.SnakeCaseLower` so property names match frontend conventions (e.g. `created_at`, `due_date`).

## Tasks Service

The `tasks` service handles full CRUD for household tasks with optional due dates, times, recurrence, and countdown events.

### Endpoints

| Method   | Path          | Description                                                                                 |
| -------- | ------------- | ------------------------------------------------------------------------------------------- |
| `GET`    | `/tasks`      | List tasks: all done tasks + undone tasks due within 1 year + undone tasks with no due date |
| `POST`   | `/tasks`      | Create a task; pre-generates all recurring instances up to 1 year ahead                     |
| `PUT`    | `/tasks/{id}` | Update done status, title, due date, due time, description, or assignee                     |
| `DELETE` | `/tasks/{id}` | Delete a task; `?series=true` deletes all instances of the recurring series                 |

### Recurrence Model

Recurring tasks use a **pre-generation** approach: when a task with a recurrence rule is created, all instances up to one year ahead are inserted as individual rows at creation time. Each instance row stores the full recurrence definition and shares a `series_id` (equal to the `id` of the first instance in the series).

| Field                 | Type                             | Description                                                                             |
| --------------------- | -------------------------------- | --------------------------------------------------------------------------------------- |
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
| --------- | ----------------- | --------------------- |
| Daily     | `day`             | 1                     |
| Weekly    | `week`            | 1                     |
| Bi-weekly | `week`            | 2                     |
| Monthly   | `month`           | 1                     |
| Yearly    | `month`           | 12                    |

Weekly recurrences may additionally specify `recurrence_days` (e.g. `"Mon,Wed,Fri"`) to pin to specific weekdays.

### Countdown Events

Tasks with `is_countdown = 1` are one-off events tracked by time remaining rather than completion. They appear in a dedicated **Countdowns** widget on the schedule page showing the 5 nearest upcoming events sorted by days remaining, and are filtered out of the main upcoming-tasks list.

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
| ----------------------- | ---------- | -------------------------------- |
| `DB_PATH`               | `tasks.db` | Path to the SQLite database file |
| `ASPNETCORE_HTTP_PORTS` | —          | Set to `8081` in Docker          |

## Weather Service

The `weather` service fetches current conditions and a 7-day forecast from [Open-Meteo](https://open-meteo.com/) (no API key required) and caches the result in SQLite to avoid redundant fetches.

### Endpoints

| Method | Path                | Description                                     |
| ------ | ------------------- | ----------------------------------------------- |
| `GET`  | `/weather/current`  | Returns current conditions; uses cache if fresh |
| `GET`  | `/weather/forecast` | Returns 7-day forecast; uses cache if fresh     |

Both endpoints return `503` with `{ "error": "location not configured" }` if `LATITUDE` or `LONGITUDE` are missing, and log a `LogError` pointing to `.env.example`.

The forecast endpoint returns a `ForecastDay[]` where each day includes `sunrise` and `sunset` as ISO strings. `WeatherWidget.svelte` displays today's sunrise/sunset from `forecast[0]`.

### Environment variables

| Variable                | Required | Description                                       |
| ----------------------- | -------- | ------------------------------------------------- |
| `LATITUDE`              | Yes      | Decimal latitude (e.g. `40.7128`)                 |
| `LONGITUDE`             | Yes      | Decimal longitude (e.g. `-74.0060`)               |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `weather.db`) |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8082` in Docker                           |

Place these in `services/Weather/.env`. Docker Compose loads the file via `env_file`; for local `dotnet run`, `DotNetEnv` loads the same file before `CreateBuilder`.

## Spotify Service

The `spotify` service handles OAuth 2.0 authorization with Spotify and exposes now-playing data. It stores a single token row in SQLite — only one Spotify account is linked at a time.

### Endpoints

| Method   | Path                   | Description                                                                             |
| -------- | ---------------------- | --------------------------------------------------------------------------------------- |
| `GET`    | `/spotify/auth`        | Begins OAuth flow — redirects the browser to Spotify's authorization page               |
| `GET`    | `/spotify/callback`    | OAuth callback — exchanges the code for tokens, saves them, redirects to `FRONTEND_URL` |
| `GET`    | `/spotify/now-playing` | Returns the current track, or 204 if nothing is playing, or 401 if unauthenticated      |
| `GET`    | `/spotify/status`      | Returns `{ authenticated: bool }`                                                       |
| `DELETE` | `/spotify/auth`        | Clears the stored tokens, effectively disconnecting Spotify                             |

### Environment variables

Stored in `services/Spotify/.env` (loaded by `DotNetEnv`; also referenced via `env_file` in `docker-compose.yml`):

| Variable                | Required | Description                                                                                                                                        |
| ----------------------- | -------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SPOTIFY_CLIENT_ID`     | Yes      | Spotify app client ID                                                                                                                              |
| `SPOTIFY_CLIENT_SECRET` | Yes      | Spotify app client secret                                                                                                                          |
| `SPOTIFY_REDIRECT_URI`  | Yes      | Must match a URI registered in the Spotify app dashboard (e.g. `http://127.0.0.1:8083/spotify/callback`)                                           |
| `FRONTEND_URL`          | No       | URL to redirect to after OAuth completes; defaults to `/` on the service itself — set to the Caddy entry point (e.g. `http://localhost`) in Docker |

### Frontend integration

`NowPlaying.svelte` polls `/spotify/now-playing` every 5 seconds via `SpotifyStore.ts`. The store value drives three UI states:

- `undefined` — 401 response → shows "Connect Spotify" link pointing to `/spotify/auth`
- `null` — 204 response → shows "Nothing playing · Disconnect" button
- `NowPlaying` — 200 response → shows track card with album art, progress bar, and a hover-revealed disconnect button

Clicking disconnect calls `DELETE /spotify/auth` then immediately re-polls, which returns 401 and flips the store back to `undefined`.

## Photos Service

The `photos` service fetches portrait or landscape photos from the [Unsplash API](https://unsplash.com/developers) and caches a batch of 20 in SQLite for 24 hours. The frontend `/ambient` route rotates through these photos at a user-configured cadence.

### Endpoints

| Method   | Path                    | Description                                                                                 |
| -------- | ----------------------- | ------------------------------------------------------------------------------------------- |
| `GET`    | `/photos/random`        | Returns one random `PhotoResponse` from cache; refetches if cache is stale or query changed |
| `GET`    | `/photos/sources`       | Returns the list of available photo source names                                            |
| `GET`    | `/photos/uploads`       | Returns the list of user-uploaded photos                                                    |
| `POST`   | `/photos/uploads`       | Uploads a photo (multipart/form-data; max 200 MB)                                          |
| `DELETE` | `/photos/uploads/{id}`  | Deletes a user-uploaded photo by ID                                                         |

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

| Field      | Value                                                            |
| ---------- | ---------------------------------------------------------------- |
| Batch size | 20 photos per fetch                                              |
| TTL        | 24 hours                                                         |
| Cache bust | Query string changes (i.e. user changes photo categories)        |
| Fallback   | Returns 502 if Unsplash is unreachable and no valid cache exists |

### Environment variables

Stored in `services/Photos/.env`:

| Variable                | Required | Description                                      |
| ----------------------- | -------- | ------------------------------------------------ |
| `UNSPLASH_ACCESS_KEY`   | Yes      | Unsplash API access key (free tier: 50 req/hr)   |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `photos.db`) |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8084` in Docker                          |

### Frontend integration

`/ambient` (`routes/ambient/+page.svelte`) fetches a new photo from `/photos/random` on mount and then on each cadence interval. Settings are read from `SettingsStore`:

- **`cadenceSeconds`** — interval between photo advances (2m / 5m / 10m / 30m / 1hr / 2hr)
- **`photoCategories`** — array of topics joined as a comma-separated `query` param
- **`showAttribution`** — toggles the photographer credit bar at the bottom of the display

Photos transition with a 1.5-second crossfade. Clicking anywhere or pressing any key exits back to `/`.

## Quote Service

The `quote` service fetches the daily quote from the ZenQuotes API and caches it in SQLite until the next UTC day.

### Endpoints

| Method | Path     | Description                                                                 |
| ------ | -------- | --------------------------------------------------------------------------- |
| `GET`  | `/quote` | Returns the current day's quote as `{ q: string, a: string }` |

If the cached quote is from a prior UTC day (or absent), the service re-fetches before responding. On fetch failure, the endpoint returns the last cached quote; if no quote has ever been fetched, it returns 503.

### Caching

| Field  | Value                                         |
| ------ | --------------------------------------------- |
| TTL    | Until the next UTC midnight                   |
| Source | `https://zenquotes.io/api/today`              |

### Environment variables

| Variable                | Required | Description                                     |
| ----------------------- | -------- | ----------------------------------------------- |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `quote.db`) |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8086` in Docker                         |

### Frontend integration

`DailyQuoteWidget.svelte` loads the quote on mount via `DailyQuoteStore.svelte.ts`.

## RSS Service

The `rss` service fetches articles from one or more user-configured RSS/Atom feed URLs and caches them in SQLite to avoid redundant fetches.

### Endpoints

| Method | Path            | Description                                                                               |
| ------ | --------------- | ----------------------------------------------------------------------------------------- |
| `GET`  | `/rss/articles` | Accepts one or more `url` query params and a `count` (default 10); returns a `RssFeedGroup[]` sorted by publish date |

If a feed's cache is stale (older than 30 minutes) or empty, the service re-fetches from the upstream URL before responding. On fetch failure, the endpoint returns whatever is in the cache (possibly empty).

### Caching

| Field  | Value                                          |
| ------ | ---------------------------------------------- |
| TTL    | 30 minutes per feed URL                        |
| Format | Atom 1.0 (XDocument parsing; RSS 2.0 fallback) |

### Environment variables

| Variable                | Required | Description                                    |
| ----------------------- | -------- | ---------------------------------------------- |
| `DB_PATH`               | No       | Path to SQLite cache file (default: `rss.db`)  |
| `ASPNETCORE_HTTP_PORTS` | —        | Set to `8085` in Docker                        |

### Frontend integration

`NewsFeedWidget.svelte` loads articles on mount via `RssFeedStore.svelte.ts`. Feed URLs and the article count are controlled by the settings persisted in `SettingsStore.svelte.ts`, configurable in Settings → RSS Feeds. Clicking an article opens the link in a new tab.

## Routing

**Caddy** acts as the reverse proxy and TLS terminator. URL-prefix routing maps paths to services:

```text
/tasks/*    →  tasks:8081
/weather/*  →  weather:8082
/spotify/*  →  spotify:8083
/photos/*   →  photos:8084
/rss/*      →  rss:8085
/quote/*    →  quote:8086
/           →  frontend:3000
```

The Caddy config is the same in development and production — the only difference is that `frontend:3000` points to the Vite dev server in dev and the Node production server in prod.

## Infrastructure

**Docker Compose** orchestrates all services. The compose setup is split into two files:

| File                          | Purpose                                                                                                      |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
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

