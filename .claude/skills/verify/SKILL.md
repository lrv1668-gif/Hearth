---
name: verify
description: Build, run, and drive the Hearth frontend to verify changes at the real UI surface. Use before committing any UI-visible change, or when the user asks to check that something works, test it in the browser, take a screenshot, or confirm a widget/theme/settings/frame change renders correctly.
---

# Verifying Hearth changes

Checks (`npm run check`, `npm run test`, `npm run build` in `apps/frontend/`) are setup, not evidence — drive the real UI and look.

## Run it

- **Frontend:** `preview_start` with `name: "frontend"` (from `.claude/launch.json`) → http://localhost:5173. Without the preview tools, run `cd apps/frontend && npm run dev` in the background.
- **Backends:** `cd infra && docker compose up --build` (dev overrides in `infra/docker-compose.override.yml` expose each service on its localhost port; Vite proxies `/tasks`, `/weather`, etc. to them). The UI shell, settings page and theme/font pickers work without backends — widgets just show empty or "unavailable" states.
- **Project site:** `preview_start` with `name: "site"` → http://localhost:4173 (static `site/`, no build).

## Drive it

Use the built-in browser tools: `read_page`/`get_page_text` for content, `read_console_messages` for errors, `javascript_tool` for computed styles and state, `computer` to click, `resize_window` for mobile or dark mode, a screenshot as the proof you share.

If those tools aren't available, `puppeteer-core` installed in the scratchpad can drive any locally installed Chromium-family browser headlessly (find its path; don't write it into the repo). The repo's only Playwright install is the renderer's (`apps/renderer/`), which is for frame screenshots, not tests.

A fresh browser profile starts with empty localStorage, so defaults are exercised for free; set `localStorage` then reload to test persistence.

## Useful checks

- Color theme: `document.documentElement.dataset.theme`, localStorage `hearth-theme`
- Font theme: `document.documentElement.dataset.font`, localStorage `hearth-font`; computed `fontFamily`/`fontWeight` on `html`; `--font-scale` shows up as a font-size multiplier on `.type-*` elements
- Font size slider: `--font-user-scale` inline on `<html>`, localStorage `hearth-font-size`
- Settings pickers live at `/settings`; font preset buttons are `button[data-font]`
- Await `document.fonts.ready` before screenshots

## E-paper frame

- `/frame` renders the panel-sized page the renderer screenshots. It sets `data-ready="true"` once its data and fonts have loaded — wait for that before judging it.
- Override what Settings → Frame saved with URL params: `mode` (`daily`, `ambient`, `photo`, `countdown`, `night`, `note`), `left=a,b&right=c&split=55` (frame widget ids from `constants/frameWidgets.ts`), `theme` (a frame theme id), `palette`, `font`, `width`/`height`, `orientation`, `text` (Note mode).
- Set the viewport to the frame size (default 1600×1200) so the layout matches the panel.
- The dithered six-ink result comes from the Display service and renderer: `GET /display/preview` (Settings → Frame's live preview) and `GET /display/frame` (what the Pi pulls). These need `docker compose up` with `display` and `renderer` running.
- Only frame themes (`epaper-*`) apply on `/frame`; dashboard themes don't.

## Backend services

`dotnet build apps/services/<Service>/<Service>.csproj` and `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj`; run via `cd infra && docker compose up --build`; hit endpoints on the localhost ports from `infra/docker-compose.override.yml`. `GET /<prefix>/health` shows each service's health (also listed in Settings → Diagnostics).

## Renderer

`cd apps/renderer && npm test` (`node --test`) covers dithering, rotation, the render queue and time zones.
