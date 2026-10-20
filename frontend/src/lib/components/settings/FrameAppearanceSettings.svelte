<script lang="ts">
    import { frameThemes } from '$lib/constants/frameThemes';
    import { fontThemes } from '$lib/constants/fontThemes';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import SegmentedControl from './SegmentedControl.svelte';
</script>

<div class="flex flex-col gap-6">
    <!-- Colour theme -->
    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Colour theme</p>
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            {#each frameThemes as t (t.id)}
                {@const selected = settings.frameTheme === t.id}
                <button
                    onclick={() => (settings.frameTheme = t.id)}
                    aria-pressed={selected}
                    style={selected ? 'outline: 2px solid var(--accent); outline-offset: 2px;' : ''}
                    class="flex flex-col overflow-hidden rounded-xl border-2 bg-(--surface) text-left transition-all
                        {selected ? 'border-(--accent)' : 'border-(--border) opacity-85 hover:opacity-100'}"
                >
                    <!-- Swatches are the theme's real inks, shown on its own paper colour -->
                    <div class="flex gap-1 p-3" style="background: {t.swatches[0]}">
                        {#each t.swatches.slice(1) as color}
                            <span class="h-6 flex-1 rounded-sm" style="background: {color}"></span>
                        {/each}
                    </div>
                    <div class="border-t border-(--border) px-3 py-2">
                        <p class="type-label font-medium text-(--text-1)">{t.label}</p>
                        <p class="type-caption text-(--text-2)">{t.description}</p>
                    </div>
                </button>
            {/each}
        </div>
    </div>

    <!-- Typeface -->
    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Typeface</p>
        <div class="flex flex-wrap gap-2">
            {#each fontThemes as f (f.id)}
                <!-- data-font scopes the preset's [data-font] block to this chip -->
                <button
                    data-font={f.id}
                    onclick={() => (settings.frameFont = f.id)}
                    aria-pressed={settings.frameFont === f.id}
                    class="type-label rounded-lg border border-(--border) px-3 py-1.5 text-(--text-2) transition-colors
                        hover:text-(--text-1) aria-pressed:border-(--accent) aria-pressed:font-semibold aria-pressed:text-(--text-1)"
                >
                    {f.label}
                </button>
            {/each}
        </div>
    </div>

    <!-- Orientation -->
    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Orientation</p>
        <SegmentedControl
            options={[
                { value: 'landscape', label: 'Landscape' },
                { value: 'portrait', label: 'Portrait' },
            ]}
            value={settings.frameOrientation}
            onchange={(v) => (settings.frameOrientation = v)}
        />
    </div>
</div>
