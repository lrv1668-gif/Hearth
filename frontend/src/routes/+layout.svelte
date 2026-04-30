<script lang="ts">
  import '../app.css';
  import { browser } from '$app/environment';
  import Nav from '$lib/components/Nav.svelte';
  import { loadTasks } from '$lib/TaskStore';
  import { isValidThemeId, DEFAULT_THEME, type ThemeId } from '$lib/themes';

  let { children } = $props();

  const stored = browser ? localStorage.getItem('hearth-theme') : null;
  let theme = $state<ThemeId>(isValidThemeId(stored) ? stored : DEFAULT_THEME);

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
  <link href="https://fonts.googleapis.com/css2?family=Spectral:ital,wght@0,400;0,600;0,700;1,400&display=swap" rel="stylesheet">
</svelte:head>

<div class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] transition-colors duration-300 pb-16 md:pb-0">
  <Nav {theme} onChangeTheme={(id) => (theme = id)} />
  {@render children()}
</div>
