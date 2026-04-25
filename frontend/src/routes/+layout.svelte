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

<div class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] transition-colors duration-300">
  <Nav {theme} onChangeTheme={(id) => (theme = id)} />
  {@render children()}
</div>
