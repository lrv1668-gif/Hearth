# Hardware — Putting Hearth in a Frame

This document covers what to buy and what to build to get Hearth running on a physical, wall-mounted e-paper frame, per the vision in `PRODUCT.md`. It complements `PRODUCT.md` (product decisions) and `SOFTWARE-DESIGN.md` (implemented architecture) — nothing described here as future work belongs in `SOFTWARE-DESIGN.md` until it's actually built.

## The key decision: two devices, two power budgets

Don't treat "the frame" as one device. Split it into:

1. **The host** — runs the full Docker Compose stack (10 backend services + frontend, soon an 11th Display service). Its power draw is dominated by running Docker/.NET continuously, not by where it physically sits. Hearth is local-first and self-hosted, so the host can live anywhere on the LAN — a closet, a shelf, wherever — it does not need to be inside the frame.
2. **The frame client** — the small board physically mounted behind the e-paper panel. It does almost no work: wake on a timer, fetch a pre-rendered bitmap over HTTP, push it to the panel over SPI, sleep. Its power draw is dominated by radio + refresh duty cycle, not compute.

Conflating these two is how you end up with a Raspberry Pi 5 (~2.7–3W idle) mounted in the frame. Splitting them lets the frame client run at a fraction of a watt, with battery operation genuinely viable, while the host runs on whatever's convenient.

## What to buy

### Host (runs the Docker Compose stack)

| Option | Idle power | Notes |
| --- | --- | --- |
| Raspberry Pi 4 or 5 (4–8 GB) | ~3–8W under Docker load | All current Dockerfiles use multi-arch base images (`mcr.microsoft.com/dotnet/sdk:10.0`/`aspnet:10.0`, `node:26-alpine`) — arm64 needs no image changes. **Verify before buying:** if the Display service renders via headless Chromium (see below), confirm Playwright/Puppeteer's bundled Chromium has a working arm64 build. |
| x86 mini PC (e.g. Intel N100 class) | ~6–10W | Slightly higher idle draw but sidesteps the arm64-Chromium question entirely; more headroom for 11 containers. |

Either way, prefer an SSD over microSD (if Pi) for reliability under constant SQLite writes from multiple services — not a performance concern, a wear-leveling one.

### Frame client (the board behind the panel)

Minimize this one. Both options below use the same panel technology — E Ink Spectra 6, 6-color, 1600×1200, SPI — which matches `PRODUCT.md`'s 11–13" color spec:

- **Inkplate 13SPECTRA** (Soldered Electronics) — recommended starting point. All-in-one: 13.3" Spectra 6 panel + onboard ESP32-S3 driver + optional case/battery (3000 mAh Li-ion) in a single purchase. Sleep current ~22 µA — battery operation is genuinely viable, meaning no cable needs to reach the frame at all. Firmware is C/C++ (Arduino-compatible), not .NET — that's fine, see "What to code" below for why.
- **Waveshare 13.3" E-Paper HAT+ (E6/Spectra 6) + Raspberry Pi Zero 2 W** — same panel, separate driver HAT and compute board. Pi Zero 2 W idles ~0.6–0.7W. Keeps the client on Linux/Python (Waveshare ships Python drivers) — easier to debug than embedded C, at the cost of higher idle draw than the Inkplate and a second cable/power source to run.

Fall back to the Waveshare + Zero 2 W path only if the Inkplate firmware work turns out to be a blocker.

*Selection criteria that matter more than any specific SKU (lineups and prices drift — verify current products on soldered.com / waveshare.com before ordering):*

- Panel diagonal: 11–13" (readable from 3–4m per `PRODUCT.md`'s Daily Mode spec)
- Palette: 6-color Spectra (or 7-color ACeP) — richer than 2-bit grayscale, matches `PRODUCT.md`'s Color Mode
- Native resolution and interface: SPI, resolution known ahead of time so the Display service can dither to it exactly
- Whether a driver board/HAT is included or a separate purchase
- Driver library language — determines what the frame-client firmware looks like

### Enclosure

- Shadow-box-style frame, deep enough for the panel + driver board + FPC ribbon clearance. The panel ships as bare glass with a fragile ribbon cable — size the mat cutout to the panel's active area and leave slack where the ribbon exits.
- **No front glazing/glass.** Glare is exactly what e-paper is being bought to avoid; a bare or matte-protected panel keeps the "looks like paper" effect.
- Wall mount: French cleat or picture-hanger hardware sized to the enclosure's actual loaded weight (board + battery, if used).

### Physical input — decided: none in v1

`PRODUCT.md` left "does the frame have any physical controls" open, and it needs to be settled before the enclosure is built (a button or PIR sensor needs a hole cut). Decision: **no physical input in v1.** The phone-as-remote flow already covers every interaction `PRODUCT.md` describes (mode toggle, refresh trigger, task entry). Revisit in a v2 enclosure once the render/display pipeline is proven — see `PRODUCT.md`'s Phase 2 "frame tap" concept.

## What to code

In dependency order — panel choice gates the dither target, so it comes first (done above: Spectra 6, 1600×1200):

1. **Frame-specific render route (frontend)** — not a screenshot of the existing `/?kiosk=1` view, which is built for a backlit screen with 18 color themes and an arbitrary viewport. E-paper needs a layout fixed to the panel's native resolution using a fixed, print-safe palette. `CLAUDE.md`'s `--font-scale` 0.95–1.08 "e-paper caption legibility floor" guardrail anticipated this but it was never built. Reuses the existing Ambient/Daily mode components and theming system — a new route + a Spectra-safe theme variant, not a new rendering stack.

2. **Display service (new backend service)** — port `8090` is the next free slot (current services run 8081–8089, 8091). Responsibilities:
   - On a configurable schedule, render the frame route above to a bitmap. Recommended: headless Chromium (e.g. via Playwright) screenshotting the SvelteKit route at the panel's resolution — reuses all existing widget/layout/theme work instead of re-implementing layout in a second renderer. This is why the arm64-Chromium check under "Host" above matters.
   - Dither the screenshot to the panel's actual 6-color palette (e.g. Floyd–Steinberg via `ImageSharp`).
   - Expose `GET /display/frame` (or similar) returning the dithered bitmap for the frame client to pull.
   - Evaluate once built whether it needs `IDatabase` at all, or just caches the last-rendered frame in memory/disk.
   - Once built: add to `docker-compose.yml`, `Caddyfile`, `Hearth.slnx`, and update `SOFTWARE-DESIGN.md`'s service table, per `CLAUDE.md`'s "keep docs in sync" rule. Not done as part of this planning doc — the service doesn't exist yet.

3. **Frame client firmware/script** — lives outside the .NET/Compose stack since it runs on separate physical hardware; not held to `CLAUDE.md`'s backend-service conventions, which are for Compose services.
   - Inkplate path: Arduino/C++ sketch — wake on timer, HTTP GET `/display/frame`, draw via Soldered's Inkplate library, deep-sleep.
   - Waveshare + Pi path: small Python script doing the same via Waveshare's driver library, run on a systemd timer.

4. **mDNS advertising (host)** — `PRODUCT.md`'s first-run flow assumes the display shows a `hearth.local` URL on first boot. Needs Avahi (or equivalent) running on the host; not currently configured in either compose file.

## Verification

No running code yet — this is a docs/planning artifact. Before ordering hardware: re-verify current panel/board SKUs, prices, and driver-library language on the vendor sites, since this document favors durable selection criteria over pinned part numbers that will drift.
