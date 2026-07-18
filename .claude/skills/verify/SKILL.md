---
name: verify
description: Build, run, and drive the Hearth frontend to verify changes at the real UI surface.
---

# Verifying Hearth changes

## Frontend (SvelteKit)

Build/typecheck (setup, not evidence): `cd frontend && npx svelte-check --tsconfig ./tsconfig.json`, `npm run build`.

Run: `cd frontend && npm run dev` (background) → http://localhost:5173. Backends come from `docker compose up` (dev overrides expose service ports; Vite proxies `/tasks`, `/weather`, etc.). The UI shell, settings page, and theme/font pickers all work without backends — widgets just show empty states.

Drive: no Playwright in the repo. `puppeteer-core` (install in scratchpad) driving Brave works headlessly:
`executablePath: '/Applications/Brave Browser.app/Contents/MacOS/Brave Browser'`, `args: ['--headless=new']`, viewport 1280×900 (e-paper target width is 1200px). Each launch gets a fresh temp profile — localStorage starts empty, so defaults are exercised for free; set `localStorage` then `page.reload()` to test persistence paths.

Useful checks:
- Color theme: `document.documentElement.dataset.theme`, localStorage `hearth-theme`
- Font theme: `document.documentElement.dataset.font`, localStorage `hearth-font`, computed `fontFamily`/`fontWeight` on `html`; `--font-scale` shows up as a font-size multiplier on `.type-*` elements
- Settings pickers live at `/settings` (Display section); font preset buttons are `button[data-font]`
- Await `document.fonts.ready` before screenshots

## Backend services

`cd services && dotnet build <Service>/<Service>.csproj`; run via `docker compose up --build`; hit endpoints on the localhost ports from `docker-compose.override.yml`.
