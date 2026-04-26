# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, plant care, music, and daily tasks into a single quiet display. The primary target is an always-on, ambient display (e.g. a digital picture frame), though it is also accessible via web browser.

## Tech Stack

- **Frontend:** SvelteKit 2 + Svelte 5 (runes syntax) + Tailwind CSS v3 + Lucide Svelte (`@lucide/svelte`), built with Vite
- **Services:** .NET 10, ASP.NET Core Minimal APIs, SQLite (isolated to `services/Data` — service projects do not reference `Microsoft.Data.Sqlite` directly)
- **Proxy:** Caddy 2
- **Infra:** Docker Compose

See `SOFTWARE-DESIGN.md` for full architecture decisions.

## Directory Structure

```
frontend/                    # SvelteKit app
services/
  Data.Abstractions/         # Shared interfaces — IDatabase, DbCommandExtensions (no SQLite dep)
  Data/                      # SQLite implementation of IDatabase
  Tasks/                     # ASP.NET Core 10 Minimal API, port 8081
docker-compose.yml
Caddyfile
```

## Running Locally (dev, no Docker)

**Tasks service:**
```bash
cd services/Tasks
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install        # first time only
npm run dev
```

Vite proxies `/tasks` → `http://localhost:8081`, so no CORS configuration needed during dev. Open http://localhost:5173.

## Running in Docker

**Development (live reload via Vite HMR):**
```bash
docker compose watch
```
`docker-compose.override.yml` is auto-merged and swaps the frontend for the Vite dev server (`Dockerfile.dev`). Changes to `frontend/src/` and `frontend/static/` sync instantly; changes to `package.json`, `svelte.config.js`, or `vite.config.ts` trigger a rebuild.

**Production:**
```bash
docker compose -f docker-compose.yml up --build
```

## Conventions

- Svelte components use Svelte 5 runes: `$state`, `$effect`, `$props`, `{@render}`
- API calls live in `frontend/src/lib/api.ts` — all paths are relative (e.g. `/tasks`)
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `tasks.db`)
- JSON uses snake_case naming (`JsonNamingPolicy.SnakeCaseLower`) — keep record property names PascalCase in C#
- Service stores (e.g. `TaskStore`) take `IDatabase` from `Data.Abstractions` via `[FromKeyedServices("key")]` — never a concrete `Database` or SQLite types directly
- `Program.cs` in each service registers the concrete `Database` as `IDatabase` keyed by service name: `AddKeyedSingleton<IDatabase>("key", (_, _) => new Database(dbPath))`
- New database-backed services: reference `Data.Abstractions` for `IDatabase`, reference `Data` only in `Program.cs` for the concrete registration; never add `Microsoft.Data.Sqlite` as a direct dependency
