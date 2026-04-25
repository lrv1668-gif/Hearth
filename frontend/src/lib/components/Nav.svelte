<script lang="ts">
  import { HouseHeart } from '@lucide/svelte';
  import ThemeSwitcher from './ThemeSwitcher.svelte';

  interface Props {
    theme: string;
    onChangeTheme: (id: string) => void;
  }

  let { theme, onChangeTheme }: Props = $props();
</script>

{#snippet navLinks()}
  <a
    href="/"
    class="px-3 py-1.5 rounded-lg text-sm transition-colors
           text-[var(--text-3)] hover:text-[var(--text-2)]
           aria-[current=page]:text-[var(--text-1)] aria-[current=page]:bg-[var(--surface)] aria-[current=page]:font-medium"
  >
    Schedule
  </a>
  <a
    href="/calendar"
    class="px-3 py-1.5 rounded-lg text-sm transition-colors
           text-[var(--text-3)] hover:text-[var(--text-2)]
           aria-[current=page]:text-[var(--text-1)] aria-[current=page]:bg-[var(--surface)] aria-[current=page]:font-medium"
  >
    Calendar
  </a>
{/snippet}

<header class="flex items-center gap-6 px-8 py-3 border-b border-[var(--border)] bg-[var(--bg)]">
  <a href="/" class="flex items-center gap-2 text-[var(--text-2)] hover:text-[var(--text-1)] transition-colors">
    <HouseHeart size="18" />
    <span class="text-sm font-light tracking-[0.3em] uppercase">Hearth</span>
  </a>

  <nav class="flex items-center gap-1">
    {@render navLinks()}
  </nav>

  <!-- Future mobile bottom bar: render navLinks() inside a
       `fixed bottom-0 w-full flex justify-around border-t bg-[var(--bg)] md:hidden` wrapper -->

  <div class="flex-1"></div>

  <ThemeSwitcher {theme} onChange={onChangeTheme} />
</header>
