<script lang="ts">
    import { onMount } from 'svelte';
    import { setLucideProps } from '@lucide/svelte';
    import { page } from '$app/state';
    import { api, type Photo } from '$lib/api';
    import { loadWeather } from '$lib/stores/WeatherStore.svelte.ts';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { loadCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { loadAlmanac } from '$lib/stores/AlmanacStore.svelte.ts';
    import { loadDailyQuote } from '$lib/stores/DailyQuoteStore.svelte.ts';
    import { loadBirds } from '$lib/stores/BirdsStore.svelte.ts';
    import { parseFrameLayout, type FrameWidgetId } from '$lib/constants/frameWidgets';
    import { SECOND } from '$lib/utils';
    import {
        frameCanvas,
        isAmbientPosition,
        isFrameSize,
        isFrameTextSize,
        setFrameScale,
        type Pixels,
    } from '$lib/frame';
    import { parsePhotoParam, pickPhoto, pickPinned } from '$lib/photos';
    import FrameAmbient from '$lib/components/frame/FrameAmbient.svelte';
    import FrameDaily from '$lib/components/frame/FrameDaily.svelte';
    import FrameCountdownHero from '$lib/components/frame/modes/FrameCountdownHero.svelte';
    import FrameNight from '$lib/components/frame/modes/FrameNight.svelte';
    import FrameNote from '$lib/components/frame/modes/FrameNote.svelte';
    import { DEFAULT_FRAME_MODE, isFrameMode, type FrameModeId } from '$lib/constants/frameModes';
    import { frameDataTheme, isValidFrameThemeId } from '$lib/constants/frameThemes';
    import { isValidFontThemeId } from '$lib/constants/fontThemes';
    import { frameSettings, loadFrameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';

    // The e-paper render target: a fixed-size canvas that the (future) Display service
    // screenshots and dithers. Everything it needs comes from the URL, because a
    // headless renderer starts with empty localStorage. A param the URL leaves out
    // falls back to the shared Settings → Frame choices held by the Display service
    // (theme, font, text size, orientation, panel size, layout), then to the defaults:
    //   ?mode=ambient|daily|photo|countdown|night|note
    //                                (default: what the Display service resolves now —
    //                                 an override, the schedule, or its default mode —
    //                                 else ambient; see constants/frameModes.ts)
    //   ?pin=<photo id>              (with ?mode=photo: the upload "Show on frame" pinned —
    //                                 Display passes it with the mode it resolved)
    //   ?palette=color|bw            (default color — the Color ↔ B&W mode)
    //   ?theme=almanac|garden|…      (colour theme, see constants/frameThemes.ts;
    //                                 B&W follows its lightness)
    //   ?font=inter|lora|…           (any font preset from constants/fontThemes.ts)
    //   ?text=100–150                (percent on the reading type and icons — Settings →
    //                                 Frame's text size; see isFrameTextSize)
    //   ?orientation=landscape|portrait
    //       landscape = the panel's native shape (default); portrait swaps width and
    //       height for a frame hung on its side — the capture is rotated 90° before
    //       it goes to the panel.
    //   ?width=1600&height=1200      (the panel's native size, width ≥ height; default
    //                                 the Inky Impression 13.3". Type, icons and
    //                                 spacing scale to fit it — see frameCanvas)
    //   ?left=a,b&right=c&split=50   (Daily widget columns, ids from constants/frameWidgets.ts,
    //                                 and the left column's width %; a missing param
    //                                 uses the default layout)
    //   ?position=top-left|top-middle|top-right|bottom-left|bottom-middle|bottom-right
    //                                (Ambient info card)
    //   ?source=unsplash|local|both   (Ambient photos; default: the shared choice)
    //   ?categories=a,b       (Ambient Unsplash categories; default: the shared choice)
    //   ?photo={...}          (a JSON Photo shown instead of a random pick — Settings'
    //                          preview pins one so its Live/Screen/Paper views match)
    // The root's data-ready flips to "true" once data, fonts and the photo have
    // settled, or after READY_DEADLINE_MS regardless, so one hung upstream can't
    // hold back the whole frame — screenshot after that. No polling: the renderer
    // reloads per frame.
    const params = page.url.searchParams;
    const modeParam = params.get('mode');
    // Null until resolved: the URL's mode, else Display's (onMount).
    let mode = $state<FrameModeId | null>(isFrameMode(modeParam) ? modeParam : null);
    // The upload "Show on frame" pinned, when Display resolved to it (or named it in ?pin=).
    let pinnedPhotoId: string | null = isFrameMode(modeParam) ? params.get('pin') || null : null;
    const palette = params.get('palette') === 'bw' ? 'bw' : 'color';
    const themeParam = params.get('theme');
    const theme = $derived(isValidFrameThemeId(themeParam) ? themeParam : frameSettings.theme);
    const fontParam = params.get('font');
    const font = $derived(isValidFontThemeId(fontParam) ? fontParam : frameSettings.font);
    const textParam = Number(params.get('text'));
    const textSize = $derived(isFrameTextSize(textParam) ? textParam : frameSettings.textSize);
    const orientationParam = params.get('orientation');
    const orientation = $derived(
        orientationParam === 'portrait' || orientationParam === 'landscape'
            ? orientationParam
            : frameSettings.orientation
    );
    const widthParam = Number(params.get('width'));
    const heightParam = Number(params.get('height'));
    const canvas = $derived(
        frameCanvas({
            ...(isFrameSize(widthParam, heightParam)
                ? { width: widthParam, height: heightParam }
                : { width: frameSettings.width, height: frameSettings.height }),
            orientation,
        })
    );
    setFrameScale(() => canvas.scale);
    const positionParam = params.get('position');
    const position = $derived(isAmbientPosition(positionParam) ? positionParam : frameSettings.ambientPosition);
    const sourceParam = params.get('source');
    const categoriesParam = params.get('categories');
    const photoParam = parsePhotoParam(params.get('photo'));
    const layout = $derived(parseFrameLayout(params, { ...frameSettings.layout, split: frameSettings.split }));

    const READY_DEADLINE_MS = 15 * SECOND;

    // One 3px line weight across the frame: icons keep the stroke of the rules and
    // check boxes at every size, instead of scaling it with the icon.
    setLucideProps({ strokeWidth: 3, absoluteStrokeWidth: true });

    // Extra data each slot widget needs. Weather, tasks, calendar and the almanac
    // always load in Daily mode — the edition bar, masthead and sun path use them.
    const widgetLoaders: Partial<Record<FrameWidgetId, () => Promise<void>>> = {
        quote: loadDailyQuote,
        birds: loadBirds,
    };

    const now = new Date();
    let photo = $state<Photo | null>(null);
    // The photo as the frame shows it (cover-cropped), at 1/8 size — Ambient mode
    // reads the colours around its plate from this to pick the date block's ink.
    let photoPixels = $state<Pixels | null>(null);
    let ready = $state(false);

    /** Photo mode's pinned upload or album, or null (none chosen, or all deleted). */
    async function pinnedPhoto(): Promise<Photo | null> {
        const ids = pinnedPhotoId ? [pinnedPhotoId] : frameSettings.photoIds;
        return ids.length > 0 ? pickPinned(ids, await api.photos.list()) : null;
    }

    async function loadPhoto() {
        // Photo mode falls back to Ambient's pick, so the frame never goes blank.
        const next =
            photoParam ??
            (mode === 'photo' ? await pinnedPhoto() : null) ??
            (await pickPhoto(
                sourceParam || frameSettings.photoSource,
                categoriesParam ? categoriesParam.split(',') : frameSettings.photoCategories,
                orientation
            ));
        if (!next) return;
        // Decode before signalling ready so the screenshot never catches a blank frame.
        // CORS mode so the pixels can be read back (Unsplash's CDN allows it; uploads
        // are same-origin) — the <img> asks the same way, so it reuses this fetch.
        const img = new Image();
        img.crossOrigin = 'anonymous';
        await new Promise<void>((resolve, reject) => {
            img.onload = () => resolve();
            img.onerror = () => reject(new Error(`failed to load ${next.url}`));
            img.src = next.url;
        });
        photoPixels = coverPixels(img, [Math.round(canvas.w / 8), Math.round(canvas.h / 8)]);
        photo = next;
    }

    /** `img` drawn like CSS object-cover into a w×h canvas, or null if it can't be read. */
    function coverPixels(img: HTMLImageElement, [w, h]: [number, number]): Pixels | null {
        const scale = Math.max(w / img.naturalWidth, h / img.naturalHeight);
        const sw = w / scale;
        const sh = h / scale;
        const canvas = document.createElement('canvas');
        canvas.width = w;
        canvas.height = h;
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        if (!ctx) return null;
        ctx.drawImage(img, (img.naturalWidth - sw) / 2, (img.naturalHeight - sh) / 2, sw, sh, 0, 0, w, h);
        try {
            return ctx.getImageData(0, 0, w, h);
        } catch {
            return null; // a tainted canvas (no CORS headers) — keep the theme's block ink
        }
    }

    function dataLoads(): Promise<unknown>[] {
        if (mode === 'ambient' || mode === 'photo') return [loadWeather(), loadPhoto()];
        if (mode === 'countdown') return [loadTasks()];
        if (mode === 'night') return [loadWeather()];
        if (mode === 'note') return [];
        const extras = [...layout.left, ...layout.right].flatMap((id) => widgetLoaders[id] ?? []);
        return [loadWeather(), loadTasks(), loadCalendarItems(), loadAlmanac(), ...extras.map((load) => load())];
    }

    onMount(async () => {
        // Settings first: the layout decides which data to load.
        await loadFrameSettings();
        if (!mode) {
            const resolved = await api.display.mode();
            mode = resolved?.mode ?? DEFAULT_FRAME_MODE;
            pinnedPhotoId = resolved?.photo_id ?? null;
        }
        const settled = Promise.allSettled(dataLoads()).then(() => document.fonts.ready);
        await Promise.race([settled, new Promise((resolve) => setTimeout(resolve, READY_DEADLINE_MS))]);
        ready = true;
    });
</script>

<svelte:head>
    <title>Hearth — Frame</title>
</svelte:head>

<div
    class="frame {orientation} bg-(--bg) text-(--text-1)"
    data-theme={frameDataTheme(theme, palette)}
    data-font={font}
    data-mode={mode}
    data-orientation={orientation}
    data-position={position}
    data-ready={ready}
    style="width: {canvas.w}px; height: {canvas.h}px; --frame-scale: {canvas.scale}; --frame-text: {textSize / 100}"
>
    <!-- Wait for the shared settings so the layout is measured once, in its final shape. -->
    {#if frameSettings.loaded && mode}
        {#if mode === 'daily'}
            <FrameDaily {now} {orientation} {layout} split={layout.split} />
        {:else if mode === 'countdown'}
            <FrameCountdownHero {now} countdownId={frameSettings.countdownId} />
        {:else if mode === 'night'}
            <FrameNight {now} />
        {:else if mode === 'note'}
            <FrameNote title={frameSettings.noteTitle} body={frameSettings.noteBody} />
        {:else}
            <FrameAmbient
                {now}
                {photo}
                pixels={photoPixels}
                card={mode === 'photo' ? frameSettings.photoCard : 'full'}
            />
        {/if}
    {/if}
</div>

<style>
    /* Panel-sized (inline, from frameCanvas), never scrolls. Type and icon sizes are
       px tuned for reading at 3–4 m on the 1600×1200, ~150 PPI 13.3" panel, times
       --frame-scale for other panels; overriding the tokens here resizes the shared
       .type-* / .icon-* utilities, so reused widgets scale with the frame. Tailwind's
       --spacing scales too, so padding and gaps keep their proportions; 3px lines don't.
       Type multiplies by the font preset's --font-scale (its optical correction,
       from the data-font block) but deliberately not the dashboard's user size
       slider, which is a per-browser preference. The reading sizes (title down to
       caption) and icons also take --frame-text, the frame's own shared text size;
       the display faces don't — they're sized to fit the masthead and hero rows. */
    .frame {
        position: relative;
        overflow: hidden;
        --spacing: calc(0.25rem * var(--frame-scale, 1));

        --font-display: calc(112px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-title: calc(var(--frame-text, 1) * 48px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-subtitle: calc(var(--frame-text, 1) * 38px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-body: calc(var(--frame-text, 1) * 32px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-label: calc(var(--frame-text, 1) * 26px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-caption: calc(var(--frame-text, 1) * 22px * var(--font-scale, 1) * var(--frame-scale, 1));
        /* Frame-only display sizes (.type-weekday/-date/-hero/-stamp/-poster), set in --display-family */
        --font-weekday: calc(112px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-date: calc(128px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-hero: calc(100px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-stamp: calc(80px * var(--font-scale, 1) * var(--frame-scale, 1));
        --font-poster: calc(400px * var(--font-scale, 1) * var(--frame-scale, 1));

        --icon-lg: calc(var(--frame-text, 1) * 56px * var(--frame-scale, 1));
        --icon-md: calc(var(--frame-text, 1) * 40px * var(--frame-scale, 1));
        --icon-sm: calc(var(--frame-text, 1) * 30px * var(--frame-scale, 1));
        --icon-xs: calc(var(--frame-text, 1) * 24px * var(--frame-scale, 1));
    }

    .frame.portrait {
        /* Smaller weekday so the masthead stays one row beside the compact sun path */
        --font-weekday: calc(88px * var(--font-scale, 1) * var(--frame-scale, 1));
    }

    /* B&W mode greyscales the ambient photo. Keyed off data-theme (not a prop) so
       the Settings preview can switch palettes in place without reloading. */
    .frame:is([data-theme='epaper-bw'], [data-theme='epaper-bw-night']) :global(img) {
        filter: grayscale(1);
    }

    /* Shared widget drawings at the frame's line weight; the meter becomes an outlined
       pure-ink bar — a tinted track would dither into speckle. */
    .frame {
        --stroke: 3px;
    }
    .frame :global(.meter) {
        height: calc(16px * var(--frame-scale, 1));
        border: 3px solid var(--border);
        border-radius: 0;
        background: none;
    }

    /* The frame's one texture: vertical 3px pure-ink lines every 9px, pixel-aligned,
       so ordered dithering leaves them exact where a grey tint would speckle. */
    .frame :global(.hatch) {
        background-image: repeating-linear-gradient(90deg, var(--border) 0 3px, transparent 3px 9px);
    }

    /* Display numerals and the masthead use lining, tabular figures so columns of
       numbers line up. */
    .frame {
        font-variant-numeric: lining-nums tabular-nums;
    }

    /* E-paper shows a single still frame — no motion to settle before the screenshot. */
    .frame :global(*) {
        transition: none !important;
        animation: none !important;
    }
</style>
