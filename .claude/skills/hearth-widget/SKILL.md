---
name: hearth-widget
description: Create new widgets for the Hearth home dashboard. Use this skill whenever the user asks to add a widget, build a new panel, create a new dashboard section, or extend the Hearth display with new functionality — even if they just say "add a X widget" without mentioning the skill by name.
---

# Hearth Widget Creator

Creates new widgets for the Hearth SvelteKit home dashboard.

## The Files

| File | What changes |
|------|-------------|
| `apps/frontend/src/lib/constants/widgets.ts` | Register the widget ID |
| `apps/frontend/src/lib/components/widgets/YourWidget.svelte` | Create the component |
| `apps/frontend/src/routes/+page.svelte` | Import + wire into the dashboard |
| `apps/frontend/src/lib/api.ts` | Response type + `api.<service>.<method>()` (data-fetching widgets) |
| `apps/frontend/src/lib/stores/YourStore.svelte.ts` | One store per data type (data-fetching widgets) |
| `apps/frontend/src/lib/components/widgets/views/YourView.svelte` | Shared markup, if the widget also goes on the e-paper frame (Step 5) |
| `apps/frontend/src/lib/constants/frameWidgets.ts`, `components/frame/FrameDaily.svelte`, `routes/frame/+page.svelte` | Frame registration, slot and data loader (Step 5) |
| `README.md` (the `/` row of the Pages table, plus the `/frame` row for frame widgets), `docs/SOFTWARE-DESIGN.md` | Add the widget to the feature list / frontend integration notes |
| `site/index.html` | The widget grid on the project site (`Frame` badge for frame widgets) |

**Backend first:** domain data and math (API calls to third parties, caching, astronomy, recurrence, unit conversion) belong in a backend service under `apps/services/`, not in the widget. If the widget needs data that no service provides yet, add a service first — use the `hearth-service` skill. The frontend stays a thin renderer and never calls third-party APIs directly.

---

## Step 0: Explore Context First

Before writing any code, read these files to understand the current state of the dashboard:

1. `apps/frontend/src/lib/constants/widgets.ts` — see which widget IDs already exist, what's in each column, and which are toggleable
2. `apps/frontend/src/routes/+page.svelte` — see the existing `renderWidget` snippet so you can match the pattern
3. If it should also show on the e-paper frame: `apps/frontend/src/lib/constants/frameWidgets.ts` and `components/frame/FrameDaily.svelte`

Also skim one or two existing widget components in `apps/frontend/src/lib/components/widgets/` for style reference — pick one that's similar to what you're building (e.g. `BirdsWidget.svelte` if yours fetches data and also appears on the frame).

The Settings layout editors (`WidgetLayoutEditor`, shared by the dashboard and the frame) read these registries — a new widget shows up there without any settings code.

---

## Step 1: Register in widgets.ts

```typescript
// Always add to allWidgets
export const allWidgets = [
    ...
    { id: 'your-widget-id', label: 'Display Name' },
] as const;

// Add to toggleableWidgets only if the user can turn it on/off in Settings
export const toggleableWidgets = [
    ...
    { id: 'your-widget-id', label: 'Display Name', description: 'One-line description for the settings panel' },
] as const;

// Place it in the appropriate column (see Column Placement below)
export const DEFAULT_WIDGET_COLUMNS = {
    left: [..., 'your-widget-id'],
    right: [...],
};
```

### Column Placement

- **Left column** — action-oriented or time-sensitive content: tasks, calendar, countdowns, now-playing
- **Right column** — ambient or informational content: weather, moon phase, news feeds, quotes

When in doubt, ask the user which column they prefer.

---

## Step 2: Choose a Data Pattern

### A — Static / Computed (no network, e.g. clocks, moon phase)
```svelte
<script lang="ts">
    let value = $state(compute());
    const derived = $derived(transform(value));

    $effect(() => {
        const id = setInterval(() => (value = compute()), 1000);
        return () => clearInterval(id);
    });
</script>
```

### B — Backend data (the default for anything fetched)

Every fetching widget uses the same three layers. Never call `fetch()` from a component. See `BirdsWidget.svelte` / `BirdsStore.svelte.ts` for a complete example.

**1. `api.ts`** — add the response interface (snake_case fields, matching the service's JSON) and a method on `ApiClient`. Paths are relative; the private `get<T>` helper returns `null` on failure:
```typescript
export interface YourItem {
    field: string;
    observed_at: string;
}

// inside class ApiClient
readonly yours = {
    list: (): Promise<YourItem[] | null> => this.get<YourItem[]>('/yours/list'),
};
```

**2. `apps/frontend/src/lib/stores/YourStore.svelte.ts`** — one store per data type (the `.svelte.ts` suffix is required for runes):
```typescript
import { api, type YourItem } from '$lib/api';

class YourStore {
    items = $state<YourItem[]>([]);
    error = $state(false);
}

export const yourStore = new YourStore();

export async function loadYours() {
    const items = await api.yours.list();
    if (items === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last items.
        yourStore.error = yourStore.items.length === 0;
        return;
    }
    yourStore.error = false;
    yourStore.items = items;
}
```

**3. The widget** — loads once, then refreshes with `poll(load, ms)` from `utils.ts`, returned from `onMount` so it's cleaned up:
```svelte
<script lang="ts">
    import { onMount } from 'svelte';
    import { MINUTE, poll } from '$lib/utils';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { yourStore, loadYours } from '$lib/stores/YourStore.svelte.ts';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadYours();
        return poll(loadYours, 15 * MINUTE);
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if yourStore.error}
        <p class="type-label text-(--text-2)">Your data unavailable.</p>
    {:else}
        <!-- render yourStore.items, or <YourView items={yourStore.items} /> (Step 5) -->
    {/if}
</SkeletonLoader>
```

---

## Step 3: Style with the Design System

### Semantic type utilities
```html
<p class="type-display">   <!-- largest: clocks, temperatures, big numbers -->
<p class="type-title">     <!-- widget headings, key values -->
<p class="type-subtitle">  <!-- secondary headings -->
<p class="type-body">      <!-- main content -->
<p class="type-label">     <!-- timestamps, tags, badges -->
<p class="type-caption">   <!-- tiny hints, metadata -->
```

Never raw `text-sm`-style size utilities or numeric `font-[...]` weights — use `.type-*` plus `font-medium`/`font-semibold`/`font-bold`.

### Color tokens (always via CSS variables, never hardcoded)

Use Tailwind v4's `(--var)` shorthand — the codebase never uses `[var(--var)]`:
```html
text-(--text-1)     <!-- primary text -->
text-(--text-2)     <!-- secondary text, empty/error states -->
text-(--text-3)     <!-- icons and decoration only — not for text people need to read -->
text-(--text-4)     <!-- dimmest decoration -->
text-(--accent)     <!-- accent highlight -->
bg-(--surface)      <!-- card / container backgrounds -->
bg-(--surface-hi)   <!-- elevated surfaces, badges, inputs -->
border-(--border)   <!-- dividers -->
```

Buttons, inputs, toggles, dialogs: use the shared components in `apps/frontend/src/lib/components/ui/` — don't hand-roll class strings.

### Lucide icon sizes
```html
<Icon class="icon-lg" />  <!-- 20–28px -->
<Icon class="icon-md" />  <!-- 14–22px -->
<Icon class="icon-sm" />  <!-- 12–18px -->
<Icon class="icon-xs" />  <!-- 10–14px -->
```

---

## Step 4: Wire into +page.svelte

```svelte
<!-- At the top of the script block -->
import YourWidget from '$lib/components/widgets/YourWidget.svelte';

<!-- Inside the renderWidget snippet -->
{:else if id === 'your-widget-id'}
    <WidgetContainer title="Widget Title" associatedWidgetId="your-widget-id">
        <YourWidget />
    </WidgetContainer>
```

> Omit `associatedWidgetId` for always-visible widgets that are not in `toggleableWidgets`.

---

## Step 5 (optional): Put it on the e-paper frame

Only widgets that stay correct between scheduled refreshes (an hour or more) belong on the frame — no live clocks, departures or now-playing.

**Same content as the dashboard → a shared view, not a copy.** Move the markup into `components/widgets/views/YourView.svelte` (see `BirdSightingsView.svelte`): data in via props, an optional `lines` budget that trims the list (`itemsThatFit` from `lib/frame.ts`), sizes from `type-*`/`icon-*`/em units and `--stroke` so the frame's overrides scale it. The dashboard widget becomes a thin wrapper that loads, polls and renders `<YourView items={yourStore.items} />`. Only frame-specific designs go in `components/frame/widgets/`.

1. `constants/frameWidgets.ts` — add `{ id, label, description }` to `frameWidgets`
2. `components/frame/FrameDaily.svelte` — add a branch to the `widget` snippet: `<YourView items={yourStore.items} lines={n} />`
3. `lib/frame.ts` — if the widget has a fixed height (not a list that trims), add it to `FIXED_LINES`; otherwise `allocateLines` gives it a share of the column
4. `routes/frame/+page.svelte` — add its loader to `widgetLoaders` (weather, tasks, calendar and almanac already always load)
5. `README.md` `/frame` row and the `Frame` badge in `site/index.html`

Check it at `/frame?mode=daily&left=your-id` with the `verify` skill.

---

## Checklist

- [ ] Read `widgets.ts` and `+page.svelte` before starting
- [ ] Domain logic / third-party calls live in a backend service, not the widget
- [ ] Added to `allWidgets` in `widgets.ts`
- [ ] Added to `toggleableWidgets` if user-toggleable (with `description`)
- [ ] Added to `DEFAULT_WIDGET_COLUMNS` in the appropriate column
- [ ] Created `YourWidget.svelte` using the appropriate data pattern
- [ ] Pattern B: response interface + `ApiClient` method in `api.ts`, own `YourStore.svelte.ts` with an `error` flag, no `fetch()` in the component
- [ ] Polls with `poll(load, ms)` returned from `onMount`
- [ ] Styled with type utilities + `text-(--token)` CSS variables only; `--text-3`/`--text-4` never on readable text
- [ ] Imported in `+page.svelte`
- [ ] Added to `renderWidget` snippet in `+page.svelte`
- [ ] Wrapped in `<WidgetContainer>` with correct `title` and optional `associatedWidgetId`
- [ ] Intervals cleaned up via `return () => clearInterval(id)` inside `$effect`
- [ ] Frame widget (if any): shared view in `widgets/views/`, registered in `frameWidgets.ts`, slot in `FrameDaily.svelte`, loader in `widgetLoaders`
- [ ] Widget added to `README.md` (Pages table), `docs/SOFTWARE-DESIGN.md` and the `site/index.html` widget grid
- [ ] `npm run check` and `npm run test` pass, and the widget was checked in the real UI with the `verify` skill
