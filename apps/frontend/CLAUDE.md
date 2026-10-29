# Frontend conventions (SvelteKit / Svelte 5)

Loaded when working under `apps/frontend/`. Project-wide guidance is in the root [`CLAUDE.md`](../../CLAUDE.md); new widgets go through the `hearth-widget` skill.

## Components and data

- Svelte components use Svelte 5 runes: `$state`, `$derived`, `$effect`, `$props`, `{@render}`
- Build UI from the shared components in `src/lib/components/ui/` (`Button`, `IconButton`, `TextInput`/`TextArea`/`Select` + `Field`, `ChoiceChips`/`Chip`, `SegmentedControl`, `DayPicker`, `Toggle`/`ToggleRow`, `Disclosure`, `Modal`, `ListRow`, `InlineMessage`, …) and the `.eyebrow` class for small-caps headings — don't hand-roll button, input or dialog class strings. The full list is in `docs/SOFTWARE-DESIGN.md` → Shared UI components.
- Widget visibility and layout settings use the shared `WidgetLayoutEditor` (columns plus a Hidden tray) for both the dashboard and the frame — keep the two screens consistent rather than adding bespoke editors.
- Each data type gets its own `src/lib/stores/<Type>Store.svelte.ts` (the `.svelte.ts` suffix is required for runes) — never combine stores into a shared `stores.ts`.
- API calls live in `src/lib/api.ts` — all paths are relative (e.g. `/tasks`), and every backend call goes through an `api.<service>` namespace on `ApiClient` — no standalone `fetch` helpers exported from `api.ts`.
- `ApiClient` has `get<T>(url, { timeoutMs })` (null on any failure), `getSaved<T>` (null on 204, undefined on failure) and `post<T>`/`put<T>`/`patch`/`del` for JSON writes — don't add overloads; use `tryFetch(url, { method: 'POST', timeoutMs })` (null instead of throwing) for body-less POST calls and other one-off status handling.
- Widgets that show upstream data refresh with `poll(load, ms)` from `utils.ts` (returned from `onMount`); stores keep the last good data when a refresh fails.
- `{@html}` with any external content (e.g. calendar descriptions) must use `DOMPurify.sanitize()` first.

## E-paper frame widgets

Registered in `src/lib/constants/frameWidgets.ts`, they take a `lines` budget (see `allocateLines` in `lib/frame.ts`) and add their data loader to `widgetLoaders` in `routes/frame/+page.svelte`. When the frame shows the same content as a dashboard widget, don't copy it: put the markup in a shared view in `components/widgets/views/` (data in via props, an optional `lines` budget, sizes from `type-*`/`icon-*`/em units and `--stroke` so the frame's overrides scale it) and keep the dashboard widget a thin wrapper that loads and polls. Only frame-specific designs go in `components/frame/widgets/`. Only widgets that stay correct between scheduled refreshes belong on the frame.

## Styling

- Colors come from theme tokens with Tailwind v4's shorthand: `text-(--text-2)`, `bg-(--surface)`, `border-(--border)` — never hardcoded colors or the older `text-[var(--…)]` form.
- Tailwind's `fontWeight` scale is redefined to the weight vars, so text styling must use `.type-*` size classes plus `font-medium`/`font-semibold`/`font-bold` — never raw `text-sm`-style size utilities or numeric `font-[...]` weights.
- Use `--text-3`/`--text-4` only for icons and decoration, never for text people need to read.

## Themes

Defined in two places — update both together:

1. `src/themes.css` — CSS custom property block `[data-theme="id"] { ... }`
2. `src/lib/constants/themes.ts` — entry in the `themes` array (the list of themes; don't repeat it in docs other than `docs/SOFTWARE-DESIGN.md`)

Every picker theme must keep `--text-2` ≥ 4.5:1 and `--text-3` ≥ 3:1 against `--bg`, and `--accent`/`--danger` ≥ 4.5:1 on `--bg`, `--surface` and `--surface-hi` (enforced by `src/lib/themes.test.ts`).

Each theme defines: `--bg`, `--surface`, `--surface-hi`, `--border`, `--text-1` through `--text-4`, `--done`, `--done-bg`, `--accent`, `--accent-hi`, `--accent-fg`, `--danger`, and `color-scheme` (for native inputs).

`--accent` is the theme's one chromatic hue. Use it sparingly: now/today, chosen or active state, primary actions and widget titles. Never use it for errors (`--danger`) or bulk text. Icons sit one `--text-*` step quieter than the data they label.

## Frame themes

E-paper frame themes (`/frame?theme=`) are separate from the dashboard themes. Defined in two places — update both together:

1. `src/themes.css` — an `[data-theme='epaper-<id>']` block under "Frame palettes" (plus the B&W blocks `epaper-bw` and `epaper-bw-night`)
2. `src/lib/constants/frameThemes.ts` — entry in the `frameThemes` array (picker metadata; `almanac` is the default; `dark: true` makes B&W mode use `epaper-bw-night`)

Colours are limited to the six measured Spectra 6 inks plus the approved two-ink mixes listed in `themes.css`, and text/icon/line tokens must be pure inks, with text legible on both `--bg` and `--surface` — `themes.test.ts` enforces all of it. Never add a frame theme to `constants/themes.ts`. Frame blocks set the same tokens minus `color-scheme`, plus `--block`/`--on-block` (a pure ink for large solid shapes and the ink set on it, ≥4.5:1) and optional `--arc-night/day/golden/outline` for the Day Arc.

## Font themes

Typography presets, orthogonal to color themes. Defined in two places — update both together:

1. `src/fonts.css` — one `[data-font="id"]` block per preset (source of truth: `--font-family`, `--weight-regular/medium/semibold/bold`, `--font-scale`, and optionally `--display-family`, the e-paper frame's masthead/numeral face — Fraunces unless a serif preset sets its own). Keep selectors bare `[data-font]` (never `html[data-font]`) — `FontThemePicker.svelte` reuses the blocks on its preview buttons. Keep `--font-scale` within 0.95–1.08 (e-paper caption legibility floor).
2. `src/lib/constants/fontThemes.ts` — entry in the `fontThemes` array (picker metadata only; `inter` is the default)

Families are self-hosted `@fontsource-variable/*` packages (devDependencies) — no CDN fonts.

A separate user size slider (`FontSizeStore.svelte.ts`, localStorage `hearth-font-size`) sets `--font-user-scale` (0.9–1.3) inline on `<html>`; `app.css` multiplies every size clamp by `--scale` = preset `--font-scale` × `--font-user-scale`.
