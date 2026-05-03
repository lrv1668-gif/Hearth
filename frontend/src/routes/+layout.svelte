<script lang="ts">
    import '../app.css';
    import { browser } from '$app/environment';
    import Nav from '$lib/components/Nav.svelte';
    import { loadTasks } from '$lib/stores/TaskStore';

    let { children } = $props();

    $effect(() => {
        if (browser) loadTasks();
    });
</script>

<svelte:head>
    <link rel="icon" href="/favicon.ico" />
</svelte:head>

<div class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] transition-colors duration-300 pb-16 md:pb-0">
    <Nav />
    {@render children()}
</div>
