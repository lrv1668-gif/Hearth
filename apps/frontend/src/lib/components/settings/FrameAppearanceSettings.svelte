<script lang="ts">
    import { frameThemes } from '$lib/constants/frameThemes';
    import { fontThemes } from '$lib/constants/fontThemes';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { modeStore, setPalette } from '$lib/stores/ModeStore.svelte.ts';
    import Chip from '../ui/Chip.svelte';
    import Disclosure from '../ui/Disclosure.svelte';
    import { fieldClass } from '../ui/field';
    import PickerCard from '../ui/PickerCard.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import SizeSlider from '../ui/SizeSlider.svelte';
    import {
        AMBIENT_POSITIONS,
        FRAME_SIZE_PRESETS,
        MAX_FRAME_SIZE,
        MAX_FRAME_TEXT_SIZE,
        MIN_FRAME_SIZE,
        MIN_FRAME_TEXT_SIZE,
        normalizeFrameSize,
    } from '$lib/frame';

    const currentTheme = $derived(frameThemes.find((t) => t.id === frameSettings.theme) ?? frameThemes[0]);

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
    <!-- Color ↔ B&W: saved on its own (the frame's Button C flips it too) and applied at once -->
    <div>
        <p class="eyebrow mb-2">Colour</p>
        <SegmentedControl
            options={[
                { value: 'color', label: 'Color' },
                { value: 'bw', label: 'Black & white' },
            ]}
            value={modeStore.palette}
            onchange={(v) => setPalette(v)}
            label="Colour"
        />
        <p class="type-caption mt-2 text-(--text-2)">
            {modeStore.palette === 'color'
                ? 'The theme’s colours, mixed from the panel’s six inks.'
                : 'Pure black and white — black on white, or white on black for dark themes. Photos turn greyscale.'}
        </p>
    </div>

    <!-- Colour theme -->
    <div>
        <p class="eyebrow mb-1">Colour theme</p>
        <!-- Folded to the current theme; opens to the full grid -->
        <Disclosure>
            {#snippet summary()}
                <span
                    class="flex w-16 shrink-0 gap-0.5 rounded-md border border-(--border) p-1.5"
                    style="background: {currentTheme.swatches[0]}"
                    aria-hidden="true"
                >
                    {#each currentTheme.swatches.slice(1, 5) as color}
                        <span class="h-3 flex-1 rounded-sm" style="background: {color}"></span>
                    {/each}
                </span>
                <span class="min-w-0">
                    <span class="type-body block font-medium text-(--text-1)">{currentTheme.label}</span>
                    <span class="type-caption block truncate text-(--text-2)">{currentTheme.description}</span>
                </span>
            {/snippet}
            <!-- Container-based columns: this card may sit in a narrow column beside the preview -->
            <div class="grid grid-cols-2 gap-3 @sm:grid-cols-3 @lg:grid-cols-4 @3xl:grid-cols-5">
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
        </Disclosure>
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

    <!-- Text size -->
    <div>
        <p class="eyebrow mb-2">Text size</p>
        <SizeSlider
            value={frameSettings.textSize / 100}
            min={MIN_FRAME_TEXT_SIZE / 100}
            max={MAX_FRAME_TEXT_SIZE / 100}
            oninput={(v) => (frameSettings.textSize = Math.round(v * 100))}
        />
        <p class="type-caption mt-2 text-(--text-2)">
            Larger text fills a big panel; lists show fewer items to make room.
        </p>
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
    <Disclosure>
        {#snippet summary()}
            <span class="eyebrow">
                Panel size, dithering &amp; ambient card
                <span class="font-normal tracking-normal normal-case tabular-nums">
                    · {frameSettings.width}×{frameSettings.height}
                </span>
            </span>
        {/snippet}
        <div class="flex flex-col gap-6">
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

            <!-- Who dithers the panel's frame (GET /display/frame) -->
            <div>
                <p class="eyebrow mb-2">Dithering</p>
                <SegmentedControl
                    options={[
                        { value: 'hearth', label: 'Hearth' },
                        { value: 'frame', label: 'On the frame' },
                    ]}
                    value={frameSettings.dithering}
                    onchange={(v) => (frameSettings.dithering = v)}
                    label="Dithering"
                />
                <p class="type-caption mt-2 text-(--text-2)">
                    {frameSettings.dithering === 'hearth'
                        ? 'Hearth dithers each mode to the six inks — crisp text and even tints.'
                        : 'The frame gets the undithered picture and dithers it itself (inky’s Floyd–Steinberg). The preview still shows Hearth’s, and Hearth still dithers black & white.'}
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
    </Disclosure>
</div>
