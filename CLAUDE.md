# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Stack-specific conventions live next to the code and load when you work there: [`apps/frontend/CLAUDE.md`](apps/frontend/CLAUDE.md) (Svelte, themes, fonts), [`apps/services/CLAUDE.md`](apps/services/CLAUDE.md) (.NET services) and [`tests/CLAUDE.md`](tests/CLAUDE.md) (xUnit).

## Skills

Use the project skills in `.claude/skills/` for these workflows — they hold the step-by-step file lists:

- **`hearth-widget`** — adding a dashboard or e-paper frame widget
- **`hearth-service`** — adding a backend service (project, Compose, Caddy, Vite proxy, CI image, health checks, docs)
- **`write-unit-tests`** — xUnit tests for a backend service
- **`verify`** — driving the real UI before committing a UI-visible change

## Keep docs in sync

- A service added or changed → `docs/SOFTWARE-DESIGN.md` and the directory structure below.
- Product decisions or feature scope change → `docs/PRODUCT.md`.
- A theme added or removed → `docs/SOFTWARE-DESIGN.md` → Themes.
- A widget added or removed → the `/` (and, for frame widgets, `/frame`) row of the Pages table in `README.md`, and the relevant section of `docs/SOFTWARE-DESIGN.md`.
- The GitHub Pages site in `site/` repeats some of this — theme/palette/typeface counts, the widget grid, the host's service list, and the status table in `build.html`. When those change, or a planned piece gets built, update the site too.

## Project Overview

Hearth is a calm, self-hosted home dashboard designed to be displayed on a wall-mounted frame. It aggregates art, weather, moon phase, tasks and countdowns, a calendar agenda, Spotify now-playing, news feeds, a daily quote, bird sightings, transit departures, sports scores, and almanac facts into a single quiet display. (Plant care is planned in `docs/PRODUCT.md` but not built yet — there is no plants service.) The primary target is an always-on, ambient e-paper frame — a Pimoroni Inky Impression 13.3" (Spectra 6, 1600×1200) driven by a Raspberry Pi Zero 2 W thin client that pulls a host-rendered frame (see `docs/HARDWARE.md`); the frame's panel size is configurable for other panels — though it is also accessible via web browser.

Domain data and math (third-party calls, caching, astronomy, recurrence) belong in a backend service; the frontend is a thin renderer.

## Tech Stack

- **Frontend:** SvelteKit 2 + Svelte 5 (runes syntax) + Tailwind CSS v4 + Lucide Svelte (`@lucide/svelte`), built with Vite
- **Services:** .NET 10, ASP.NET Core Minimal APIs, SQLite (isolated to `apps/services/Data`)
- **Renderer:** Node + Playwright (headless Chromium) sidecar that screenshots and dithers the frame
- **Proxy:** Caddy 2
- **Infra:** Docker Compose; images published to GHCR by CI

## Directory Structure

```text
apps/                        # Everything that runs — one folder per stack
  services/                  # .NET services + shared libraries (Hearth.slnx at the root builds them)
    Dockerfile               # Shared image for every backend service — pick one with the SERVICE build arg
    Data.Abstractions/       # Shared interfaces — IDatabase, DbCommandExtensions (no SQLite dep)
    Data/                    # SQLite implementation of IDatabase + AddSqliteDatabase() registration helper; SqliteDistributedCache (FusionCache L2)
    ServiceDefaults/         # Shared JSON defaults (AddHearthWebDefaults — no CORS: everything is same-origin via Caddy), AddHearthCache (FusionCache), ConfigRequirement env-var validation + HomeLocationClient (the home location saved in Display, env-var fallback), HearthHealth (GET /<service>/health for Settings → Diagnostics) — referenced by every service
    Tasks/                   # ASP.NET Core 10 Minimal API, port 8081
    Spotify/                 # ASP.NET Core 10 Minimal API, port 8083 — Spotify OAuth + now-playing
    Weather/                 # ASP.NET Core 10 Minimal API, port 8082 — weather fetch + cache
    Photos/                  # ASP.NET Core 10 Minimal API, port 8084 — Unsplash photo fetch + cache, local uploads + captions + photo groups, seasonal query expansion
    Rss/                     # ASP.NET Core 10 Minimal API, port 8085 — RSS/Atom feed fetch + cache
    Quote/                   # ASP.NET Core 10 Minimal API, port 8086 — ZenQuotes daily quote + cache
    Calendar/                # ASP.NET Core 10 Minimal API, port 8087 — Google Calendar OAuth + events cache
    Birds/                   # ASP.NET Core 10 Minimal API, port 8088 — eBird nearby sightings + cache
    Almanac/                 # ASP.NET Core 10 Minimal API, port 8089 — season/daylight/frost facts, computed locally (no DB)
    Trains/                  # ASP.NET Core 10 Minimal API, port 8091 — Transitland v2 departures fetch + cache
    Sports/                  # ASP.NET Core 10 Minimal API, port 8094 — ESPN live games, results and fixtures for followed teams + cache
    Display/                 # ASP.NET Core 10 Minimal API, port 8090 — shared home location/time zone, e-paper frame settings, dithered frame preview + the scheduled panel render (`GET /display/frame`, FrameRenderService)
  frontend/                  # SvelteKit app — shared controls in src/lib/components/ui/
  renderer/                  # Node sidecar for Display — headless Chromium (Playwright) screenshots /frame, dither.mjs reduces it to the 6 inks (epdoptimize + Bayer); `npm test`
tests/                       # xUnit test projects — <Service>.Tests/
  Hearth.TestUtilities/      # Shared test helpers — FakeHttpMessageHandler, TempDatabase, TempDirectory, HearthApp (endpoint tests)
infra/                       # Runs the stack: Compose, Caddy, host deploy — run `docker compose` from here
  docker-compose.yml
  docker-compose.override.yml  # dev overrides — auto-merged by Compose
  .env.example               # Compose vars for a deployed host (image prefix/tag, FRONTEND_URL) — copy to infra/.env
  Caddyfile
  deploy/                    # Host auto-update: update.sh + systemd timer (pulls CI-published GHCR images); dozzle-user.sh sets the /logs login
  dozzle/                    # Dozzle's /data bind mount (users.yml login) — gitignored except .gitkeep; Dozzle itself is opt-in (COMPOSE_PROFILES=logs)
site/                        # Static GitHub Pages showcase (no build step; deployed by .github/workflows/pages.yml) — screenshots use demo data, never real tasks/calendar/location
.claude/                     # Project skills, the Prettier hook, shared permissions, launch.json (preview servers)
```

## Commands

- Type-check frontend: `cd apps/frontend && npm run check` (runs `svelte-kit sync` + `svelte-check`)
- Frontend unit tests (vitest): `cd apps/frontend && npm run test`
- Frontend formatting: `cd apps/frontend && npm run format:check` (`npm run format` to fix; a PostToolUse hook runs Prettier on files Claude edits)
- Renderer tests: `cd apps/renderer && npm test`
- Build a backend service: `dotnet build apps/services/<Service>/<Service>.csproj`
- Run a service's tests: `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj`
- Full CI equivalent: `dotnet build Hearth.slnx && dotnet test Hearth.slnx`; `npm run format:check && npm run check && npm run test && npm run build` in `apps/frontend/`; `npm test` in `apps/renderer/`
- CI runs only the stacks a PR touches (the `changes` job's path filters in `.github/workflows/ci.yml`) and everything on pushes to `main` — a new top-level folder or root file a stack depends on needs adding to those filters
- Before committing a UI-visible change, run the `verify` skill to drive the real UI
- Run the app locally: `cd infra && docker compose up --build` (dev overrides in `infra/docker-compose.override.yml` expose service ports on localhost), then `cd apps/frontend && npm run dev` for a hot-reloading frontend — Vite's dev proxy in `apps/frontend/vite.config.ts` forwards `/tasks`, `/weather`, etc. to those ports
- Deploying: CI's `publish-images` job pushes amd64+arm64 images to GHCR on every green push to `main`; a host pulls them via `infra/deploy/update.sh` (see `docs/SOFTWARE-DESIGN.md` → Deploying to a host)

## Repo hygiene

- Never commit secrets: per-service `apps/services/<Service>/.env` files and `infra/.env` are gitignored — document vars in the matching `.env.example`.
- Keep the repo device-agnostic: no host IPs, hostnames, registries or local paths in tracked files.
- C# keeps its hand-aligned columns (record parameters, switch arms); `.editorconfig` sets only basics, so don't run `dotnet format` style fixes over existing code.
