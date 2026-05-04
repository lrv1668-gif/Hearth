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
    <div class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] transition-colors duration-300 pb-16 md:pb-0">
        <Nav />
        {@render children()}
    </div>
{/if}
