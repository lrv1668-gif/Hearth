# Hearth — Software Design

## Architecture Overview

Hearth is a self-hosted home dashboard. The frontend (SvelteKit) talks to backend microservices through a Caddy reverse proxy. All services are isolated .NET 10 Minimal API processes; each owns its own SQLite database.

```
Browser
  └── Caddy :80
        ├── /tasks*    → Tasks      :8081
        ├── /spotify*  → Spotify    :8083
        ├── /weather*  → Weather    :8082
        ├── /photos*   → Photos     :8084
        ├── /rss*      → Rss        :8085
        ├── /calendar* → Calendar   :8087
        ├── /quote*    → Quote      :8086
        ├── /birds*    → Birds      :8088
        ├── /almanac*  → Almanac    :8089
        └── /*         → Frontend   :3000
```

### Shared libraries

| Project | Purpose |
|---|---|
| `Data.Abstractions` | `IDatabase` interface + `DbCommandExtensions` / `DbReaderExtensions`. No SQLite dependency. |
| `Data` | `Database : IDatabase` backed by `Microsoft.Data.Sqlite`, plus the `AddSqliteDatabase(key, defaultDbFileName)` registration helper. |
| `ServiceDefaults` | Framework-only (no SQLite) library referenced by all 9 services: `AddHearthWebDefaults()` (CORS + snake_case JSON), the shared `HearthJson.SnakeCaseLower` options instance, and the `ConfigRequirement` helpers for validating required env vars. |

Services reference `Data.Abstractions`/`Data` in their `.csproj` and inject `IDatabase` via `[FromKeyedServices("key")]`. The 6 SQLite-backed services register it with one call in `ServiceCollectionExtensions.cs`:

```csharp
services.AddSqliteDatabase("key", "service.db");
```

Every service's `ServiceCollectionExtensions.cs` also calls `services.AddHearthWebDefaults();` once, replacing the CORS + JSON setup that used to be repeated per service.

---

## Backend Services

### Tasks (port 8081)

CRUD for tasks with due dates, recurrence, assignees, and countdown events. No external API.

### Spotify (port 8083)

OAuth 2.0 integration with Spotify Web API. Surfaces currently-playing track for the Now Playing widget. Tokens persisted in SQLite; auto-refreshed on each `now-playing` request via `AuthorizationCodeAuthenticator`.

**Env vars:** `SPOTIFY_CLIENT_ID`, `SPOTIFY_CLIENT_SECRET`, `SPOTIFY_REDIRECT_URI`

### Weather (port 8082)

Fetches current conditions and 7-day forecast from Open-Meteo. Cached in SQLite with a configurable TTL.

**Env vars:** `LATITUDE`, `LONGITUDE`, `WEATHER_UNIT`

### Photos (port 8084)

Serves random photos from Unsplash API or user-uploaded local photos. Uploaded files stored on a Docker volume, with optional per-photo captions persisted to `captions.json` alongside them. A `seasonal` category token in the query is expanded server-side into a season-appropriate Unsplash search term (hemisphere from `LATITUDE`).

**Env vars:** `UNSPLASH_ACCESS_KEY`, `LATITUDE` (optional — seasonal category hemisphere)

### Rss (port 8085)

Fetches and parses RSS/Atom feeds on demand. No persistent cache — feeds are fetched per request with HTTP conditional requests.

### Birds (port 8088)

Fetches recent and notable bird observations near the configured coordinates from the eBird API v2, merges them (one sighting per species, most recent wins, notable species flagged), and caches the result in SQLite for 60 minutes. Distance from home is computed with the haversine formula and served in miles.

**Env vars:** `EBIRD_API_KEY` (free at https://ebird.org/api/keygen), `LATITUDE`, `LONGITUDE`, `BIRDS_RADIUS_KM` (optional, default 15)

**Endpoint:** `GET /birds/recent` → `BirdSighting[]` (`503` when env vars missing, `502` when eBird is unreachable)

### Almanac (port 8089)

Computes seasonal facts entirely locally — no external API, no SQLite (stateless, so it has no `Data`/`Data.Abstractions` reference and no Docker volume). The response always contains the pinned **season** section plus at most **two rotating slots**, filled in priority order (daylight → timely frost → note); unfilled or bumped sections are `null`:

- **Season** (always present) — name, Early/Mid/Late label, day-of-season, progress, and countdown to the next equinox/solstice, from a hardcoded table of solstice/equinox UTC instants (2024–2040) in `SeasonCalculator`. Hemisphere derived from the sign of `LATITUDE` (northern when unset).
- **Daylight** — trend (min/day gained or lost over the past week), drift vs. the most recent solstice, and the single next wall-clock milestone ("Last 8 pm sunset · Aug 13") from the NOAA sunrise/sunset algorithm in `SolarCalculator`. `null` when coordinates are unset.
- **Frost** — countdown to the next of the user's typical first/last frost dates. Only claims a slot when ≤ 42 days away (bumping the note); `null` when unset or not yet timely.
- **Note** — curated phenology/in-season sentence keyed by half-month (`PhenologyData`, temperate Northern Hemisphere; `null` for southern installs).

Unlike Weather, missing coordinates do **not** produce a `503` — the endpoint logs an error at startup and returns `200` with `daylight: null`, because season and note are date-only.

**Env vars (all optional):** `LATITUDE`, `LONGITUDE`, `TZ` (IANA zone for wall-clock milestones; defaults to system zone), `FIRST_FROST` / `LAST_FROST` (`MM-DD`)

**Endpoint:** `GET /almanac` → `{ season, daylight | null, frost | null, note | null }`

### Calendar (port 8087)

Google Calendar + Google Tasks OAuth 2.0 integration. Surfaces events (read-only) and tasks (completable via checkbox) for the next 14 days. Built with an extensibility interface so future providers (Outlook, etc.) can be added.

#### Extensibility interface

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

`/calendar/items` injects `IEnumerable<ICalendarProvider>` and fans out across all authenticated providers. Adding a second provider requires implementing `ICalendarProvider` and registering it with `services.AddSingleton<ICalendarProvider>(...)`.

#### SQLite schema

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

#### Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/calendar/google/auth` | Validate env vars, generate CSRF state, redirect to Google |
| `GET` | `/calendar/google/callback` | Validate state, exchange code for tokens, redirect to frontend |
| `GET` | `/calendar/google/status` | `{ authenticated: bool }` |
| `DELETE` | `/calendar/google/auth` | Clear token + items cache (disconnect) |
| `GET` | `/calendar/items` | Aggregated `CalendarItem[]` from all authenticated providers (5-min cache) |
| `PATCH` | `/calendar/google/tasks/{listId}/{taskId}` | Toggle Google Task completion, invalidate cache |

#### OAuth details

- Scopes: `calendar.readonly` + `tasks` (write scope required for task toggle)
- `access_type=offline` + `prompt=consent` force refresh token on every authorization
- Token refresh: manual — checked 30 seconds before expiry using `UserCredential.RefreshTokenAsync`
- If refresh token is absent in the refresh response, falls back to the stored value
- `GoogleCredential.FromAccessToken` used to build service clients after refresh

#### Caching

`GetItemsAsync` fetches Google Calendar events and Google Tasks in parallel, merges them, and caches the result for 5 minutes per provider. Task toggle (`SetTaskCompletedAsync`) invalidates the cache so the next `/calendar/items` fetch reflects the change.

**Env vars:** `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI`

#### `CalendarItem` record

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

---

## Frontend

### Stores

Each data type has its own store file in `src/lib/stores/`:

| File | State | Init |
|---|---|---|
| `TaskStore.svelte.ts` | `tasks: Task[]` | `loadTasks()` on layout mount |
| `CalendarStore.svelte.ts` | `items: CalendarItem[]`, `googleConnected: boolean` | `loadCalendarStatus()` + `loadCalendarItems()` on layout mount |
| `SpotifyStore.svelte.ts` | `nowPlaying: NowPlaying \| null \| undefined` | On demand |
| `SettingsStore.svelte.ts` | Widget config, theme, RSS feeds | localStorage |
| `ThemeStore.svelte.ts` | `theme: ThemeId` | `initTheme()` on layout mount |
| `FontThemeStore.svelte.ts` | `fontTheme: FontThemeId` | `initFontTheme()` on layout mount |
| `FontSizeStore.svelte.ts` | `scale: number` (0.9–1.3) | `initFontSize()` on layout mount |
| `RssFeedStore.svelte.ts` | `groups: RssFeedGroup[]` | On demand |
| `DailyQuoteStore.svelte.ts` | `quote: ZenQuote \| null` | On demand, once per day |
| `BirdsStore.svelte.ts` | `sightings: BirdSighting[]`, `error: boolean` | On demand (widget mount) |

### Discriminated union for mixed task/event lists

`api.ts` exports an `Item` type shared by `Calendar.svelte`, `DayOverflowModal.svelte`, and `UpcomingTasksWidget.svelte`:

```typescript
export type Item =
    | { kind: 'task';  data: Task }
    | { kind: 'event'; data: CalendarItem };
```

### Key utility functions (`utils.ts`)

- `formatTime(time)` — Converts `"HH:MM"` (24h) to `"4PM"` / `"4:30PM"` format
- `eventDateKey(event)` — Returns `"YYYY-MM-DD"` in local time; slices all-day strings directly to avoid UTC midnight shift from `new Date("YYYY-MM-DD")`

### Themes

Defined in two places (both must be updated together):
1. `src/themes.css` — CSS custom property blocks `[data-theme="id"] { ... }`
2. `src/lib/constants/themes.ts` — `themes` array

Current themes: `stone`, `linen`, `forest`, `dusk`, `ash`, `chalk`, `terracotta`, `tide`, `slate`, `blush`, `frost`, `smoke`, `sage`, `sky`, `plum`, `olive`

### Font themes

Named typography presets, orthogonal to color themes. Each preset bundles a font family (self-hosted `@fontsource-variable` packages), four semantic weights (`--weight-regular/medium/semibold/bold` — Tailwind's `font-medium/semibold/bold` resolve to these vars via the `@theme` block in `app.css`), and a size multiplier (`--font-scale`, folded into every `--font-*`/`--icon-*` clamp in `app.css`).

Defined in two places (both must be updated together):
1. `src/fonts.css` — `@fontsource` imports plus one `[data-font="id"] { ... }` block per preset (source of truth for stacks, weights, scale). Selectors stay bare `[data-font]` — `FontThemePicker.svelte` sets `data-font` on its preview buttons so the same blocks style the previews.
2. `src/lib/constants/fontThemes.ts` — `fontThemes` array (picker metadata only: `id`, `label`, `tag`)

Applied as `data-font` on `<html>` by `FontThemeStore.svelte.ts` (localStorage key `hearth-font`).

Separately, a user size slider in Settings (`FontSizeSlider.svelte`) sets `--font-user-scale` (0.9–1.3) as an inline style on `<html>` via `FontSizeStore.svelte.ts` (localStorage key `hearth-font-size`). `app.css` composes both into `--scale: calc(var(--font-scale) * var(--font-user-scale))`, which every `--font-*`/`--icon-*` clamp multiplies by.

Current font themes: `inter` (default), `system`, `nunito`, `source-serif`, `space-grotesk`, `roboto-slab`, `lora`, `manrope`, `fraunces`
