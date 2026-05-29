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
        └── /*         → Frontend   :3000
```

### Shared libraries

| Project | Purpose |
|---|---|
| `Data.Abstractions` | `IDatabase` interface + `DbCommandExtensions` / `DbReaderExtensions`. No SQLite dependency. |
| `Data` | `Database : IDatabase` backed by `Microsoft.Data.Sqlite`. Referenced only in `Program.cs` of each service. |

Services reference `Data.Abstractions` in their `.csproj` and inject `IDatabase` via `[FromKeyedServices("key")]`. The concrete `Database` is registered once in `Program.cs`:

```csharp
services.AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath));
```

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

Serves random photos from Unsplash API or user-uploaded local photos. Uploaded files stored on a Docker volume.

**Env vars:** `UNSPLASH_ACCESS_KEY`

### Rss (port 8085)

Fetches and parses RSS/Atom feeds on demand. No persistent cache — feeds are fetched per request with HTTP conditional requests.

### Calendar (port 8087)

Google Calendar OAuth 2.0 integration. Exposes read-only events for the next 14 days. Built with an extensibility interface so future providers (Outlook, etc.) can be added.

#### Extensibility interface

```csharp
public interface ICalendarProvider
{
    string ProviderKey { get; }          // "google", "outlook", etc.
    bool IsAuthenticated { get; }        // sync SQLite row-presence check
    string GetAuthUrl(string state);
    Task HandleCallbackAsync(string code, CancellationToken ct = default);
    void Disconnect();                   // clears token + events cache
    Task<IEnumerable<CalendarEvent>> GetEventsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
```

`/calendar/events` injects `IEnumerable<ICalendarProvider>` and fans out across all authenticated providers. Adding a second provider requires implementing `ICalendarProvider` and registering it with `services.AddSingleton<ICalendarProvider>(...)`.

#### SQLite schema

```sql
-- One row per provider
CREATE TABLE IF NOT EXISTS calendar_tokens (
    provider      TEXT PRIMARY KEY,
    access_token  TEXT NOT NULL,
    refresh_token TEXT NOT NULL,
    expires_at    TEXT NOT NULL   -- ISO 8601
);

-- Cached event JSON per provider, 5-minute TTL
CREATE TABLE IF NOT EXISTS calendar_events_cache (
    provider    TEXT PRIMARY KEY,
    events_json TEXT NOT NULL,
    cached_at   TEXT NOT NULL     -- ISO 8601
);
```

#### Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/calendar/google/auth` | Validate env vars, generate CSRF state, redirect to Google |
| `GET` | `/calendar/google/callback` | Validate state, exchange code for tokens, redirect to frontend |
| `GET` | `/calendar/google/status` | `{ authenticated: bool }` |
| `DELETE` | `/calendar/google/auth` | Clear token + events cache (disconnect) |
| `GET` | `/calendar/events` | Aggregated `CalendarEvent[]` from all authenticated providers |

#### OAuth details

- Scope: `calendar.readonly`
- `access_type=offline` + `prompt=consent` force refresh token on every authorization
- Token refresh: manual — checked 30 seconds before expiry using `UserCredential.RefreshTokenAsync`
- If refresh token is absent in the refresh response, falls back to the stored value
- `GoogleCredential.FromAccessToken` used to build `CalendarService` after refresh

#### Caching

`GetEventsAsync` checks `calendar_events_cache` on every call. If `cached_at` is within the last 5 minutes, returns the cached JSON. On cache miss: refreshes token if needed, calls Google Calendar API, stores result.

**Env vars:** `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI`

#### `CalendarEvent` record

```csharp
public record CalendarEvent(
    string Id,
    string Title,
    string? Description,
    string? Location,
    string Start,       // ISO 8601 with offset OR "YYYY-MM-DD" for all-day
    string End,
    bool IsAllDay,
    string? CalendarName,
    string Provider     // "google"
);
```

---

## Frontend

### Stores

Each data type has its own store file in `src/lib/stores/`:

| File | State | Init |
|---|---|---|
| `TaskStore.svelte.ts` | `tasks: Task[]` | `loadTasks()` on layout mount |
| `CalendarStore.svelte.ts` | `events: CalendarEvent[]`, `googleConnected: boolean` | `loadCalendarStatus()` + `loadCalendarEvents()` on layout mount |
| `SpotifyStore.svelte.ts` | `nowPlaying: NowPlaying \| null \| undefined` | On demand |
| `SettingsStore.svelte.ts` | Widget config, theme, RSS feeds | localStorage |
| `ThemeStore.svelte.ts` | `theme: ThemeId` | `initTheme()` on layout mount |
| `RssFeedStore.svelte.ts` | `groups: RssFeedGroup[]` | On demand |
| `DailyQuoteStore.svelte.ts` | `quote: ZenQuote \| null` | On demand, once per day |

### Discriminated union for mixed task/event lists

`api.ts` exports an `Item` type shared by `Calendar.svelte`, `DayOverflowModal.svelte`, and `UpcomingTasksWidget.svelte`:

```typescript
export type Item =
    | { kind: 'task';  data: Task }
    | { kind: 'event'; data: CalendarEvent };
```

### Key utility functions (`utils.ts`)

- `formatTime(time)` — Converts `"HH:MM"` (24h) to `"4PM"` / `"4:30PM"` format
- `eventDateKey(event)` — Returns `"YYYY-MM-DD"` in local time; slices all-day strings directly to avoid UTC midnight shift from `new Date("YYYY-MM-DD")`

### Themes

Defined in two places (both must be updated together):
1. `src/themes.css` — CSS custom property blocks `[data-theme="id"] { ... }`
2. `src/lib/constants/themes.ts` — `themes` array

Current themes: `stone`, `linen`, `forest`, `dusk`, `ash`, `chalk`, `terracotta`, `tide`, `slate`, `blush`, `frost`, `smoke`, `sage`, `sky`
