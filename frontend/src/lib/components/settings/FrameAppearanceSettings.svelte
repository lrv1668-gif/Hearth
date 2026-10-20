<script lang="ts">
    import { frameThemes } from '$lib/constants/frameThemes';
    import { fontThemes } from '$lib/constants/fontThemes';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { frameUrl } from '$lib/frame';

    // Preview-only toggles: on the real frame, Daily/Ambient and Color/B&W are
    // switched by schedule or the frame's buttons, so they aren't saved here.
    let previewMode = $state<'daily' | 'ambient'>('daily');
    let previewPalette = $state<'color' | 'bw'>('color');

    const url = $derived(
        frameUrl({
            mode: previewMode,
            palette: previewPalette,
            theme: settings.frameTheme,
            font: settings.frameFont,
            orientation: settings.frameOrientation,
        })
    );

    const size = $derived(settings.frameOrientation === 'portrait' ? { w: 1200, h: 1600 } : { w: 1600, h: 1200 });
    let boxWidth = $state(0);
    const scale = $derived(boxWidth / size.w);
</script>

{#snippet segmented<T extends string>(options: { value: T; label: string }[], current: T, set: (v: T) => void)}
    <div class="inline-flex rounded-lg border border-(--border) p-0.5">
        {#each options as o (o.value)}
            <button
                onclick={() => set(o.value)}
                aria-pressed={current === o.value}
                class="type-label rounded-md px-3 py-1 text-(--text-2) transition-colors hover:text-(--text-1)
                    aria-pressed:bg-(--surface-hi) aria-pressed:font-semibold aria-pressed:text-(--text-1)"
            >
                {o.label}
            </button>
        {/each}
    </div>
{/snippet}

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
        {@render segmented(
            [
                { value: 'landscape', label: 'Landscape' },
                { value: 'portrait', label: 'Portrait' },
            ],
            settings.frameOrientation,
            (v) => (settings.frameOrientation = v)
        )}
    </div>

    <!-- Live preview -->
    <div class="border-t border-(--border) pt-4">
        <div class="mb-3 flex flex-wrap items-center gap-3">
            <p class="type-label font-semibold tracking-widest text-(--text-2) uppercase">Preview</p>
            {@render segmented(
                [
                    { value: 'daily', label: 'Daily' },
                    { value: 'ambient', label: 'Ambient' },
                ],
                previewMode,
                (v) => (previewMode = v)
            )}
            {@render segmented(
                [
                    { value: 'color', label: 'Color' },
                    { value: 'bw', label: 'B&W' },
                ],
                previewPalette,
                (v) => (previewPalette = v)
            )}
        </div>

        <!-- The real /frame page at panel resolution, scaled down to fit -->
        <div
            bind:clientWidth={boxWidth}
            class="relative overflow-hidden rounded-lg border border-(--border)
                {settings.frameOrientation === 'portrait' ? 'max-w-sm' : 'max-w-2xl'}"
            style="height: {size.h * scale}px"
        >
            {#if boxWidth > 0}
                <iframe
                    src={url}
                    title="Frame preview"
                    tabindex="-1"
                    class="pointer-events-none absolute top-0 left-0 origin-top-left border-0"
                    style="width: {size.w}px; height: {size.h}px; transform: scale({scale})"
                ></iframe>
            {/if}
        </div>
        <p class="type-caption mt-2 text-(--text-2)">
            Shown before dithering — on the panel, fills become an even dot texture. Frame URL:
            <a href={url} target="_blank" rel="noopener noreferrer" class="font-mono break-all underline">{url}</a>
        </p>
    </div>
</div>
