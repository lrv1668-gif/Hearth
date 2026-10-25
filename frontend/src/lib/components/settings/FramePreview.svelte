<script lang="ts" module>
    import type { FrameModeId } from '$lib/constants/frameModes';
    import type { Photo } from '$lib/api';

    // Shared by every preview on the page, so the full-size one opens on the same mode,
    // palette, view and photo as the card's.
    const shared = $state({
        mode: 'daily' as FrameModeId,
        palette: 'color' as 'color' | 'bw',
        view: 'live' as 'live' | 'screen' | 'paper',
        photo: undefined as Photo | null | undefined,
    });
    let photoKey = '';
</script>

<script lang="ts">
    import { untrack } from 'svelte';
    import { frameDataTheme } from '$lib/constants/frameThemes';
    import { normalizeFrameLayout } from '$lib/constants/frameWidgets';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { frameCanvas, frameUrl, type FrameOptions } from '$lib/frame';
    import { frameModes } from '$lib/constants/frameModes';
    import { api } from '$lib/api';
    import { pickPhoto, pickPinned } from '$lib/photos';
    import { Maximize2 } from '@lucide/svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import IconButton from '../ui/IconButton.svelte';
    import Disclosure from '../ui/Disclosure.svelte';

    // The most height (px) the whole preview may take, so a tall portrait frame shrinks to
    // fit the window instead of running off it. Unset: scale to the width alone.
    // `onexpand` adds a button that opens a full-size preview.
    let { maxHeight, onexpand }: { maxHeight?: number; onexpand?: () => void } = $props();

    // Preview-only toggles: on the real frame, the mode and Color/B&W are switched by
    // the mode schedule, a manual choice or the frame's buttons, so they aren't saved here.
    // Live is the page itself; Screen and Paper are the Display service's dithered render.

    const layout = $derived(normalizeFrameLayout(frameSettings.layout));

    // Ambient and Photo show a random photo, so the preview picks one itself and hands
    // it to every view: Live, Screen and Paper show the same photo. It picks again only
    // when the mode, photo choices or orientation change. Undefined while a pick loads.
    const photoMode = $derived(shared.mode === 'ambient' || shared.mode === 'photo');
    $effect(() => {
        if (!photoMode || !frameSettings.loaded) return;
        const mode = shared.mode;
        const ids = mode === 'photo' ? [...frameSettings.photoIds] : [];
        const source = frameSettings.photoSource;
        const categories = [...frameSettings.photoCategories];
        const orientation = frameSettings.orientation;
        const key = JSON.stringify([mode, ids, source, categories, orientation]);
        if (key === photoKey) return;
        photoKey = key;
        shared.photo = undefined;
        // As the frame picks: Photo's pinned upload or album, else an Ambient photo.
        const pick = async () =>
            (ids.length > 0 ? pickPinned(ids, await api.photos.list()) : null) ??
            (await pickPhoto(source, categories, orientation));
        pick().then(
            (p) => key === photoKey && (shared.photo = p),
            () => key === photoKey && (shared.photo = null)
        );
    });
    const photo = $derived(photoMode ? shared.photo : null);

    const options = $derived<FrameOptions>({
        mode: shared.mode,
        palette: shared.palette,
        theme: frameSettings.theme,
        font: frameSettings.font,
        textSize: frameSettings.textSize,
        orientation: frameSettings.orientation,
        width: frameSettings.width,
        height: frameSettings.height,
        layout,
        split: frameSettings.split,
        position: frameSettings.ambientPosition,
        photo,
    });

    // The full URL, shown below the preview — without the pinned photo, as the frame loads it.
    const url = $derived(frameUrl({ ...options, photo: null }));

    // A render takes a few seconds of headless Chromium on the host, so wait for the
    // settings to stop changing, and only render while a dithered view is showing. The
    // last render stays loaded under Live, so switching back doesn't render it again.
    let renderSrc = $state('');
    let rendering = $state(false);
    let renderFailed = $state(false);
    $effect(() => {
        if (shared.view === 'live' || photo === undefined) return;
        const next = api.display.previewUrl(options, shared.view);
        const timer = setTimeout(() => {
            if (next === renderSrc) return;
            renderSrc = next;
            rendering = true;
            renderFailed = false;
        }, 600);
        return () => clearTimeout(timer);
    });

    // The iframe only reloads for structural changes (mode, orientation, size, layout,
    // split, the picked photo). Theme, font, text size and palette are applied to the loaded frame
    // in place below — a reload would refetch everything.
    const previewSrc = $derived.by(() => {
        const structure = {
            mode: shared.mode,
            orientation: frameSettings.orientation,
            width: frameSettings.width,
            height: frameSettings.height,
            layout,
            split: frameSettings.split,
            photo,
        };
        return untrack(() =>
            frameUrl({
                ...structure,
                palette: shared.palette,
                theme: frameSettings.theme,
                font: frameSettings.font,
                textSize: frameSettings.textSize,
                position: frameSettings.ambientPosition,
            })
        );
    });

    let iframe = $state<HTMLIFrameElement>();

    function applyLook() {
        // Read the settings before touching the iframe: if the frame hasn't loaded yet
        // the early return must not stop the $effect from tracking them.
        const theme = frameDataTheme(frameSettings.theme, shared.palette);
        const font = frameSettings.font;
        const textSize = frameSettings.textSize;
        const position = frameSettings.ambientPosition;
        const root = iframe?.contentDocument?.querySelector<HTMLElement>('.frame');
        if (!root) return;
        root.dataset.theme = theme;
        root.dataset.font = font;
        root.dataset.position = position;
        root.style.setProperty('--frame-text', String(textSize / 100));
    }

    $effect(applyLook);

    // /frame is client-rendered, so SvelteKit's start-up navigation focuses its <body> —
    // which pulls this page's focus into the preview, out of reach of Escape, keyboard
    // shortcuts and whatever field was being typed in. Hand it straight back.
    $effect(() => {
        let lastFocus: HTMLElement | null = null;
        const onFocusIn = (e: FocusEvent) => {
            if (e.target !== iframe) lastFocus = e.target as HTMLElement;
        };
        const onBlur = () =>
            setTimeout(() => {
                if (!iframe || document.activeElement !== iframe) return;
                if (lastFocus?.isConnected) lastFocus.focus({ preventScroll: true });
                else iframe.blur();
            });
        document.addEventListener('focusin', onFocusIn);
        window.addEventListener('blur', onBlur);
        return () => {
            document.removeEventListener('focusin', onFocusIn);
            window.removeEventListener('blur', onBlur);
        };
    });

    const size = $derived(
        frameCanvas({
            width: frameSettings.width,
            height: frameSettings.height,
            orientation: frameSettings.orientation,
        })
    );
    // Fit the width available, and the height left after the controls and caption.
    let boxWidth = $state(0);
    // Measured apart from the picture, so its own size never feeds back into the fit.
    let controlsHeight = $state(0);
    let footerHeight = $state(0);
    const scale = $derived.by(() => {
        const byWidth = boxWidth / size.w;
        if (!maxHeight) return byWidth;
        return Math.max(0, Math.min(byWidth, (maxHeight - controlsHeight - footerHeight) / size.h));
    });
</script>

<!--
@component
Live preview of the real /frame page at panel resolution, scaled to fit, with
mode and Color/B&W toggles, an optional full-size button and the frame URL.
-->
<div>
    <div bind:offsetHeight={controlsHeight} class="flex flex-wrap items-center gap-3 pb-3">
        <SegmentedControl
            options={frameModes.map((m) => ({ value: m.id, label: m.label }))}
            value={shared.mode}
            onchange={(v) => (shared.mode = v)}
            label="Preview mode"
        />
        <SegmentedControl
            options={[
                { value: 'color', label: 'Color' },
                { value: 'bw', label: 'B&W' },
            ]}
            value={shared.palette}
            onchange={(v) => (shared.palette = v)}
            label="Palette"
        />
        <SegmentedControl
            options={[
                { value: 'live', label: 'Live' },
                { value: 'screen', label: 'Screen' },
                { value: 'paper', label: 'Paper' },
            ]}
            value={shared.view}
            onchange={(v) => {
                // Coming back after a failed render: clear it so the effect asks again
                if (renderFailed) renderSrc = '';
                shared.view = v;
            }}
            label="Preview view"
        />
        {#if onexpand}
            <IconButton label="Full-size preview" title="Full-size preview" onclick={onexpand} class="ml-auto">
                <Maximize2 class="icon-sm" aria-hidden="true" />
            </IconButton>
        {/if}
    </div>

    <!-- With a height budget the height sets the size; without one, cap the width so a portrait frame isn't huge -->
    <div bind:clientWidth={boxWidth} class={maxHeight ? '' : size.h > size.w ? 'mx-auto max-w-sm' : 'max-w-2xl'}>
        <div
            class="relative mx-auto overflow-hidden rounded-lg border border-(--border)"
            style="width: {size.w * scale}px; height: {size.h * scale}px"
        >
            {#if renderSrc}
                <img
                    src={renderSrc}
                    alt="Dithered frame preview"
                    onload={() => ((rendering = false), (renderFailed = false))}
                    onerror={() => ((rendering = false), (renderFailed = true))}
                    class="absolute inset-0 h-full w-full {shared.view === 'live' ? 'hidden' : ''}"
                />
            {/if}
            {#if shared.view !== 'live' && (rendering || renderFailed || !renderSrc)}
                <div
                    class="type-label absolute inset-0 flex items-center justify-center bg-(--surface)/80 text-(--text-2)"
                >
                    {renderFailed ? 'Couldn’t render the frame — is the renderer running?' : 'Rendering…'}
                </div>
            {/if}
            {#if shared.view === 'live' && boxWidth > 0 && photo !== undefined}
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
    </div>
    <div bind:offsetHeight={footerHeight} class="pt-2">
        <p class="type-caption text-(--text-2)">
            {#if shared.view === 'live'}
                Shown before dithering — on the panel, fills become an even dot texture.
            {:else}
                {shared.view === 'screen'
                    ? 'The exact pixels the panel will show, in screen colours.'
                    : 'Approximate panel colours — e-paper is greyer and less saturated than a screen.'}
                {#if renderSrc}
                    <a href={renderSrc} target="_blank" rel="noopener noreferrer" class="underline">Open full size</a>.
                {/if}
            {/if}
        </p>
        <Disclosure class="type-caption">
            {#snippet summary()}Frame URL{/snippet}
            <a
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                class="font-mono break-all text-(--text-2) underline">{url}</a
            >
        </Disclosure>
    </div>
</div>
