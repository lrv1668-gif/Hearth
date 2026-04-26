<script lang="ts">
  import '../app.css';
  import { browser } from '$app/environment';
  import Nav from '$lib/components/Nav.svelte';
  import { loadTasks } from '$lib/TaskStore';

  let { children } = $props();

  let theme = $state(browser ? (localStorage.getItem('hearth-theme') ?? 'stone') : 'stone');

  $effect(() => {
    document.documentElement.dataset.theme = theme;
    if (browser) localStorage.setItem('hearth-theme', theme);
  });

  $effect(() => {
    if (browser) loadTasks();
  });
</script>

<svelte:head>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="">
  <link href="https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,600;0,700;1,400&display=swap" rel="stylesheet">
</svelte:head>

<div class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] transition-colors duration-300 pb-16 md:pb-0">
  <Nav {theme} onChangeTheme={(id) => (theme = id)} />
  {@render children()}
</div>
