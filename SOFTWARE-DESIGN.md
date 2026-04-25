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
- In development, Vite proxies `/tasks` → `$TASKS_URL` (defaults to `http://localhost:8081`); in production, Caddy handles the same routing

### Themes

Five built-in themes (stone, linen, forest, dusk, terracotta) are applied via CSS custom properties on `document.documentElement`. The active theme is persisted in `localStorage`.

## Backend Services

Each domain is a small, self-contained **ASP.NET Core 10 Minimal API** service backed by **SQLite**.

| Service   | Port | Status      | Responsibility          |
|-----------|------|-------------|-------------------------|
| `tasks`   | 8081 | Implemented | Task CRUD with due dates |
| `weather` | 8082 | Planned     | Polls weather API, caches results |
| `music`   | 8083 | Planned     | Wraps Spotify / Last.fm |
| `plants`  | 8084 | Planned     | Watering schedules and reminders |
| `art`     | 8085 | Planned     | Rotates artwork from local files or APIs |

**Why .NET 10:** Required constraint. ASP.NET Core Minimal APIs provide a clean, low-ceremony HTTP layer that maps well to small single-domain services.

**Why SQLite via `Microsoft.Data.Sqlite`:** Zero external dependencies, single-file persistence, more than sufficient at this scale. One database file per service at `$DB_PATH` (defaults to `tasks.db`).

JSON responses use `JsonNamingPolicy.SnakeCaseLower` so property names match frontend conventions (e.g. `created_at`, `due_date`).

## Routing

**Caddy** acts as the reverse proxy and TLS terminator. URL-prefix routing maps paths to services:

```text
/tasks/*  →  tasks:8081
/         →  frontend:3000
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
│   │   │   └── +page.svelte
│   │   └── lib/
│   │       ├── api.ts
│   │       └── components/
│   │           ├── Calendar.svelte
│   │           ├── TaskList.svelte
│   │           ├── TaskModal.svelte
│   │           └── ThemeSwitcher.svelte
│   ├── Dockerfile              # production (multi-stage, node adapter)
│   ├── Dockerfile.dev          # development (vite dev server)
│   └── vite.config.ts
├── services/
│   └── tasks/                  # ASP.NET Core 10 Minimal API
│       ├── Program.cs
│       ├── TaskStore.cs
│       └── Dockerfile
├── docker-compose.yml          # production
├── docker-compose.override.yml # development (auto-merged)
├── Caddyfile
├── README.md
├── PRODUCT.md
└── SOFTWARE-DESIGN.md
```
