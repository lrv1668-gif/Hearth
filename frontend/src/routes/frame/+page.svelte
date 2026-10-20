<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/state';
    import { api, type Photo } from '$lib/api';
    import { loadWeather } from '$lib/stores/WeatherStore.svelte.ts';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { loadCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import FrameAmbient from '$lib/components/frame/FrameAmbient.svelte';
    import FrameDaily from '$lib/components/frame/FrameDaily.svelte';

    // The e-paper render target: a fixed 1600×1200 canvas that the (future) Display
    // service screenshots and dithers. Everything it needs comes from the URL, because
    // a headless renderer starts with empty localStorage:
    //   ?mode=ambient|daily   (default ambient, per PRODUCT.md)
    //   ?palette=color|bw     (default color)
    //   ?categories=a,b       (ambient photo query, default nature)
    // The root's data-ready flips to "true" once data, fonts and the photo have
    // settled — screenshot after that. No polling: the renderer reloads per frame.
    const params = page.url.searchParams;
    const mode = params.get('mode') === 'daily' ? 'daily' : 'ambient';
    const palette = params.get('palette') === 'bw' ? 'bw' : 'color';
    const categories = params.get('categories') || 'nature';

    const now = new Date();
    let photo = $state<Photo | null>(null);
    let ready = $state(false);

    async function loadPhoto() {
        const next = await api.photos.random(categories, 'landscape', 'unsplash');
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

    onMount(async () => {
        await Promise.allSettled([
            loadWeather(),
            ...(mode === 'daily' ? [loadTasks(), loadCalendarItems(), refreshNowPlaying()] : [loadPhoto()]),
        ]);
        await document.fonts.ready;
        ready = true;
    });
</script>

<svelte:head>
    <title>Hearth — Frame</title>
</svelte:head>

<div class="frame bg-(--bg) text-(--text-1)" data-theme="epaper-{palette}" data-mode={mode} data-ready={ready}>
    {#if mode === 'daily'}
        <FrameDaily {now} />
    {:else}
        <FrameAmbient {now} {photo} />
    {/if}
</div>

<style>
    /* Panel-native size, never scrolls. Type and icon sizes are fixed px tuned for
       reading at 3–4 m on a ~150 PPI panel; overriding the tokens here resizes the
       shared .type-* / .icon-* utilities, so reused widgets scale with the frame. */
    .frame {
        position: relative;
        width: 1600px;
        height: 1200px;
        overflow: hidden;

        --font-display: 112px;
        --font-title: 48px;
        --font-subtitle: 38px;
        --font-body: 32px;
        --font-label: 26px;
        --font-caption: 22px;

        --icon-lg: 56px;
        --icon-md: 40px;
        --icon-sm: 30px;
        --icon-xs: 24px;
    }

    /* E-paper shows a single still frame — no motion to settle before the screenshot. */
    .frame :global(*) {
        transition: none !important;
        animation: none !important;
    }
</style>
