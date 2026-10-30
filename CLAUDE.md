# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Stack-specific conventions live next to the code and load when you work there: [`apps/frontend/CLAUDE.md`](apps/frontend/CLAUDE.md) (Svelte, themes, fonts), [`apps/services/CLAUDE.md`](apps/services/CLAUDE.md) (.NET services) and [`tests/CLAUDE.md`](tests/CLAUDE.md) (xUnit).

## Skills

Use the project skills in `.claude/skills/` for these workflows — they hold the step-by-step file lists:

- **`hearth-widget`** — adding a dashboard or e-paper frame widget
- **`hearth-service`** — adding a backend service (project, Compose, Caddy, Vite proxy, CI image, health checks, docs)
- **`write-unit-tests`** — xUnit tests for a backend service
- **`verify`** — driving the real UI before committing a UI-visible change

## Keep docs in sync

- A service added or changed → `docs/SOFTWARE-DESIGN.md`.
- Product decisions or feature scope change → `docs/PRODUCT.md`.
- A theme added or removed → `docs/SOFTWARE-DESIGN.md` → Themes.
- A widget added or removed → the `/` (and, for frame widgets, `/frame`) row of the Pages table in `README.md`, and the relevant section of `docs/SOFTWARE-DESIGN.md`.
- The GitHub Pages site in `site/` repeats some of this — theme/palette/typeface counts, the widget grid, the host's service list, and the status table in `build.html`. When those change, or a planned piece gets built, update the site too.

## Project Overview

Hearth is a calm, self-hosted home dashboard. Its primary target is an always-on e-paper frame — a Pimoroni Inky Impression 13.3" (Spectra 6) on a Raspberry Pi thin client that pulls a frame the host renders (`docs/HARDWARE.md`) — though it also runs in a browser. Plant care is planned in `docs/PRODUCT.md` but not built — there is no plants service.

- Domain data and math (third-party calls, caching, astronomy, recurrence) belong in a backend service; the frontend is a thin renderer.
- Everything is same-origin behind Caddy (`/tasks`, `/weather`, …), so services set no CORS. Services, ports and endpoints are listed in `docs/SOFTWARE-DESIGN.md` → Backend Services.
- `apps/renderer/` is a Node + Playwright sidecar for Display: it screenshots `/frame` and dithers it to the panel's six inks.
- `infra/` holds Compose, Caddy and host deploy — run `docker compose` from there. Dozzle (`/logs`) is opt-in via `COMPOSE_PROFILES=logs`; `infra/dozzle/` holds its login and is gitignored.
- `site/` is a static GitHub Pages showcase with no build step; its screenshots use demo data, never real tasks, calendar or location.
- `.claude/launch.json` defines the preview servers: `frontend` (:5173) and `site` (:4173).

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
