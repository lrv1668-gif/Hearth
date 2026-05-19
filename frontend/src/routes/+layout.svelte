<script lang="ts">
    import '../app.css';
    import { browser } from '$app/environment';
    import Nav from '$lib/components/Nav.svelte';
    import { loadTasks } from '$lib/stores/TaskStore.svelte.ts';
    import { initTheme } from '$lib/stores/ThemeStore.svelte.ts';
    let { children } = $props();

    let loaded = $state(false);

    $effect(() => {
        if (browser) {
            initTheme();
            loadTasks();
            loaded = true;
        }
    });
</script>

<svelte:head>
    <link rel="icon" href="/favicon.ico" />
</svelte:head>

{#if loaded}
    <div class="grid h-[100dvh] grid-rows-[auto_1fr] bg-[var(--bg)] pb-16 text-[var(--text-1)] transition-colors duration-300 lg:pb-0">
        <Nav />
        {@render children()}
    </div>
{/if}
