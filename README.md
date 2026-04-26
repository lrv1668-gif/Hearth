# Hearth

A calm, self-hosted home dashboard for a wall-mounted frame — art, weather, plants, music, and daily tasks in one quiet display. Designed to look beautiful and recede into the room rather than demand attention.

The primary target is an always-on e-paper frame (Raspberry Pi), but it is also fully accessible via web browser.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose v2.22+
- [.NET 10 SDK](https://dotnet.microsoft.com/download) *(for running the backend locally without Docker)*
- [Node.js 20+](https://nodejs.org/) *(for running the frontend locally without Docker)*

## Running in Docker

### Development (with live reload)

```bash
docker compose watch
```

Compose automatically merges `docker-compose.override.yml` (dev overrides) with `docker-compose.yml`. The frontend runs the Vite dev server — file changes under `frontend/src/` and `frontend/static/` sync into the container and trigger HMR instantly. Changes to `package.json`, `svelte.config.js`, or `vite.config.ts` trigger a container rebuild.

Open [http://localhost](http://localhost).

### Production

```bash
docker compose -f docker-compose.yml up --build
```

Builds the frontend as a static bundle served by Node, and runs all backend services behind Caddy.

## Running Locally (without Docker)

Run each piece in its own terminal:

```bash
# Backend — tasks service
cd services/Tasks
dotnet run
```

```bash
# Frontend
cd frontend
npm install   # first time only
npm run dev
```

Vite proxies `/tasks` → `http://localhost:8081`, so no CORS configuration is needed. Open [http://localhost:5173](http://localhost:5173).

## Tech Stack

| Layer     | Technology                                              |
|-----------|---------------------------------------------------------|
| Frontend  | SvelteKit 2, Svelte 5 (runes), Tailwind CSS v3, Lucide Svelte |
| Backend   | .NET 10, ASP.NET Core Minimal APIs, SQLite              |
| Proxy     | Caddy 2                                                 |
| Infra     | Docker Compose                                          |

See [`SOFTWARE-DESIGN.md`](SOFTWARE-DESIGN.md) for architecture decisions and [`PRODUCT.md`](PRODUCT.md) for the product vision.
