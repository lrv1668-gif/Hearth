# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, plant care, music, and daily tasks into a single quiet display. The primary target is an always-on, ambient display (e.g. a digital picture frame), though it is also accessible via web browser.

## Tech Stack

- **Frontend:** SvelteKit 2 + Svelte 5 (runes syntax) + Tailwind CSS v3, built with Vite
- **Services:** .NET 10, ASP.NET Core Minimal APIs, SQLite via `Microsoft.Data.Sqlite`
- **Proxy:** Caddy 2
- **Infra:** Docker Compose

See `SOFTWARE-DESIGN.md` for full architecture decisions.

## Directory Structure

```
frontend/          # SvelteKit app
services/tasks/    # ASP.NET Core 10 Minimal API, port 8081
docker-compose.yml
Caddyfile
```

## Running Locally (dev)

**Tasks service:**
```bash
cd services/tasks
dotnet run
```

**Frontend:**
```bash
cd frontend
npm install        # first time only
npm run dev
```

Vite proxies `/tasks` → `http://localhost:8081`, so no CORS configuration needed during dev.

## Build & Deploy

```bash
docker compose up --build
```

## Conventions

- Svelte components use Svelte 5 runes: `$state`, `$effect`, `$props`, `{@render}`
- API calls live in `frontend/src/lib/api.ts` — all paths are relative (e.g. `/tasks`)
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `tasks.db`)
- JSON uses snake_case naming (`JsonNamingPolicy.SnakeCaseLower`) — keep record property names PascalCase in C#
