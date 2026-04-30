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
- In development, Vite proxies `/tasks` → `$TASKS_URL`, `/spotify` → `$SPOTIFY_URL`, and `/weather` → `$WEATHER_URL`; in production, Caddy handles the same routing

### Themes

Seven built-in themes are applied via CSS custom properties on `[data-theme]`. The active theme is persisted in `localStorage`.

| Theme | Style |
|-------|-------|
| `stone` | Dark warm neutrals |
| `linen` | Warm cream light mode |
| `forest` | Dark mossy green |
| `dusk` | Deep indigo dark mode |
| `ash` | Pure monochrome dark |
| `chalk` | Pure monochrome light |
| `terracotta` | Sandy earth tones light mode |

Each theme exposes the same semantic token set: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`. Components reference tokens only — never hardcoded colors.

Themes are defined in two places that must be kept in sync: `frontend/src/app.css` (CSS variables) and `frontend/src/lib/components/ThemeSwitcher.svelte` (switcher metadata).

## Backend Services

Each domain is a small, self-contained **ASP.NET Core 10 Minimal API** service backed by **SQLite**.

| Service   | Port | Status      | Responsibility          |
|-----------|------|-------------|-------------------------|
| `tasks`   | 8081 | Implemented | Task CRUD with due dates and recurrence |
| `weather` | 8082 | Implemented | Polls Open-Meteo, caches current + forecast |
| `spotify` | 8083 | Implemented | Spotify OAuth + now-playing |
| `plants`  | 8084 | Planned     | Watering schedules and reminders |
| `art`     | 8085 | Planned     | Rotates artwork from local files or APIs |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `<service>.db`). The SQLite dependency (`Microsoft.Data.Sqlite`) is isolated to the `Data` shared library — individual service projects depend only on the `Data.Abstractions` interface, keeping them portable and testable.

## Shared Libraries

Two shared projects live under `services/` and are referenced by service projects via `ProjectReference`. They are not deployed independently — they compile into each service that uses them.

| Project            | Responsibility |
|--------------------|----------------|
| `Data.Abstractions` | `IDatabase` interface and `DbCommandExtensions`; depends only on `System.Data.Common` (BCL) — no SQLite or other NuGet packages |
| `Data`              | `Database` — the concrete SQLite implementation of `IDatabase`; the only project that references `Microsoft.Data.Sqlite` |

Service projects follow this pattern:
- Reference `Data.Abstractions` for the `IDatabase` type used in stores and handlers
- Reference `Data` in `Program.cs` only, to register the concrete implementation: `AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath))`
- Never reference `Microsoft.Data.Sqlite` directly

JSON responses use `JsonNamingPolicy.SnakeCaseLower` so property names match frontend conventions (e.g. `created_at`, `due_date`).

## Tasks Service

The `tasks` service handles full CRUD for household tasks with optional due dates, times, and recurrence.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/tasks` | List tasks: all done tasks + undone tasks due within 60 days + undone tasks with no due date |
| `POST` | `/tasks` | Create a task; pre-generates all recurring instances up to 1 year ahead |
| `PUT` | `/tasks/{id}` | Update done status, description, or assignee |
| `DELETE` | `/tasks/{id}` | Delete a task; `?series=true` deletes all instances of the recurring series |

### Recurrence Model

Recurring tasks use a **pre-generation** approach: when a task with a recurrence rule is created, all instances up to one year ahead are inserted as individual rows at creation time. Each instance row stores the full recurrence definition and shares a `series_id` (equal to the `id` of the first instance in the series).

| Field | Type | Description |
|-------|------|-------------|
| `recurrence_unit` | `"day"` \| `"week"` \| `"month"` | Interval unit |
| `recurrence_interval` | integer | Number of units between occurrences |
| `recurrence_days` | comma-separated string | Weekday names for weekly rules (e.g. `"Mon,Wed,Fri"`) |
| `recurrence_end_date` | datetime | Optional last date for the series; no instances are generated beyond this date |
| `series_id` | integer | Groups instances; equals the `id` of the first instance; `NULL` for non-recurring tasks |

**Rolling horizon:** `GET /tasks` lazily extends any series whose last undone instance falls within 30 days, generating new rows up to `recurrence_end_date` (or 1 year if no end date is set). No background job is required.

**Marking done:** Marking an instance done simply sets `done = 1` on that row. Future instances already exist and remain unaffected.

**Deleting a series:** `DELETE /tasks/{id}?series=true` looks up the `series_id` of the given task and deletes all rows sharing that `series_id`.

### Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DB_PATH` | `tasks.db` | Path to the SQLite database file |
| `ASPNETCORE_HTTP_PORTS` | — | Set to `8081` in Docker |

## Weather Service

The `weather` service fetches current conditions and a 7-day forecast from [Open-Meteo](https://open-meteo.com/) (no API key required) and caches the result in SQLite to avoid redundant fetches.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/weather/current` | Returns current conditions; uses cache if fresh |
| `GET` | `/weather/forecast` | Returns 7-day forecast; uses cache if fresh |

Both endpoints return `503` with `{ "error": "location not configured" }` if `LATITUDE` or `LONGITUDE` are missing, and log a `LogError` pointing to `.env.example`.

### Environment variables

| Variable | Required | Description |
|----------|----------|-------------|
| `LATITUDE` | Yes | Decimal latitude (e.g. `40.7128`) |
| `LONGITUDE` | Yes | Decimal longitude (e.g. `-74.0060`) |
| `DB_PATH` | No | Path to SQLite cache file (default: `weather.db`) |
| `ASPNETCORE_HTTP_PORTS` | — | Set to `8082` in Docker |

Place these in `services/Weather/.env`. Docker Compose loads the file via `env_file`; for local `dotnet run`, `DotNetEnv` loads the same file before `CreateBuilder`.

## Spotify Service

The `spotify` service handles OAuth 2.0 authorization with Spotify and exposes now-playing data. It stores a single token row in SQLite — only one Spotify account is linked at a time.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/spotify/auth` | Begins OAuth flow — redirects the browser to Spotify's authorization page |
| `GET` | `/spotify/callback` | OAuth callback — exchanges the code for tokens, saves them, redirects to `FRONTEND_URL` |
| `GET` | `/spotify/now-playing` | Returns the current track, or 204 if nothing is playing, or 401 if unauthenticated |
| `GET` | `/spotify/status` | Returns `{ authenticated: bool }` |
| `DELETE` | `/spotify/auth` | Clears the stored tokens, effectively disconnecting Spotify |

### Environment variables

Stored in `services/Spotify/.env` (loaded by `DotNetEnv`; also referenced via `env_file` in `docker-compose.yml`):

| Variable | Required | Description |
|----------|----------|-------------|
| `SPOTIFY_CLIENT_ID` | Yes | Spotify app client ID |
| `SPOTIFY_CLIENT_SECRET` | Yes | Spotify app client secret |
| `SPOTIFY_REDIRECT_URI` | Yes | Must match a URI registered in the Spotify app dashboard (e.g. `http://127.0.0.1:8083/spotify/callback`) |
| `FRONTEND_URL` | No | URL to redirect to after OAuth completes; defaults to `/` on the service itself — set to the Caddy entry point (e.g. `http://localhost`) in Docker |

### Frontend integration

`NowPlaying.svelte` polls `/spotify/now-playing` every 5 seconds via `SpotifyStore.ts`. The store value drives three UI states:

- `undefined` — 401 response → shows "Connect Spotify" link pointing to `/spotify/auth`
- `null` — 204 response → shows "Nothing playing · Disconnect" button
- `NowPlaying` — 200 response → shows track card with album art, progress bar, and a hover-revealed disconnect button

Clicking disconnect calls `DELETE /spotify/auth` then immediately re-polls, which returns 401 and flips the store back to `undefined`.

## Routing

**Caddy** acts as the reverse proxy and TLS terminator. URL-prefix routing maps paths to services:

```text
/tasks/*    →  tasks:8081
/weather/*  →  weather:8082
/spotify/*  →  spotify:8083
/           →  frontend:3000
```

The Caddy config is the same in development and production — the only difference is that `frontend:3000` points to the Vite dev server in dev and the Node production server in prod.

## Infrastructure

**Docker Compose** orchestrates all services. The compose setup is split into two files:

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Production: multi-stage builds, static frontend bundle |
| `docker-compose.override.yml` | Development: auto-merged by Compose; swaps the frontend for the Vite dev server and configures file watching |

### Development workflow

```bash
docker compose watch
```

`docker-compose.override.yml` overrides the frontend service to use `frontend/Dockerfile.dev`, which runs `vite dev --host --port 3000`. The `develop.watch` block syncs changes from:

- `frontend/src/` → `/app/src` (triggers Vite HMR — browser updates without a page reload)
- `frontend/static/` → `/app/static` (same)
- `package.json`, `svelte.config.js`, `vite.config.ts` → full container rebuild

HMR WebSocket connections go through Caddy on port 80. The `HMR_CLIENT_PORT=80` environment variable tells Vite to advertise port 80 to the browser (`server.hmr.clientPort`), so the websocket connects through the proxy rather than directly to the container port.

### Production build

```bash
docker compose -f docker-compose.yml up --build
```

Explicitly excludes `docker-compose.override.yml`, using the production Dockerfile which builds the SvelteKit app with `@sveltejs/adapter-node` and serves it with `node index.js`.

## Real-time Updates

**SSE (Server-Sent Events)** will be used for pushing display refreshes to the frontend. One-way push is simpler and more reliable than WebSockets for an ambient screen that only reads data.

## Directory Structure

```text
Hearth/
├── frontend/
│   ├── src/
│   │   ├── routes/
│   │   │   ├── +layout.svelte          # root layout, theme init, task loading
│   │   │   ├── +page.svelte            # schedule page (tasks + music)
│   │   │   └── calendar/
│   │   │       └── +page.svelte        # calendar view
│   │   ├── lib/
│   │   │   ├── api.ts                  # centralised API calls
│   │   │   ├── TaskStore.ts            # tasks writable store
│   │   │   ├── SpotifyStore.ts         # now-playing writable store
│   │   │   └── components/
│   │   │       ├── Nav.svelte
│   │   │       ├── ThemeSwitcher.svelte
│   │   │       ├── Schedule.svelte
│   │   │       ├── NowPlaying.svelte
│   │   │       ├── TaskList.svelte
│   │   │       └── Calendar.svelte
│   │   └── app.css                     # Tailwind base + 7 theme definitions
│   ├── Dockerfile                      # production (multi-stage, node adapter)
│   ├── Dockerfile.dev                  # development (vite dev server)
│   └── vite.config.ts
├── services/
│   ├── Data.Abstractions/              # IDatabase interface + DbCommandExtensions (no SQLite dep)
│   │   ├── IDatabase.cs
│   │   ├── DbCommandExtensions.cs
│   │   └── Data.Abstractions.csproj
│   ├── Data/                           # concrete SQLite implementation of IDatabase
│   │   ├── Database.cs
│   │   └── Data.csproj
│   ├── Tasks/                          # ASP.NET Core 10 Minimal API, port 8081
│   │   ├── Program.cs
│   │   ├── TaskStore.cs
│   │   ├── Extensions/
│   │   ├── Records/
│   │   └── Dockerfile
│   ├── Weather/                        # ASP.NET Core 10 Minimal API, port 8082
│   │   ├── Program.cs
│   │   ├── WeatherStore.cs
│   │   ├── WeatherFetcher.cs
│   │   ├── Extensions/
│   │   ├── Records/
│   │   └── Dockerfile
│   └── Spotify/                        # ASP.NET Core 10 Minimal API, port 8083
│       ├── Program.cs
│       ├── SpotifyStore.cs
│       ├── SpotifyClientService.cs
│       ├── Extensions/
│       ├── Records/
│       ├── .env                        # SPOTIFY_CLIENT_ID, SPOTIFY_CLIENT_SECRET, SPOTIFY_REDIRECT_URI
│       └── Dockerfile
├── docker-compose.yml                  # production
├── docker-compose.override.yml         # development (auto-merged)
├── Caddyfile
├── README.md
└── docs/
    ├── PRODUCT.md
    └── SOFTWARE-DESIGN.md
```
