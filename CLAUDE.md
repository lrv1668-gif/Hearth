# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Agent Instructions

Keep documentation in sync with code at all times:

- When a new service is added or changed, update `SOFTWARE-DESIGN.md` and the directory structure below.
- When product decisions or feature scope changes, update `docs/PRODUCT.md`.
- When a theme is added or removed, update `SOFTWARE-DESIGN.md` (theme list) and the theme list below.
- When a new service needs environment variables, create `services/<Service>/.env`, add `env_file` to `docker-compose.yml` (alongside any static `environment:` values), and add a `LogError` call in the endpoint when required vars are missing.
- When adding a new backend service: add it to `docker-compose.yml`, `Caddyfile`, the Vite dev proxy in `frontend/vite.config.ts` (plus a localhost port mapping in `docker-compose.override.yml`), and follow the service pattern below. Do not add `Microsoft.Data.Sqlite` as a direct dependency.
- Each frontend data type gets its own `<Type>Store.svelte.ts` file (the `.svelte.ts` suffix is required for runes) — do not combine stores.
- Use `[FromKeyedServices("key")]` to inject `IDatabase` into stores — never inject the concrete `Database` class.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, plant care, music, daily tasks, countdown events, moon phase, and news feeds into a single quiet display. The primary target is an always-on, ambient display (e.g. a digital picture frame), though it is also accessible via web browser.

## Tech Stack

- **Frontend:** SvelteKit 2 + Svelte 5 (runes syntax) + Tailwind CSS v3 + Lucide Svelte (`@lucide/svelte`), built with Vite
- **Services:** .NET 10, ASP.NET Core Minimal APIs, SQLite (isolated to `services/Data` — service projects do not reference `Microsoft.Data.Sqlite` directly)
- **Proxy:** Caddy 2
- **Infra:** Docker Compose

## Directory Structure

```text
frontend/                    # SvelteKit app
services/
  Data.Abstractions/         # Shared interfaces — IDatabase, DbCommandExtensions (no SQLite dep)
  Data/                      # SQLite implementation of IDatabase
  Tasks/                     # ASP.NET Core 10 Minimal API, port 8081
  Spotify/                   # ASP.NET Core 10 Minimal API, port 8083 — Spotify OAuth + now-playing
  Weather/                   # ASP.NET Core 10 Minimal API, port 8082 — weather fetch + cache
  Photos/                    # ASP.NET Core 10 Minimal API, port 8084 — Unsplash photo fetch + cache
  Rss/                       # ASP.NET Core 10 Minimal API, port 8085 — RSS/Atom feed fetch + cache
  Quote/                     # ASP.NET Core 10 Minimal API, port 8086 — ZenQuotes daily quote + cache
  Calendar/                  # ASP.NET Core 10 Minimal API, port 8087 — Google Calendar OAuth + events cache
  Birds/                     # ASP.NET Core 10 Minimal API, port 8088 — eBird nearby sightings + cache
docker-compose.yml
docker-compose.override.yml  # dev overrides — auto-merged by Compose
Caddyfile
```

## Conventions

### Verification Commands

- Type-check frontend: `cd frontend && npx svelte-check --tsconfig ./tsconfig.json`
- Build a backend service: `cd services && dotnet build <Service>/<Service>.csproj`
- Run a service's tests: `cd services && dotnet test <Service>.Tests/<Service>.Tests.csproj`
- Run the app locally: `docker compose up --build` (dev overrides in `docker-compose.override.yml` expose service ports on localhost), then `cd frontend && npm run dev` for a hot-reloading frontend — Vite's dev proxy in `frontend/vite.config.ts` forwards `/tasks`, `/weather`, etc. to those ports

### Backend (C# / ASP.NET Core)

- `Program.cs` in each service registers the concrete `Database` as `IDatabase` keyed by service name: `AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath))`
- Service stores (e.g. `TaskStore`) inject `IDatabase` via `[FromKeyedServices("key")]` — never a concrete `Database` or SQLite types directly
- New database-backed services: reference `Data.Abstractions` for `IDatabase`, reference `Data` only in `Program.cs` for the concrete registration; never add `Microsoft.Data.Sqlite` as a direct dependency
- JSON uses snake_case naming (`JsonNamingPolicy.SnakeCaseLower`) — keep record property names PascalCase in C#
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `<service>.db`)
- Log a `LogError` when a required environment variable is missing, before returning a 503

### Frontend (Svelte / TypeScript)

- Svelte components use Svelte 5 runes: `$state`, `$derived`, `$effect`, `$props`, `{@render}`
- API calls live in `frontend/src/lib/api.ts` — all paths are relative (e.g. `/tasks`)
- Each data type has its own store file: `TaskStore.svelte.ts`, `SpotifyStore.svelte.ts`, etc. — never combine into a shared `stores.ts`
- `ApiClient` in `api.ts` has `post<T>(url, body)` for JSON POST — don't add overloads; inline `fetch()` directly for body-less POST calls
- `{@html}` with any external content (e.g. calendar descriptions) must use `DOMPurify.sanitize()` first — `dompurify` is installed in `frontend/`

### Themes

Themes are defined in two places — both must be updated together:

1. `frontend/src/themes.css` — CSS custom property block `[data-theme="id"] { ... }`
2. `frontend/src/lib/constants/themes.ts` — entry in the `themes` array

Current themes: `stone`, `linen`, `forest`, `dusk`, `ash`, `chalk`, `terracotta`, `tide`, `slate`, `blush`, `frost`, `smoke`, `sage`, `sky`, `plum`, `olive`

Each theme defines: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, and `color-scheme` (for native inputs).

### Font Themes

Typography presets, orthogonal to color themes. Defined in two places — both must be updated together:

1. `frontend/src/fonts.css` — one `[data-font="id"]` block per preset (source of truth: `--font-family`, `--weight-regular/medium/semibold/bold`, `--font-scale`). Keep selectors bare `[data-font]` (never `html[data-font]`) — `FontThemePicker.svelte` reuses the blocks on its preview buttons. Keep `--font-scale` within 0.95–1.08 (e-paper caption legibility floor).
2. `frontend/src/lib/constants/fontThemes.ts` — entry in the `fontThemes` array (picker metadata only)

Current font themes: `inter` (default), `system`, `nunito`, `source-serif`, `space-grotesk`, `roboto-slab`. Families are self-hosted `@fontsource-variable/*` packages (devDependencies) — no CDN fonts.

Tailwind's `fontWeight` scale is redefined to the weight vars, so text styling must use `.type-*` size classes plus `font-medium`/`font-semibold`/`font-bold` — never raw `text-sm`-style size utilities or numeric `font-[...]` weights.

A separate user size slider (`FontSizeStore.svelte.ts`, localStorage `hearth-font-size`) sets `--font-user-scale` (0.9–1.3) inline on `<html>`; `app.css` multiplies every size clamp by `--scale` = preset `--font-scale` × `--font-user-scale`.

### Testing (xUnit)

- Each service that gets tests has a matching `<Service>.Tests/` project in `services/`
- Test projects use `Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Web`)
- HTTP-backed services use a `FakeHttpMessageHandler` test helper (see `Quote.Tests/Helpers/`)
- Test method naming: `Method_Scenario_ExpectedOutcome`
- Add each new test project to `Hearth.slnx`
- Use the `/write-unit-tests` skill to scaffold a new test project
