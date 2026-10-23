<script lang="ts">
    import { frameThemes } from '$lib/constants/frameThemes';
    import { fontThemes } from '$lib/constants/fontThemes';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { ChevronRight } from '@lucide/svelte';
    import SegmentedControl from './SegmentedControl.svelte';
    import {
        AMBIENT_POSITIONS,
        FRAME_SIZE_PRESETS,
        MAX_FRAME_SIZE,
        MIN_FRAME_SIZE,
        normalizeFrameSize,
    } from '$lib/frame';

    let widthInput = $state<HTMLInputElement>();
    let heightInput = $state<HTMLInputElement>();

    // Commit on change (blur/Enter), never per keystroke: a half-typed "8" would be
    // saved, rejected by the Display service, and stop settings from syncing.
    function commitSize() {
        const { width, height } = normalizeFrameSize(Number(widthInput?.value), Number(heightInput?.value));
        frameSettings.width = width;
        frameSettings.height = height;
        // The store may be unchanged (e.g. clamped back to its value), so reset the fields too.
        if (widthInput) widthInput.value = String(width);
        if (heightInput) heightInput.value = String(height);
    }
</script>

<div class="@container flex flex-col gap-6">
    <!-- Colour theme -->
    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Colour theme</p>
        <!-- Container-based columns: this card may sit in a narrow column beside the preview -->
        <div class="grid grid-cols-2 gap-3 @sm:grid-cols-3 @3xl:grid-cols-5">
            {#each frameThemes as t (t.id)}
                {@const selected = frameSettings.theme === t.id}
                <button
                    onclick={() => (frameSettings.theme = t.id)}
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
                    onclick={() => (frameSettings.font = f.id)}
                    aria-pressed={frameSettings.font === f.id}
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
            value={frameSettings.orientation}
            onchange={(v) => (frameSettings.orientation = v)}
        />
    </div>

    <!-- Set-once options, folded away so the everyday ones stay close together -->
    <details class="group">
        <summary
            class="type-label flex min-h-10 cursor-pointer list-none items-center gap-1.5 font-semibold tracking-widest text-(--text-2) uppercase transition-colors hover:text-(--text-1) [&::-webkit-details-marker]:hidden"
        >
            <ChevronRight class="icon-sm shrink-0 transition-transform group-open:rotate-90" aria-hidden="true" />
            Panel size &amp; ambient card
            <span class="font-normal tracking-normal normal-case tabular-nums">
                · {frameSettings.width}×{frameSettings.height}
            </span>
        </summary>
        <div class="mt-3 flex flex-col gap-6">
            <!-- Panel size: the native (landscape) resolution; Orientation swaps it -->
            <div>
                <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Panel size</p>
                <div class="flex flex-wrap items-center gap-2">
                    <input
                        bind:this={widthInput}
                        type="number"
                        inputmode="numeric"
                        min={MIN_FRAME_SIZE}
                        max={MAX_FRAME_SIZE}
                        value={frameSettings.width}
                        onchange={commitSize}
                        aria-label="Panel width in pixels"
                        class="type-body w-24 rounded-lg border border-(--border) bg-(--surface) px-3 py-1.5 text-(--text-1) tabular-nums transition-colors outline-none focus:border-(--text-3)"
                    />
                    <span class="type-label text-(--text-2)" aria-hidden="true">×</span>
                    <input
                        bind:this={heightInput}
                        type="number"
                        inputmode="numeric"
                        min={MIN_FRAME_SIZE}
                        max={MAX_FRAME_SIZE}
                        value={frameSettings.height}
                        onchange={commitSize}
                        aria-label="Panel height in pixels"
                        class="type-body w-24 rounded-lg border border-(--border) bg-(--surface) px-3 py-1.5 text-(--text-1) tabular-nums transition-colors outline-none focus:border-(--text-3)"
                    />
                    <span class="type-label text-(--text-2)">px</span>
                </div>
                <div class="mt-2 flex flex-wrap gap-2">
                    {#each FRAME_SIZE_PRESETS as p (p.label)}
                        <button
                            onclick={() => {
                                frameSettings.width = p.width;
                                frameSettings.height = p.height;
                            }}
                            aria-pressed={frameSettings.width === p.width && frameSettings.height === p.height}
                            class="type-label rounded-lg border border-(--border) px-3 py-1.5 text-(--text-2) transition-colors
                        hover:text-(--text-1) aria-pressed:border-(--accent) aria-pressed:font-semibold aria-pressed:text-(--text-1)"
                        >
                            {p.label} <span class="tabular-nums">{p.width}×{p.height}</span>
                        </button>
                    {/each}
                </div>
                <p class="type-caption mt-2 text-(--text-2)">
                    The panel's native resolution, long edge first. Type and spacing scale to match.
                </p>
            </div>

            <!-- Ambient card position: a 3×2 pad laid out like the frame -->
            <div>
                <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Ambient card</p>
                <div
                    class="grid w-fit grid-cols-3 gap-1 rounded border border-(--border) p-1"
                    role="radiogroup"
                    aria-label="Ambient card position"
                >
                    {#each AMBIENT_POSITIONS as p (p.value)}
                        {@const selected = frameSettings.ambientPosition === p.value}
                        <button
                            type="button"
                            role="radio"
                            aria-checked={selected}
                            title={p.label}
                            onclick={() => (frameSettings.ambientPosition = p.value)}
                            class="type-label rounded px-3 py-1.5 transition-colors {selected
                                ? 'bg-(--accent) text-(--accent-fg)'
                                : 'text-(--text-2) hover:bg-(--surface-hi)'}"
                        >
                            {p.label}
                        </button>
                    {/each}
                </div>
                <p class="type-caption mt-2 text-(--text-2)">
                    Where the time and weather sit on the photo. The photos themselves are chosen under Ambient.
                </p>
            </div>
        </div>
    </details>
</div>
