# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Agent Instructions

Keep documentation in sync with code at all times:

- When a new service is added or changed, update `docs/SOFTWARE-DESIGN.md` and the directory structure below.
- When product decisions or feature scope changes, update `docs/PRODUCT.md`.
- When a theme is added or removed, update `docs/SOFTWARE-DESIGN.md` (theme list) and the theme list below.
- When a widget is added or removed, update the widget list in `README.md` and the relevant section of `docs/SOFTWARE-DESIGN.md` — use the `hearth-widget` skill to build it. E-paper frame widgets are registered in `frontend/src/lib/constants/frameWidgets.ts`, take a `lines` budget (see `allocateLines` in `lib/frame.ts`), and add their data loader to `widgetLoaders` in `routes/frame/+page.svelte`. When the frame shows the same content as a dashboard widget, don't copy it: put the markup in a shared view in `components/widgets/views/` (data in via props, an optional `lines` budget, sizes from `type-*`/`icon-*`/em units and `--stroke` so the frame's overrides scale it) and keep the dashboard widget a thin wrapper that loads and polls. Only frame-specific designs go in `components/frame/widgets/`. Only widgets that stay correct between scheduled refreshes belong on the frame.
- The GitHub Pages site in `site/` repeats some of this — theme/palette/typeface counts, the widget grid, the host's service list, and the status table in `build.html`. When those change, or a planned piece gets built, update the site too.
- Build UI from the shared components in `frontend/src/lib/components/ui/` (`Button`, `IconButton`, `TextInput`/`TextArea`/`Select` + `Field`, `ChoiceChips`/`Chip`, `SegmentedControl`, `DayPicker`, `Toggle`/`ToggleRow`, `Disclosure`, `Modal`, `ListRow`, `InlineMessage`, …) and the `.eyebrow` class for small-caps headings — don't hand-roll button, input or dialog class strings. The list is in `docs/SOFTWARE-DESIGN.md` → Shared UI components.
- Widget visibility and layout settings use the shared `WidgetLayoutEditor` (columns plus a Hidden tray) for both the dashboard and the frame — keep the two screens consistent rather than adding bespoke editors.
- When a new service needs environment variables, create `src/<Service>/.env`, add `env_file` to `docker-compose.yml` (alongside any static `environment:` values), and add a `LogError` call in the endpoint when required vars are missing.
- When adding a new backend service: add it to `docker-compose.yml` (merge `*defaults`, `build: { context: ./src, args: { SERVICE: <Service> } }` — there are no per-service Dockerfiles; the shared `src/Dockerfile` builds them all), `Caddyfile`, the Vite dev proxy in `frontend/vite.config.ts` (plus a localhost port mapping in `docker-compose.override.yml`), and follow the service pattern below. Do not add `Microsoft.Data.Sqlite` as a direct dependency. Reference `ServiceDefaults` and call `AddHearthWebDefaults()` in `ServiceCollectionExtensions.cs` — every service does this, including services with no database. Give it health checks (`services.AddHealthChecks().AddDatabase(...)`/`.AddRequiredConfig(...)`), call `app.MapHearthHealth("<prefix>")`, add it to `frontend/src/lib/constants/services.ts` so Settings → Diagnostics lists it, and give its `docker-compose.yml` entry the `healthcheck` (`<<: *probe`, `test: ["CMD", "/app/service", "--health", "/<prefix>/health"]`) — `Program.cs` starts with `HealthProbe.ExitIfProbe(args);`.
- Each frontend data type gets its own `<Type>Store.svelte.ts` file (the `.svelte.ts` suffix is required for runes) — do not combine stores.
- Use `[FromKeyedServices("key")]` to inject `IDatabase` into stores — never inject the concrete `Database` class.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, moon phase, tasks and countdowns, a calendar agenda, Spotify now-playing, news feeds, a daily quote, bird sightings, transit departures, and almanac facts into a single quiet display. (Plant care is planned in `docs/PRODUCT.md` but not built yet — there is no plants service.) The primary target is an always-on, ambient e-paper frame — a Pimoroni Inky Impression 13.3" (Spectra 6, 1600×1200) driven by a Raspberry Pi Zero 2 W thin client that pulls a host-rendered frame (see `docs/HARDWARE.md`); the frame's panel size is configurable for other panels — though it is also accessible via web browser.

## Tech Stack

- **Frontend:** SvelteKit 2 + Svelte 5 (runes syntax) + Tailwind CSS v4 + Lucide Svelte (`@lucide/svelte`), built with Vite
- **Services:** .NET 10, ASP.NET Core Minimal APIs, SQLite (isolated to `src/Data` — service projects do not reference `Microsoft.Data.Sqlite` directly)
- **Proxy:** Caddy 2
- **Infra:** Docker Compose

## Directory Structure

```text
frontend/                    # SvelteKit app — shared controls in src/lib/components/ui/
src/                         # Backend API + shared-library projects
  Dockerfile                 # Shared image for every backend service — pick one with the SERVICE build arg
  Data.Abstractions/         # Shared interfaces — IDatabase, DbCommandExtensions (no SQLite dep)
  Data/                      # SQLite implementation of IDatabase + AddSqliteDatabase() registration helper; SqliteDistributedCache (FusionCache L2)
  ServiceDefaults/           # Shared JSON defaults (AddHearthWebDefaults — no CORS: everything is same-origin via Caddy), AddHearthCache (FusionCache), ConfigRequirement env-var validation + HomeLocationClient (the home location saved in Display, env-var fallback), HearthHealth (GET /<service>/health for Settings → Diagnostics) — referenced by every service
  Tasks/                     # ASP.NET Core 10 Minimal API, port 8081
  Spotify/                   # ASP.NET Core 10 Minimal API, port 8083 — Spotify OAuth + now-playing
  Weather/                   # ASP.NET Core 10 Minimal API, port 8082 — weather fetch + cache
  Photos/                    # ASP.NET Core 10 Minimal API, port 8084 — Unsplash photo fetch + cache, local uploads + captions + photo groups, seasonal query expansion
  Rss/                       # ASP.NET Core 10 Minimal API, port 8085 — RSS/Atom feed fetch + cache
  Quote/                     # ASP.NET Core 10 Minimal API, port 8086 — ZenQuotes daily quote + cache
  Calendar/                  # ASP.NET Core 10 Minimal API, port 8087 — Google Calendar OAuth + events cache
  Birds/                     # ASP.NET Core 10 Minimal API, port 8088 — eBird nearby sightings + cache
  Almanac/                   # ASP.NET Core 10 Minimal API, port 8089 — season/daylight/frost facts, computed locally (no DB)
  Trains/                    # ASP.NET Core 10 Minimal API, port 8091 — Transitland v2 departures fetch + cache
  Display/                   # ASP.NET Core 10 Minimal API, port 8090 — shared home location/time zone, e-paper frame settings, dithered frame preview + the scheduled panel render (`GET /display/frame`, FrameRenderService)
renderer/                    # Node sidecar for Display — headless Chromium (Playwright) screenshots /frame, dither.mjs reduces it to the 6 inks (epdoptimize + Bayer); `npm test`
tests/                       # xUnit test projects — <Service>.Tests/
  Hearth.TestUtilities/      # Shared test helpers — FakeHttpMessageHandler, TempDatabase
docker-compose.yml
docker-compose.override.yml  # dev overrides — auto-merged by Compose
.env.example                 # Root compose vars for a deployed host (image prefix/tag, FRONTEND_URL)
deploy/                      # Host auto-update: update.sh + systemd timer (pulls CI-published GHCR images); dozzle-user.sh sets the /logs login
dozzle/                      # Dozzle's /data bind mount (users.yml login) — gitignored except .gitkeep; Dozzle itself is opt-in (COMPOSE_PROFILES=logs)
site/                        # Static GitHub Pages showcase (no build step; deployed by .github/workflows/pages.yml) — screenshots use demo data, never real tasks/calendar/location
Caddyfile
```

## Conventions

### Verification Commands

- Type-check frontend: `cd frontend && npm run check` (runs `svelte-kit sync` + `svelte-check`)
- Frontend unit tests (vitest): `cd frontend && npm run test`
- Frontend formatting: `cd frontend && npm run format:check` (`npm run format` to fix)
- Build a backend service: `dotnet build src/<Service>/<Service>.csproj`
- Run a service's tests: `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj`
- Full CI equivalent: `dotnet build Hearth.slnx && dotnet test Hearth.slnx`, plus `npm run check && npm run test && npm run build` in `frontend/`
- CI runs only the stacks a PR touches (the `changes` job's path filters in `.github/workflows/ci.yml`) and everything on pushes to `main` — a new top-level folder or root file a stack depends on needs adding to those filters
- Before committing a UI-visible change, run the `verify` skill to drive the real UI
- Deploying: CI's `publish-images` job pushes amd64+arm64 images to GHCR on every green push to `main`; a host pulls them via `deploy/update.sh` (see `docs/SOFTWARE-DESIGN.md` → Deploying to a host). A new service needs a matching `include` entry in that job's matrix and an `image:` line in `docker-compose.yml`
- Run the app locally: `docker compose up --build` (dev overrides in `docker-compose.override.yml` expose service ports on localhost), then `cd frontend && npm run dev` for a hot-reloading frontend — Vite's dev proxy in `frontend/vite.config.ts` forwards `/tasks`, `/weather`, etc. to those ports

### Backend (C# / ASP.NET Core)

- New database-backed services register `IDatabase` with `services.AddSqliteDatabase("key", "service.db")` (from `Data`) in `ServiceCollectionExtensions.cs`, which reads `DB_PATH` and does the keyed registration — never call `AddKeyedSingleton<IDatabase>` directly
- Service stores (e.g. `TaskStore`) inject `IDatabase` via `[FromKeyedServices("key")]` — never a concrete `Database` or SQLite types directly
- New database-backed services: reference `Data.Abstractions` for `IDatabase`, reference `Data` for the concrete registration; never add `Microsoft.Data.Sqlite` as a direct dependency
- `TargetFramework`/`Nullable`/`ImplicitUsings` and all NuGet package versions are centralized in `src/Directory.Build.props` + `src/Directory.Packages.props` (and the `tests/` equivalents) — new `.csproj` files should not redeclare them; add new packages as a version-less `<PackageReference Include="..." />` plus a matching `<PackageVersion>` entry in the relevant `Directory.Packages.props`
- JSON uses snake_case naming (`JsonNamingPolicy.SnakeCaseLower`) — keep record property names PascalCase in C#
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `<service>.db`)
- Log a `LogError` when a required environment variable is missing, before returning a 503
- Cache third-party API responses with `IFusionCache.GetOrSetAsync` (registered by `AddHearthCache()`, plus `AddSqliteDistributedCache(key)` for a restart-surviving L2) with fail-safe on — don't hand-write cache tables or staleness checks. Return 502 only when the call throws (nothing cached at all)
- Read the household's location/time zone through `HomeLocationClient` (`AddHearthHomeLocation()`), never `LATITUDE`/`LONGITUDE`/`TZ` directly — it falls back to them; key upstream caches by the coordinates
- Read numeric config with `config.GetValue<double>(...)` (invariant culture), never `double.Parse(config[...])`; format URLs with `string.Create(CultureInfo.InvariantCulture, ...)`

### Frontend (Svelte / TypeScript)

- Svelte components use Svelte 5 runes: `$state`, `$derived`, `$effect`, `$props`, `{@render}`
- API calls live in `frontend/src/lib/api.ts` — all paths are relative (e.g. `/tasks`)
- Each data type has its own store file: `TaskStore.svelte.ts`, `SpotifyStore.svelte.ts`, etc. — never combine into a shared `stores.ts`
- `ApiClient` in `api.ts` has `post<T>(url, body)` for JSON POST — don't add overloads; inline `fetch()` directly for body-less POST calls
- Every backend call goes through an `api.<service>` namespace on `ApiClient` — no standalone `fetch` helpers exported from `api.ts`
- Widgets that show upstream data refresh with `poll(load, ms)` from `utils.ts` (returned from `onMount`); stores keep the last good data when a refresh fails
- `{@html}` with any external content (e.g. calendar descriptions) must use `DOMPurify.sanitize()` first — `dompurify` is installed in `frontend/`

### Themes

Themes are defined in two places — both must be updated together:

1. `frontend/src/themes.css` — CSS custom property block `[data-theme="id"] { ... }`
2. `frontend/src/lib/constants/themes.ts` — entry in the `themes` array

Current themes: `stone`, `linen`, `forest`, `dusk`, `ash`, `chalk`, `terracotta`, `tide`, `slate`, `blush`, `frost`, `smoke`, `sage`, `sky`, `plum`, `olive`, `amber`, `ember`

Every picker theme must keep `--text-2` ≥ 4.5:1 and `--text-3` ≥ 3:1 against `--bg` (enforced by `src/lib/themes.test.ts`); use `--text-3`/`--text-4` only for icons and decoration, never for text people need to read.

Each theme defines: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, and `color-scheme` (for native inputs).

### Frame Themes

E-paper frame themes (`/frame?theme=`) are separate from the dashboard themes and defined in two places — both must be updated together:

1. `frontend/src/themes.css` — an `[data-theme='epaper-<id>']` block under "Frame palettes"
2. `frontend/src/lib/constants/frameThemes.ts` — entry in the `frameThemes` array (picker metadata; `dark: true` makes B&W mode use `epaper-bw-night`)

Current frame themes: `almanac` (default), `gallery`, `garden`, `coast`, `harbor`, `terracotta`, `letterpress`, `marigold`, `postbox`, `night`, `ember`, `moss`, `lantern`, `roundtable`; plus the B&W blocks `epaper-bw` and `epaper-bw-night`. Colours are limited to the six measured Spectra 6 inks plus the approved two-ink mixes listed in `themes.css`, and text/icon/line tokens must be pure inks, with text legible on both `--bg` and `--surface` — `themes.test.ts` enforces all of it. Never add a frame theme to `constants/themes.ts`. Frame blocks set the same tokens minus `color-scheme`, plus `--block`/`--on-block` (a pure ink for large solid shapes and the ink set on it, ≥4.5:1) and optional `--arc-night/day/golden/outline` for the Day Arc.

### Font Themes

Typography presets, orthogonal to color themes. Defined in two places — both must be updated together:

1. `frontend/src/fonts.css` — one `[data-font="id"]` block per preset (source of truth: `--font-family`, `--weight-regular/medium/semibold/bold`, `--font-scale`, and optionally `--display-family`, the e-paper frame's masthead/numeral face — Fraunces unless a serif preset sets its own). Keep selectors bare `[data-font]` (never `html[data-font]`) — `FontThemePicker.svelte` reuses the blocks on its preview buttons. Keep `--font-scale` within 0.95–1.08 (e-paper caption legibility floor).
2. `frontend/src/lib/constants/fontThemes.ts` — entry in the `fontThemes` array (picker metadata only)

Current font themes: `inter` (default), `system`, `nunito`, `source-serif`, `space-grotesk`, `roboto-slab`, `lora`, `manrope`, `fraunces`. Families are self-hosted `@fontsource-variable/*` packages (devDependencies) — no CDN fonts.

Tailwind's `fontWeight` scale is redefined to the weight vars, so text styling must use `.type-*` size classes plus `font-medium`/`font-semibold`/`font-bold` — never raw `text-sm`-style size utilities or numeric `font-[...]` weights.

A separate user size slider (`FontSizeStore.svelte.ts`, localStorage `hearth-font-size`) sets `--font-user-scale` (0.9–1.3) inline on `<html>`; `app.css` multiplies every size clamp by `--scale` = preset `--font-scale` × `--font-user-scale`.

### Testing (xUnit)

- Each service that gets tests has a matching `<Service>.Tests/` project in `tests/`
- Test projects use `Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Web`)
- Shared helpers live in `tests/Hearth.TestUtilities/` — reference it rather than copying helpers: `FakeHttpMessageHandler` for HTTP-backed services, `TempDatabase` (real SQLite temp file) for database-backed stores. Don't add a mocking library for `IDatabase`.
- Test projects inherit `TargetFramework`/`Nullable`/`ImplicitUsings` from `tests/Directory.Build.props` and package versions from `tests/Directory.Packages.props` — don't redeclare them
- Test method naming: `Method_Scenario_ExpectedOutcome`
- Add each new test project to `Hearth.slnx`
- Use the `/write-unit-tests` skill to scaffold a new test project
