# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Agent Instructions

Keep documentation in sync with code at all times:

- When a new service is added or changed, update `SOFTWARE-DESIGN.md` and the directory structure below.
- When product decisions or feature scope changes, update `docs/PRODUCT.md`.
- When a theme is added or removed, update `SOFTWARE-DESIGN.md` (theme list) and the theme list below.
- When a new service needs environment variables, create `services/<Service>/.env`, add `env_file` to `docker-compose.yml` (alongside any static `environment:` values), and add a `LogError` call in the endpoint when required vars are missing.
- When adding a new backend service: add it to `docker-compose.yml`, `Caddyfile`, and follow the service pattern below. Do not add `Microsoft.Data.Sqlite` as a direct dependency.
- Each frontend data type gets its own `<Type>Store.ts` file — do not combine stores.
- Use `[FromKeyedServices("key")]` to inject `IDatabase` into stores — never inject the concrete `Database` class.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, plant care, music, and daily tasks into a single quiet display. The primary target is an always-on, ambient display (e.g. a digital picture frame), though it is also accessible via web browser.

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
docker-compose.yml
docker-compose.override.yml  # dev overrides — auto-merged by Compose
Caddyfile
```

## Conventions

### Backend (C# / ASP.NET Core)

- `Program.cs` in each service registers the concrete `Database` as `IDatabase` keyed by service name: `AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath))`
- Service stores (e.g. `TaskStore`) inject `IDatabase` via `[FromKeyedServices("key")]` — never a concrete `Database` or SQLite types directly
- New database-backed services: reference `Data.Abstractions` for `IDatabase`, reference `Data` only in `Program.cs` for the concrete registration; never add `Microsoft.Data.Sqlite` as a direct dependency
- JSON uses snake_case naming (`JsonNamingPolicy.SnakeCaseLower`) — keep record property names PascalCase in C#
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `<service>.db`)
- Log a `LogError` when a required environment variable is missing, before returning a 503

### Frontend (Svelte / TypeScript)

- Svelte components use Svelte 5 runes: `$state`, `$effect`, `$props`, `{@render}`
- API calls live in `frontend/src/lib/api.ts` — all paths are relative (e.g. `/tasks`)
- Each data type has its own store file: `TaskStore.ts`, `SpotifyStore.ts`, etc. — never combine into a shared `stores.ts`

### Themes

Themes are defined in two places — both must be updated together:

1. `frontend/src/app.css` — CSS custom property block `[data-theme="id"] { ... }`
2. `frontend/src/lib/themes.ts` — entry in the `themes` array

Current themes: `stone`, `linen`, `forest`, `dusk`, `ash`, `chalk`, `terracotta`

Each theme defines: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, and `color-scheme` (for native inputs).
