<script lang="ts">
    import { onMount } from 'svelte';
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
    import FrameAmbient from '$lib/components/frame/FrameAmbient.svelte';
    import FrameDaily from '$lib/components/frame/FrameDaily.svelte';
    import { DEFAULT_FRAME_THEME, frameDataTheme, isValidFrameThemeId } from '$lib/constants/frameThemes';
    import { DEFAULT_FONT_THEME, isValidFontThemeId } from '$lib/constants/fontThemes';

    // The e-paper render target: a fixed-size canvas that the (future) Display service
    // screenshots and dithers. Everything it needs comes from the URL, because a
    // headless renderer starts with empty localStorage:
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
    //   ?categories=a,b       (ambient photo query, default nature)
    // The root's data-ready flips to "true" once data, fonts and the photo have
    // settled, or after READY_DEADLINE_MS regardless, so one hung upstream can't
    // hold back the whole frame — screenshot after that. No polling: the renderer
    // reloads per frame.
    const params = page.url.searchParams;
    const mode = params.get('mode') === 'daily' ? 'daily' : 'ambient';
    const palette = params.get('palette') === 'bw' ? 'bw' : 'color';
    const themeParam = params.get('theme');
    const theme = isValidFrameThemeId(themeParam) ? themeParam : DEFAULT_FRAME_THEME;
    const fontParam = params.get('font');
    const font = isValidFontThemeId(fontParam) ? fontParam : DEFAULT_FONT_THEME;
    const orientation = params.get('orientation') === 'portrait' ? 'portrait' : 'landscape';
    const categories = params.get('categories') || 'nature';
    const layout = parseFrameLayout(params);

    const READY_DEADLINE_MS = 15 * SECOND;

    // Extra data each slot widget needs. Weather, tasks and calendar always load in
    // Daily mode — the masthead and Today's Arc use them.
    const widgetLoaders: Partial<Record<FrameWidgetId, () => Promise<void>>> = {
        almanac: loadAlmanac,
        quote: loadDailyQuote,
        birds: loadBirds,
    };

    const now = new Date();
    let photo = $state<Photo | null>(null);
    let ready = $state(false);

    async function loadPhoto() {
        const next = await api.photos.random(categories, orientation, 'unsplash');
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
        return [loadWeather(), loadTasks(), loadCalendarItems(), ...extras.map((load) => load())];
    }

    onMount(async () => {
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
    data-ready={ready}
>
    {#if mode === 'daily'}
        <FrameDaily {now} {orientation} {layout} split={layout.split} />
    {:else}
        <FrameAmbient {now} {photo} />
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

        --icon-lg: 56px;
        --icon-md: 40px;
        --icon-sm: 30px;
        --icon-xs: 24px;
    }

    .frame.portrait {
        width: 1200px;
        height: 1600px;
    }

    /* B&W mode greyscales the ambient photo. Keyed off data-theme (not a prop) so
       the Settings preview can switch palettes in place without reloading. */
    .frame:is([data-theme='epaper-bw'], [data-theme='epaper-bw-night']) :global(img) {
        filter: grayscale(1);
    }

    /* E-paper shows a single still frame — no motion to settle before the screenshot. */
    .frame :global(*) {
        transition: none !important;
        animation: none !important;
    }
</style>
