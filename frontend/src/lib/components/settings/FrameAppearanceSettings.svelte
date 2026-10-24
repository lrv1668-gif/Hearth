<script lang="ts">
    import { frameThemes } from '$lib/constants/frameThemes';
    import { fontThemes } from '$lib/constants/fontThemes';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { ChevronRight } from '@lucide/svelte';
    import Chip from '../ui/Chip.svelte';
    import { fieldClass } from '../ui/field';
    import PickerCard from '../ui/PickerCard.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
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
        <p class="eyebrow mb-2">Colour theme</p>
        <!-- Container-based columns: this card may sit in a narrow column beside the preview -->
        <div class="grid grid-cols-2 gap-3 @sm:grid-cols-3 @3xl:grid-cols-5">
            {#each frameThemes as t (t.id)}
                {@const selected = frameSettings.theme === t.id}
                <PickerCard pressed={selected} onclick={() => (frameSettings.theme = t.id)}>
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
                </PickerCard>
            {/each}
        </div>
    </div>

    <!-- Typeface -->
    <div>
        <p class="eyebrow mb-2">Typeface</p>
        <div class="flex flex-wrap gap-2" role="group" aria-label="Typeface">
            {#each fontThemes as f (f.id)}
                <!-- data-font scopes the preset's [data-font] block to this chip -->
                <Chip
                    data-font={f.id}
                    pressed={frameSettings.font === f.id}
                    onclick={() => (frameSettings.font = f.id)}
                >
                    {f.label}
                </Chip>
            {/each}
        </div>
    </div>

    <!-- Orientation -->
    <div>
        <p class="eyebrow mb-2">Orientation</p>
        <SegmentedControl
            options={[
                { value: 'landscape', label: 'Landscape' },
                { value: 'portrait', label: 'Portrait' },
            ]}
            value={frameSettings.orientation}
            onchange={(v) => (frameSettings.orientation = v)}
            label="Orientation"
        />
        {#if frameSettings.orientation === 'portrait'}
            <!-- The panel is always sent landscape; which way to turn depends on how the frame hangs -->
            <p class="type-label mt-4 mb-2 text-(--text-2)">Turn onto the panel</p>
            <SegmentedControl
                options={[
                    { value: '90', label: 'Clockwise' },
                    { value: '270', label: 'Counter-clockwise' },
                ]}
                value={String(frameSettings.portraitRotation)}
                onchange={(v) => (frameSettings.portraitRotation = v === '270' ? 270 : 90)}
                label="Turn onto the panel"
            />
        {/if}
    </div>

    <!-- Set-once options, folded away so the everyday ones stay close together -->
    <details class="group">
        <summary
            class="eyebrow flex min-h-10 cursor-pointer list-none items-center gap-1.5 transition-colors hover:text-(--text-1) [&::-webkit-details-marker]:hidden"
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
                <p class="eyebrow mb-2">Panel size</p>
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
                        class="{fieldClass()} w-24 tabular-nums"
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
                        class="{fieldClass()} w-24 tabular-nums"
                    />
                    <span class="type-label text-(--text-2)">px</span>
                </div>
                <div class="mt-2 flex flex-wrap gap-2" role="group" aria-label="Panel size presets">
                    {#each FRAME_SIZE_PRESETS as p (p.label)}
                        <Chip
                            pressed={frameSettings.width === p.width && frameSettings.height === p.height}
                            onclick={() => {
                                frameSettings.width = p.width;
                                frameSettings.height = p.height;
                            }}
                        >
                            {p.label} <span class="tabular-nums">{p.width}×{p.height}</span>
                        </Chip>
                    {/each}
                </div>
                <p class="type-caption mt-2 text-(--text-2)">
                    The panel's native resolution, long edge first. Type and spacing scale to match.
                </p>
            </div>

            <!-- Ambient card position: a 3×2 pad laid out like the frame -->
            <div>
                <p class="eyebrow mb-2">Ambient card</p>
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
