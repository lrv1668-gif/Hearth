---
name: hearth-widget
description: Create new widgets for the Hearth home dashboard. Use this skill whenever the user asks to add a widget, build a new panel, create a new dashboard section, or extend the Hearth display with new functionality — even if they just say "add a X widget" without mentioning the skill by name.
---

# Hearth Widget Creator

Creates new widgets for the Hearth SvelteKit home dashboard.

## The Files

| File | What changes |
|------|-------------|
| `frontend/src/lib/constants/widgets.ts` | Register the widget ID |
| `frontend/src/lib/components/widgets/YourWidget.svelte` | Create the component |
| `frontend/src/routes/+page.svelte` | Import + wire into the dashboard |
| `frontend/src/lib/api.ts` | Response type + `api.<service>.<method>()` (data-fetching widgets) |
| `frontend/src/lib/stores/YourStore.svelte.ts` | One store per data type (data-fetching widgets) |
| `README.md`, `docs/SOFTWARE-DESIGN.md` | Add the widget to the feature list / frontend integration notes |

**Backend first:** domain data and math (API calls to third parties, caching, astronomy, recurrence, unit conversion) belong in a backend service under `src/`, not in the widget. If the widget needs data that no service provides yet, add a service first — follow the "When adding a new backend service" rule in `CLAUDE.md`. The frontend stays a thin renderer and never calls third-party APIs directly.

---

## Step 0: Explore Context First

Before writing any code, read these two files to understand the current state of the dashboard:

1. `frontend/src/lib/constants/widgets.ts` — see which widget IDs already exist, what's in each column, and which are toggleable
2. `frontend/src/routes/+page.svelte` — see the existing `renderWidget` snippet so you can match the pattern

Also skim one or two existing widget components in `frontend/src/lib/components/widgets/` for style reference — pick one that's similar to what you're building (e.g. an API-fetching widget if yours fetches data).

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

**2. `frontend/src/lib/stores/YourStore.svelte.ts`** — one store per data type (the `.svelte.ts` suffix is required for runes):
```typescript
import { api, type YourItem } from '$lib/api';

class YourStore {
    items = $state<YourItem[]>([]);
    error = $state(false);
}

export const yourStore = new YourStore();

export async function loadYours() {
    yourStore.error = false;
    const items = await api.yours.list();
    if (items === null) {
        yourStore.error = true;
        return;
    }
    yourStore.items = items;
}
```

**3. The widget:**
```svelte
<script lang="ts">
    import { onMount } from 'svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { yourStore, loadYours } from '$lib/stores/YourStore.svelte.ts';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadYours();
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if yourStore.error || yourStore.items.length === 0}
        <p class="type-label text-[var(--text-3)]">Data unavailable.</p>
    {:else}
        <!-- render yourStore.items -->
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

### Color tokens (always via CSS variables, never hardcoded)
```html
text-[var(--text-1)]     <!-- primary text -->
text-[var(--text-2)]     <!-- secondary -->
text-[var(--text-3)]     <!-- muted / tertiary -->
text-[var(--text-4)]     <!-- dimmest -->
text-[var(--accent)]     <!-- accent highlight -->
bg-[var(--surface)]      <!-- card / container backgrounds -->
bg-[var(--surface-hi)]   <!-- elevated surfaces, badges, inputs -->
border-[var(--border)]   <!-- dividers -->
```

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

## Checklist

- [ ] Read `widgets.ts` and `+page.svelte` before starting
- [ ] Domain logic / third-party calls live in a backend service, not the widget
- [ ] Added to `allWidgets` in `widgets.ts`
- [ ] Added to `toggleableWidgets` if user-toggleable (with `description`)
- [ ] Added to `DEFAULT_WIDGET_COLUMNS` in the appropriate column
- [ ] Created `YourWidget.svelte` using the appropriate data pattern
- [ ] Pattern B: response interface + `ApiClient` method in `api.ts`, own `YourStore.svelte.ts` with an `error` flag, no `fetch()` in the component
- [ ] Styled with type utilities + CSS variable tokens only
- [ ] Imported in `+page.svelte`
- [ ] Added to `renderWidget` snippet in `+page.svelte`
- [ ] Wrapped in `<WidgetContainer>` with correct `title` and optional `associatedWidgetId`
- [ ] Intervals cleaned up via `return () => clearInterval(id)` inside `$effect`
- [ ] Widget added to `README.md` feature list and `docs/SOFTWARE-DESIGN.md`
- [ ] `npm run check` passes and the widget was checked in the real UI with the `verify` skill
