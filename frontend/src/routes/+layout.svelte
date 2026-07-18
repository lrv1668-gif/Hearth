<script lang="ts">
    import '../app.css';
    import { browser } from '$app/environment';
    import Nav from '$lib/components/Nav.svelte';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { initTheme } from '$lib/stores/ThemeStore.svelte.ts';
    import { initFontTheme } from '$lib/stores/FontThemeStore.svelte.ts';
    import { initFontSize } from '$lib/stores/FontSizeStore.svelte.ts';
    import { loadCalendarStatus, loadCalendarItems } from '$lib/stores/CalendarStore.svelte.ts';
    let { children } = $props();

    let loaded = $state(false);

    $effect(() => {
        if (browser) {
            initTheme();
            initFontTheme();
            initFontSize();
            loadTasks();
            loadCalendarStatus();
            loadCalendarItems(); // fire-and-forget; returns [] silently when not authenticated
            loaded = true;
        }
    });
</script>

<svelte:head>
    <link rel="icon" href="/favicon.ico" />
</svelte:head>

{#if loaded}
    <div
        class="flex h-[100dvh] flex-col bg-[var(--bg)] pb-16 text-[var(--text-1)] transition-colors duration-300 lg:pb-0"
    >
        <Nav />
        {@render children()}
    </div>
{/if}
