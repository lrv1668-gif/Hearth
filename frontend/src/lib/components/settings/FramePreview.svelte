<script lang="ts">
    import { untrack } from 'svelte';
    import { frameDataTheme } from '$lib/constants/frameThemes';
    import { normalizeFrameLayout } from '$lib/constants/frameWidgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { frameUrl } from '$lib/frame';
    import SegmentedControl from './SegmentedControl.svelte';

    // Preview-only toggles: on the real frame, Daily/Ambient and Color/B&W are
    // switched by schedule or the frame's buttons, so they aren't saved here.
    let previewMode = $state<'daily' | 'ambient'>('daily');
    let previewPalette = $state<'color' | 'bw'>('color');

    const layout = $derived(normalizeFrameLayout(settings.frameLayout));

    // The full URL, shown below the preview.
    const url = $derived(
        frameUrl({
            mode: previewMode,
            palette: previewPalette,
            theme: settings.frameTheme,
            font: settings.frameFont,
            orientation: settings.frameOrientation,
            layout,
            split: settings.frameSplit,
        })
    );

    // The iframe only reloads for structural changes (mode, orientation, layout,
    // split). Theme, font and palette are applied to the loaded frame in place below —
    // a reload would refetch everything, and in Ambient swap in a new random photo.
    const previewSrc = $derived.by(() => {
        const structure = {
            mode: previewMode,
            orientation: settings.frameOrientation,
            layout,
            split: settings.frameSplit,
        };
        return untrack(() =>
            frameUrl({ ...structure, palette: previewPalette, theme: settings.frameTheme, font: settings.frameFont })
        );
    });

    let iframe = $state<HTMLIFrameElement>();

    function applyLook() {
        // Read the settings before touching the iframe: if the frame hasn't loaded yet
        // the early return must not stop the $effect from tracking them.
        const theme = frameDataTheme(settings.frameTheme, previewPalette);
        const font = settings.frameFont;
        const root = iframe?.contentDocument?.querySelector<HTMLElement>('.frame');
        if (!root) return;
        root.dataset.theme = theme;
        root.dataset.font = font;
    }

    $effect(applyLook);

    const size = $derived(settings.frameOrientation === 'portrait' ? { w: 1200, h: 1600 } : { w: 1600, h: 1200 });
    let boxWidth = $state(0);
    const scale = $derived(boxWidth / size.w);
</script>

<!--
@component
Live preview of the real /frame page at panel resolution, scaled to fit, with
Daily/Ambient and Color/B&W toggles and the resulting frame URL.
-->
<div>
    <div class="mb-3 flex flex-wrap items-center gap-3">
        <SegmentedControl
            options={[
                { value: 'daily', label: 'Daily' },
                { value: 'ambient', label: 'Ambient' },
            ]}
            value={previewMode}
            onchange={(v) => (previewMode = v)}
        />
        <SegmentedControl
            options={[
                { value: 'color', label: 'Color' },
                { value: 'bw', label: 'B&W' },
            ]}
            value={previewPalette}
            onchange={(v) => (previewPalette = v)}
        />
    </div>

    <div
        bind:clientWidth={boxWidth}
        class="relative overflow-hidden rounded-lg border border-(--border)
            {settings.frameOrientation === 'portrait' ? 'max-w-sm' : 'max-w-2xl'}"
        style="height: {size.h * scale}px"
    >
        {#if boxWidth > 0}
            <iframe
                bind:this={iframe}
                onload={applyLook}
                src={previewSrc}
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
