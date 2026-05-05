# Hearth

A calm, self-hosted home dashboard designed to look beautiful and recede into the room rather than demand attention. It currently ships as a web dashboard showing art, weather, music, and daily tasks. The long-term target is an always-on e-paper frame (Raspberry Pi).

## Pages

| Route | What it shows |
|-------|---------------|
| `/` | Schedule — upcoming tasks, countdown events, moon phase, current weather + forecast, now playing |
| `/calendar` | Month grid with per-day task lists and a detail modal |
| `/ambient` | Fullscreen photo slideshow at a configurable cadence |
| `/settings` | Theme picker, photo cadence and categories, attribution toggle |

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose v2.22+
- [.NET 10 SDK](https://dotnet.microsoft.com/download) *(for running the backend locally without Docker)*
- [Node.js 20+](https://nodejs.org/) *(for running the frontend locally without Docker)*

## Configuration

Services that need secrets or location data use a per-service `.env` file. Create these before starting:

**Weather** (`services/Weather/.env`):
```
LATITUDE=40.7128
LONGITUDE=-74.0060
```

**Spotify** (`services/Spotify/.env`):
```
SPOTIFY_CLIENT_ID=...
SPOTIFY_CLIENT_SECRET=...
SPOTIFY_REDIRECT_URI=http://127.0.0.1:8083/spotify/callback
```

See [`docs/SOFTWARE-DESIGN.md`](docs/SOFTWARE-DESIGN.md) for full details on each service's variables.

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
# Backend — weather service (requires services/Weather/.env with LATITUDE and LONGITUDE)
cd services/Weather
dotnet run
```

```bash
# Backend — Spotify service (optional; requires services/Spotify/.env)
cd services/Spotify
dotnet run
```

```bash
# Backend — Photos service (optional; requires services/Photos/.env with UNSPLASH_ACCESS_KEY)
cd services/Photos
dotnet run
```

```bash
# Frontend
cd frontend
npm install   # first time only
npm run dev
```

Vite proxies `/tasks` → `http://localhost:8081`, `/weather` → `http://localhost:8082`, `/spotify` → `http://localhost:8083`, and `/photos` → `http://localhost:8084`, so no CORS configuration is needed. Open [http://localhost:5173](http://localhost:5173).

## Tech Stack

| Layer     | Technology                                              |
|-----------|---------------------------------------------------------|
| Frontend  | SvelteKit 2, Svelte 5 (runes), Tailwind CSS v3, Lucide Svelte |
| Backend   | .NET 10, ASP.NET Core Minimal APIs, SQLite              |
| Proxy     | Caddy 2                                                 |
| Infra     | Docker Compose                                          |

See [`docs/SOFTWARE-DESIGN.md`](docs/SOFTWARE-DESIGN.md) for architecture decisions and [`docs/PRODUCT.md`](docs/PRODUCT.md) for the product vision.
