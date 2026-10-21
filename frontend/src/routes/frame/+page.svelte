<script lang="ts">
    import { onMount } from 'svelte';
    import { setLucideProps } from '@lucide/svelte';
    import { page } from '$app/state';
    import type { Photo } from '$lib/api';
    import { loadWeather } from '$lib/stores/WeatherStore.svelte.ts';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { loadCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { loadAlmanac } from '$lib/stores/AlmanacStore.svelte.ts';
    import { loadDailyQuote } from '$lib/stores/DailyQuoteStore.svelte.ts';
    import { loadBirds } from '$lib/stores/BirdsStore.svelte.ts';
    import { parseFrameLayout, type FrameWidgetId } from '$lib/constants/frameWidgets';
    import { SECOND } from '$lib/utils';
    import { isAmbientPosition } from '$lib/frame';
    import { pickPhoto } from '$lib/photos';
    import FrameAmbient from '$lib/components/frame/FrameAmbient.svelte';
    import FrameDaily from '$lib/components/frame/FrameDaily.svelte';
    import { frameDataTheme, isValidFrameThemeId } from '$lib/constants/frameThemes';
    import { isValidFontThemeId } from '$lib/constants/fontThemes';
    import { frameSettings, loadFrameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';

    // The e-paper render target: a fixed-size canvas that the (future) Display service
    // screenshots and dithers. Everything it needs comes from the URL, because a
    // headless renderer starts with empty localStorage. A param the URL leaves out
    // falls back to the shared Settings → Frame choices held by the Display service
    // (theme, font, orientation, layout), then to the defaults:
    //   ?mode=ambient|daily          (default ambient, per PRODUCT.md)
    //   ?palette=color|bw            (default color — the Color ↔ B&W mode)
    //   ?theme=almanac|garden|…      (colour theme, see constants/frameThemes.ts;
    //                                 B&W follows its lightness)
    //   ?font=inter|lora|…           (any font preset from constants/fontThemes.ts)
    //   ?orientation=landscape|portrait
    //       landscape = 1600×1200, the panel's native shape (default); portrait =
    //       1200×1600 for a frame hung on its side — the Display service rotates
    //       the capture 90° before sending it to the panel.
    //   ?left=a,b&right=c&split=50   (Daily widget columns, ids from constants/frameWidgets.ts,
    //                                 and the left column's width %; a missing param
    //                                 uses the default layout)
    //   ?position=top-left|top-middle|top-right|bottom-left|bottom-middle|bottom-right
    //                                (Ambient info card)
    //   ?source=unsplash|local|both   (Ambient photos; default: the shared choice)
    //   ?categories=a,b       (Ambient Unsplash categories; default: the shared choice)
    // The root's data-ready flips to "true" once data, fonts and the photo have
    // settled, or after READY_DEADLINE_MS regardless, so one hung upstream can't
    // hold back the whole frame — screenshot after that. No polling: the renderer
    // reloads per frame.
    const params = page.url.searchParams;
    const mode = params.get('mode') === 'daily' ? 'daily' : 'ambient';
    const palette = params.get('palette') === 'bw' ? 'bw' : 'color';
    const themeParam = params.get('theme');
    const theme = $derived(isValidFrameThemeId(themeParam) ? themeParam : frameSettings.theme);
    const fontParam = params.get('font');
    const font = $derived(isValidFontThemeId(fontParam) ? fontParam : frameSettings.font);
    const orientationParam = params.get('orientation');
    const orientation = $derived(
        orientationParam === 'portrait' || orientationParam === 'landscape'
            ? orientationParam
            : frameSettings.orientation
    );
    const positionParam = params.get('position');
    const position = $derived(isAmbientPosition(positionParam) ? positionParam : frameSettings.ambientPosition);
    const sourceParam = params.get('source');
    const categoriesParam = params.get('categories');
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
    let ready = $state(false);

    async function loadPhoto() {
        const next = await pickPhoto(
            sourceParam || frameSettings.photoSource,
            categoriesParam ? categoriesParam.split(',') : frameSettings.photoCategories,
            orientation
        );
        if (!next) return;
        // Decode before signalling ready so the screenshot never catches a blank frame.
        await new Promise<void>((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve();
            img.onerror = () => reject(new Error(`failed to load ${next.url}`));
            img.src = next.url;
        });
        photo = next;
    }

    function dataLoads(): Promise<unknown>[] {
        if (mode === 'ambient') return [loadWeather(), loadPhoto()];
        const extras = [...layout.left, ...layout.right].flatMap((id) => widgetLoaders[id] ?? []);
        return [loadWeather(), loadTasks(), loadCalendarItems(), loadAlmanac(), ...extras.map((load) => load())];
    }

    onMount(async () => {
        // Settings first: the layout decides which data to load.
        await loadFrameSettings();
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
>
    <!-- Wait for the shared settings so the layout is measured once, in its final shape. -->
    {#if frameSettings.loaded}
        {#if mode === 'daily'}
            <FrameDaily {now} {orientation} {layout} split={layout.split} />
        {:else}
            <FrameAmbient {now} {photo} />
        {/if}
    {/if}
</div>

<style>
    /* Panel-native size, never scrolls. Type and icon sizes are fixed px tuned for
       reading at 3–4 m on a ~150 PPI panel; overriding the tokens here resizes the
       shared .type-* / .icon-* utilities, so reused widgets scale with the frame.
       Type multiplies by the font preset's --font-scale (its optical correction,
       from the data-font block) but deliberately not the dashboard's user size
       slider, which is a per-browser preference. */
    .frame {
        position: relative;
        width: 1600px;
        height: 1200px;
        overflow: hidden;

        --font-display: calc(112px * var(--font-scale, 1));
        --font-title: calc(48px * var(--font-scale, 1));
        --font-subtitle: calc(38px * var(--font-scale, 1));
        --font-body: calc(32px * var(--font-scale, 1));
        --font-label: calc(26px * var(--font-scale, 1));
        --font-caption: calc(22px * var(--font-scale, 1));
        /* Frame-only display sizes (.type-weekday/-date/-hero/-stamp), set in --display-family */
        --font-weekday: calc(112px * var(--font-scale, 1));
        --font-date: calc(128px * var(--font-scale, 1));
        --font-hero: calc(100px * var(--font-scale, 1));
        --font-stamp: calc(80px * var(--font-scale, 1));

        --icon-lg: 56px;
        --icon-md: 40px;
        --icon-sm: 30px;
        --icon-xs: 24px;
    }

    .frame.portrait {
        width: 1200px;
        height: 1600px;
        /* Smaller weekday so the masthead stays one row beside the compact sun path */
        --font-weekday: calc(88px * var(--font-scale, 1));
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
        height: 16px;
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
