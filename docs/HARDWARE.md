# Hardware — Putting Hearth in a Frame

This document covers what to buy and what to build to get Hearth running on a physical, wall-mounted e-paper frame, per the vision in `PRODUCT.md`. It complements `PRODUCT.md` (product decisions) and `SOFTWARE-DESIGN.md` (implemented architecture) — nothing described here as future work belongs in `SOFTWARE-DESIGN.md` until it's actually built.

**Chosen frame hardware:** a Pimoroni **Inky Impression 13.3"** (E Ink Spectra 6, 1600×1200) driven by a **Raspberry Pi Zero 2 W**, running a small Python client of our own built on Pimoroni's [`inky`](https://github.com/pimoroni/inky) library.

## The key decision: two devices, two power budgets

Don't treat "the frame" as one device. Split it into:

1. **The host** — runs the full Docker Compose stack (10 backend services + frontend, soon an 11th Display service). Its power draw is dominated by running Docker/.NET continuously, not by where it physically sits. Hearth is local-first and self-hosted, so the host can live anywhere on the LAN — a closet, a shelf, wherever — it does not need to be inside the frame.
2. **The frame client** — the Pi Zero 2 W mounted on the back of the Inky Impression. It does almost no work: fetch a pre-rendered bitmap over HTTP, push it to the panel via `inky`, listen for button presses, idle. A thin client by necessity, not just preference — with 512 MB of RAM the Zero 2 W can't run the Compose stack or headless Chromium, so all rendering stays on the host.

Conflating these two is how you end up with a Raspberry Pi 5 (~2.7–3W idle) mounted in the frame. Splitting them keeps the frame at well under a watt while the host runs on whatever's convenient.

## What to buy

### Host (runs the Docker Compose stack)

| Option | Idle power | Notes |
| --- | --- | --- |
| Raspberry Pi 4 or 5 (4–8 GB) | ~3–8W under Docker load | All Dockerfiles use multi-arch base images (`mcr.microsoft.com/dotnet/sdk:10.0`/`aspnet:10.0-noble-chiseled-extra`, `node:26-alpine`, `caddy:2.11`), and the backend publishes for the build's `TARGETARCH` — arm64 needs no image changes. **Verify before buying:** if the Display service renders via headless Chromium (see below), confirm Playwright/Puppeteer's bundled Chromium has a working arm64 build. |
| x86 mini PC (e.g. Intel N100 class) | ~6–10W | Slightly higher idle draw but sidesteps the arm64-Chromium question entirely; more headroom for 11 containers. |

Either way, prefer an SSD over microSD (if Pi) for reliability under constant SQLite writes from multiple services — not a performance concern, a wear-leveling one.

### Frame client (the board behind the panel)

- **Pimoroni Inky Impression 13.3"** — E Ink Spectra 6 (6-color), 1600×1200 (~150 PPI), SPI. Includes the driver board, four tactile buttons (A–D) along one edge, and a 40-pin header on the back that a Pi mounts onto directly.
- **Raspberry Pi Zero 2 W** — buy the "WH" variant with a pre-soldered header so it plugs straight onto the Inky. Idles ~0.6–0.7W.
- **microSD card** (Raspberry Pi OS Lite) and a **5V USB power supply** with a cable long enough to reach the frame.

Power: unlike an ESP32 board, the Zero 2 W has no deep-sleep mode, so the frame needs a permanent USB power run — plan the cable route when hanging it. A battery/UPS HAT is possible but unresearched; don't design the enclosure around it.

Why this combination: the 13.3" panel meets `PRODUCT.md`'s 11–13" spec (readable from 3–4m in Daily Mode), Spectra 6 supports Color Mode, the resolution is fixed and known so the Display service can dither to it exactly, the driver board and buttons are included, and the client stays on Linux/Python — easy to debug over SSH, and `inky` handles the panel protocol. *Lineups and prices drift — verify the current product on pimoroni.com before ordering.*

(Considered and dropped: Soldered's Inkplate 13SPECTRA, an all-in-one ESP32 board with real battery viability but C/C++ firmware, and Waveshare's 13.3" E-Paper HAT+, the same panel class without built-in buttons.)

### Enclosure

- Shadow-box-style frame, deep enough for the panel + Inky driver board + the Zero 2 W on its rear header. The panel is fragile glass — size the mat cutout to the active area and don't let the frame press on the panel edges.
- **Buttons must stay reachable:** the A–D buttons sit on one edge of the Inky board, so leave a side cutout or fit extended actuators through the frame.
- A cable exit for USB power.
- Leave room and a cable path for a presence sensor (see below), even if one isn't fitted at first.
- **No front glazing/glass.** Glare is exactly what e-paper is being bought to avoid; a bare or matte-protected panel keeps the "looks like paper" effect.
- Wall mount: French cleat or picture-hanger hardware sized to the enclosure's actual loaded weight.

### Physical input — decided: the Inky's four buttons in v1

The Inky Impression ships with four buttons, so physical input costs nothing beyond keeping them reachable in the enclosure. Proposed starting mapping (revisit once the client exists):

| Button | Action |
| --- | --- |
| A | Refresh now |
| B | Toggle Ambient ↔ Daily |
| C | Toggle Color ↔ B&W |
| D | Reserved — e.g. show host URL / status |

The phone remains the primary remote (task entry, settings, schedules); buttons cover the quick actions. A tap-anywhere "frame tap" stays a v2 idea — see `PRODUCT.md`.

### Presence sensor — optional, v1.5

Goal: *refresh when nobody's watching* (see `PRODUCT.md`'s "Presence-aware refresh" open question). A full Spectra 6 refresh flickers for tens of seconds, so rotating Ambient photos while the room is empty — and holding the image steady while someone is there — keeps the flicker mostly unseen.

| Sensor | Interface | Trade-off |
| --- | --- | --- |
| **mmWave radar (e.g. HLK-LD2410)** — recommended | UART | Detects stationary people (someone reading on the couch), not just motion. Senses through thin wood, so no hole in the frame. |
| PIR module | GPIO | Cheapest; motion only, so a still person reads as "empty". Needs a lens hole. |
| Ambient light (e.g. BH1750) | I2C | Complements either: lights off → skip refreshes entirely. Can share the I2C bus the Inky already uses. |

No camera — a privacy cost the feature doesn't justify. Before wiring anything, confirm which GPIO pins the 13.3" Inky uses for SPI and its buttons, and pick sensor pins that don't collide.

## What to code

In dependency order — panel choice gates the dither target, so it comes first (done above: Spectra 6, 1600×1200):

1. **Frame-specific render route (frontend)** — ✅ built: `/frame?mode=ambient|daily&palette=color|bw&theme=…&font=…&orientation=…`. A fixed 1600×1200 (or 1200×1600) layout — not a screenshot of `/?kiosk=1`, which is built for a backlit screen and an arbitrary viewport — themed with the `epaper-*` palettes, which use only Spectra 6 inks and dither-safe two-ink mixes. Settings → Frame picks the theme, typeface and orientation, with a live preview and the resulting URL. It sets `data-ready="true"` on its root when the page is ready to capture. See `SOFTWARE-DESIGN.md` → "Frame route".

2. **Display service (new backend service)** — port `8090` is the next free slot (current services run 8081–8089, 8091). Responsibilities:
   - On a configurable schedule, render the frame route above to a bitmap (wait for `[data-ready="true"]`). Recommended: headless Chromium (e.g. via Playwright) screenshotting the SvelteKit route at the panel's resolution — reuses all existing widget/layout/theme work instead of re-implementing layout in a second renderer. This is why the arm64-Chromium check under "Host" above matters.
   - Capture at the frame's `orientation` (`landscape` 1600×1200, or `portrait` 1200×1600 for a frame hung on its side) and rotate a portrait capture 90° to the panel's native 1600×1200 before dithering.
   - Dither the screenshot to the six measured, white-balanced Spectra 6 inks — the same values the `epaper-*` palettes in `frontend/src/themes.css` are built from: black `#000000`, white `#ffffff`, yellow `#ffff6e`, red `#f16f74`, blue `#5e5b91`, green `#5a8d6c` (derived from `inky`'s measured palette, scaled so paper white is `#fff`). Pick the method by mode — tested on real frame screenshots:
     - **Daily → ordered (4×4 Bayer) dithering.** The layout is flat UI: pure-ink text and lines map exactly, and the palette's tints (cream, butter, amber — mixes of two inks close in lightness) become even, regular textures. Floyd–Steinberg instead turns flat tints into wormy streaks and sprays stray red/blue dots around text.
     - **Ambient → Floyd–Steinberg.** Error diffusion suits photographs; Bayer crosshatching looks mechanical on them. The ambient card is therefore pure ink only (tints streak under error diffusion).
   - Expose `GET /display/frame` returning the bitmap, with an `ETag` (honoring `If-None-Match`) so the client can poll cheaply and only trigger a full panel refresh when the frame actually changed.
   - Expose the endpoints the buttons and sensor need: e.g. `POST /display/refresh`, `POST /display/mode` (Ambient/Daily, Color/B&W), and `POST /display/presence`. Presence *policy* — dwell times, quiet hours, interaction with the mode schedule — lives here, not on the client.
   - Evaluate once built whether it needs `IDatabase` at all, or just caches the last-rendered frame in memory/disk.
   - Once built: add to `docker-compose.yml`, `Caddyfile`, `Hearth.slnx`, and update `SOFTWARE-DESIGN.md`'s service table, per `CLAUDE.md`'s "keep docs in sync" rule. Not done as part of this planning doc — the service doesn't exist yet.
   - **Dithering happens on the host.** `inky`'s `set_image()` always Floyd–Steinberg-dithers RGB input, which would rule out ordered dithering for Daily. It skips dithering (`Dither.NONE`) only for a palette-mode image with exactly six colours, so the service sends the dithered frame as a 6-colour palette PNG in `inky`'s colour order (black, white, yellow, red, blue, green) and the client passes it straight through. This also makes the phone's live preview match the panel pixel-for-pixel.

3. **Frame client (Python on the Zero 2 W)** — lives outside the .NET/Compose stack since it runs on separate physical hardware; not held to `CLAUDE.md`'s backend-service conventions, which are for Compose services.
   - Raspberry Pi OS Lite, SPI and I2C enabled, `pip install inky` (`inky.auto` detects the board).
   - Runs as a long-running **systemd service** (not a timer), because it must listen for button presses (and the sensor, if fitted) between refreshes.
   - Loop: poll `GET /display/frame` with `If-None-Match`; on change, `set_image()` + `show()`. Button presses and presence changes POST to the Display service, which re-renders.
   - Draws its own local screens with PIL when there's nothing to fetch: first-boot welcome (with the host URL, per `PRODUCT.md`'s first-run flow) and "can't reach host".

4. **mDNS advertising (host)** — `PRODUCT.md`'s first-run flow assumes the display shows a `hearth.local` URL on first boot. Needs Avahi (or equivalent) running on the host; not currently configured in either compose file.

## Verification

No running code yet — this is a docs/planning artifact. Before ordering hardware: re-verify the current Inky Impression 13.3" listing (resolution, button GPIO pins, header orientation) and `inky` library support on pimoroni.com / GitHub, since this document favors durable selection criteria over pinned part numbers that will drift.
