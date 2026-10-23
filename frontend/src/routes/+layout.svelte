<script lang="ts">
    import '../app.css';
    import { browser } from '$app/environment';
    import { page } from '$app/state';
    import Nav from '$lib/components/Nav.svelte';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { initTheme } from '$lib/stores/ThemeStore.svelte.ts';
    import { initFontTheme } from '$lib/stores/FontThemeStore.svelte.ts';
    import { initFontSize } from '$lib/stores/FontSizeStore.svelte.ts';
    import { loadCalendarStatus, loadCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    import { MINUTE, poll } from '$lib/utils';
    let { children } = $props();

    let loaded = $state(false);

    // The e-paper render target draws its own fixed-size canvas — no nav or app chrome.
    const isFrame = $derived(page.url.pathname.startsWith('/frame'));

    $effect(() => {
        if (browser) {
            initTheme();
            initFontTheme();
            initFontSize();
            // /frame loads (and awaits) exactly the data its layout needs — skip it here.
            if (!isFrame) {
                loadTasks();
                loadCalendarStatus();
                loadCalendarItems(); // fire-and-forget; returns [] silently when not authenticated
            }
            loaded = true;
        }
    });

    // An always-on screen picks up tasks added from a phone and newly synced events.
    $effect(() => {
        if (!browser || isFrame) return;
        const stopTasks = poll(loadTasks, MINUTE);
        const stopCalendar = poll(loadCalendarItems, 5 * MINUTE);
        return () => {
            stopTasks();
            stopCalendar();
        };
    });
</script>

<svelte:head>
    <link rel="icon" href="/favicon.ico" />
</svelte:head>

{#if loaded && isFrame}
    {@render children()}
{:else if loaded}
    <div
        class="flex h-[100dvh] flex-col bg-(--bg) pr-[env(safe-area-inset-right)] pb-[calc(4rem+env(safe-area-inset-bottom))] pl-[env(safe-area-inset-left)] text-(--text-1) transition-colors duration-300 lg:pb-0"
    >
        <Nav />
        {@render children()}
    </div>
{/if}
