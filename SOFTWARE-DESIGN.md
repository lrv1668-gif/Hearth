# Software Design

## Overview

Hearth is a calm, self-hosted home dashboard. The architecture is a set of small, independently deployable services behind a single reverse proxy, consumed by a SvelteKit frontend.

## Frontend

### SvelteKit + Svelte 5 + Tailwind CSS v3

- Lightweight runtime, well-suited for an always-on ambient display
- Svelte 5 runes syntax (`$state`, `$effect`, `$props`) for fine-grained reactivity
- Tailwind for utility-first styling with a muted, stone-palette aesthetic
- In development, Vite proxies `/tasks` → `http://localhost:8081`; in production, Caddy handles the same routing

## Backend Services

Each domain is a small, self-contained **ASP.NET Core 10 Minimal API** service backed by **SQLite**.

| Service       | Port | Responsibility                          |
|---------------|------|-----------------------------------------|
| `tasks`       | 8081 | Task CRUD                               |
| `weather`     | 8082 | Polls weather API, caches results       |
| `music`       | 8083 | Wraps Spotify / Last.fm                 |
| `plants`      | 8084 | Watering schedules and reminders        |
| `art`         | 8085 | Rotates artwork from local files or APIs|

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite via `Microsoft.Data.Sqlite`:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service under `/data`.

JSON responses use `JsonNamingPolicy.SnakeCaseLower` so property names match frontend conventions (e.g. `created_at`).

## Routing

**Caddy** acts as the reverse proxy and TLS terminator. URL-prefix routing maps paths to services:

```text
/tasks/*   → tasks:8081
/          → frontend:3000
```

## Infrastructure

**Docker Compose** orchestrates all services locally. Each service has its own `Dockerfile` using a multi-stage build to keep images small.

## Real-time Updates

**SSE (Server-Sent Events)** will be used for pushing display refreshes to the frontend. One-way push is simpler and more reliable than WebSockets for an ambient screen that only reads data.

## Directory Structure

```text
Hearth/
├── frontend/               # SvelteKit app
│   └── src/
│       ├── routes/
│       └── lib/
│           ├── api.ts
│           └── components/
├── services/
│   ├── tasks/              # ASP.NET Core 10 Minimal API
│   ├── weather/
│   ├── music/
│   ├── plants/
│   └── art/
├── docker-compose.yml
├── Caddyfile
└── SOFTWARE-DESIGN.md
```
