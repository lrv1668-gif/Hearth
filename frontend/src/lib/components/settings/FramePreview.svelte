<script lang="ts">
    import { untrack } from 'svelte';
    import { frameDataTheme } from '$lib/constants/frameThemes';
    import { normalizeFrameLayout } from '$lib/constants/frameWidgets';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { frameCanvas, frameUrl, type FrameOptions } from '$lib/frame';
    import { api } from '$lib/api';
    import SegmentedControl from './SegmentedControl.svelte';

    // Preview-only toggles: on the real frame, Daily/Ambient and Color/B&W are
    // switched by schedule or the frame's buttons, so they aren't saved here.
    let previewMode = $state<'daily' | 'ambient'>('daily');
    let previewPalette = $state<'color' | 'bw'>('color');
    // Live is the page itself; Screen and Paper are the Display service's dithered render.
    let view = $state<'live' | 'screen' | 'paper'>('live');

    const layout = $derived(normalizeFrameLayout(frameSettings.layout));

    const options = $derived<FrameOptions>({
        mode: previewMode,
        palette: previewPalette,
        theme: frameSettings.theme,
        font: frameSettings.font,
        orientation: frameSettings.orientation,
        width: frameSettings.width,
        height: frameSettings.height,
        layout,
        split: frameSettings.split,
        position: frameSettings.ambientPosition,
    });

    // The full URL, shown below the preview.
    const url = $derived(frameUrl(options));

    // A render takes a few seconds of headless Chromium on the host, so wait for the
    // settings to stop changing, and only render while a dithered view is showing.
    let renderSrc = $state('');
    let rendering = $state(false);
    let renderFailed = $state(false);
    $effect(() => {
        if (view === 'live') return;
        const next = api.display.previewUrl(options, view);
        const timer = setTimeout(() => {
            if (next === renderSrc) return;
            renderSrc = next;
            rendering = true;
            renderFailed = false;
        }, 600);
        return () => clearTimeout(timer);
    });

    // The iframe only reloads for structural changes (mode, orientation, size, layout,
    // split). Theme, font and palette are applied to the loaded frame in place below —
    // a reload would refetch everything, and in Ambient swap in a new random photo.
    const previewSrc = $derived.by(() => {
        const structure = {
            mode: previewMode,
            orientation: frameSettings.orientation,
            width: frameSettings.width,
            height: frameSettings.height,
            layout,
            split: frameSettings.split,
        };
        return untrack(() =>
            frameUrl({
                ...structure,
                palette: previewPalette,
                theme: frameSettings.theme,
                font: frameSettings.font,
                position: frameSettings.ambientPosition,
            })
        );
    });

    let iframe = $state<HTMLIFrameElement>();

    function applyLook() {
        // Read the settings before touching the iframe: if the frame hasn't loaded yet
        // the early return must not stop the $effect from tracking them.
        const theme = frameDataTheme(frameSettings.theme, previewPalette);
        const font = frameSettings.font;
        const position = frameSettings.ambientPosition;
        const root = iframe?.contentDocument?.querySelector<HTMLElement>('.frame');
        if (!root) return;
        root.dataset.theme = theme;
        root.dataset.font = font;
        root.dataset.position = position;
    }

    $effect(applyLook);

    const size = $derived(
        frameCanvas({
            width: frameSettings.width,
            height: frameSettings.height,
            orientation: frameSettings.orientation,
        })
    );
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
        <SegmentedControl
            options={[
                { value: 'live', label: 'Live' },
                { value: 'screen', label: 'Screen' },
                { value: 'paper', label: 'Paper' },
            ]}
            value={view}
            onchange={(v) => (view = v)}
        />
    </div>

    <div
        bind:clientWidth={boxWidth}
        class="relative overflow-hidden rounded-lg border border-(--border)
            {size.h > size.w ? 'mx-auto max-w-sm' : 'max-w-2xl'}"
        style="height: {size.h * scale}px"
    >
        {#if view !== 'live'}
            {#if renderSrc}
                <img
                    src={renderSrc}
                    alt="Dithered frame preview"
                    onload={() => (rendering = false)}
                    onerror={() => ((rendering = false), (renderFailed = true))}
                    class="absolute inset-0 h-full w-full"
                />
            {/if}
            {#if rendering || renderFailed || !renderSrc}
                <div
                    class="type-label absolute inset-0 flex items-center justify-center bg-(--surface)/80 text-(--text-2)"
                >
                    {renderFailed ? 'Couldn’t render the frame — is the renderer running?' : 'Rendering…'}
                </div>
            {/if}
        {:else if boxWidth > 0}
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
        {#if view === 'live'}
            Shown before dithering — on the panel, fills become an even dot texture.
        {:else}
            {view === 'screen'
                ? 'The exact pixels the panel will show, in screen colours.'
                : 'Approximate panel colours — e-paper is greyer and less saturated than a screen.'}
            {#if renderSrc}
                <a href={renderSrc} target="_blank" rel="noopener noreferrer" class="underline">Open full size</a>.
            {/if}
        {/if}
        Frame URL:
        <a href={url} target="_blank" rel="noopener noreferrer" class="font-mono break-all underline">{url}</a>
    </p>
</div>
