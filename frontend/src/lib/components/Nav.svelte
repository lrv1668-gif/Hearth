<script lang="ts">
  import { CalendarDays, LayoutList } from '@lucide/svelte';
  import ThemeSwitcher from './ThemeSwitcher.svelte';

  interface Props {
    theme: string;
    onChangeTheme: (id: string) => void;
  }

  let { theme, onChangeTheme }: Props = $props();

  const now = new Date();
  const dateLabel = now.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
</script>

<!-- Desktop masthead -->
<header class="border-b border-[var(--border)] bg-[var(--bg)] hidden md:block">
  <div class="grid grid-cols-3 items-center px-8 pt-4 pb-2">
    <span class="text-xs text-[var(--text-3)] tracking-wide">{dateLabel}</span>
    <a href="/" class="text-center">
      <span class="font-serif text-2xl font-bold tracking-[0.25em] uppercase text-[var(--text-1)] hover:text-[var(--text-2)] transition-colors">
        Hearth
      </span>
    </a>
    <div class="flex justify-end">
      <ThemeSwitcher {theme} onChange={onChangeTheme} />
    </div>
  </div>

  <div class="mx-8 border-t-2 border-[var(--text-1)]"></div>

  <nav class="flex justify-center gap-8 py-2">
    <a
      href="/"
      class="text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
    >
      Schedule
    </a>
    <a
      href="/calendar"
      class="text-xs tracking-widest uppercase transition-colors
             text-[var(--text-3)] hover:text-[var(--text-1)]
             aria-[current=page]:text-[var(--text-1)] aria-[current=page]:font-semibold"
    >
      Calendar
    </a>
  </nav>
</header>

<!-- Mobile: compact top bar -->
<header class="flex items-center justify-between px-4 py-3 border-b border-[var(--border)] bg-[var(--bg)] md:hidden">
  <a href="/" class="font-serif text-lg font-bold tracking-widest uppercase text-[var(--text-1)]">Hearth</a>
  <ThemeSwitcher {theme} onChange={onChangeTheme} />
</header>

<!-- Mobile bottom nav -->
<nav class="fixed bottom-0 left-0 right-0 flex justify-around border-t border-[var(--border)] bg-[var(--bg)] py-2 z-50 md:hidden">
  <a
    href="/"
    class="flex flex-col items-center gap-0.5 px-6 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
  >
    <LayoutList size={20} />
    <span class="text-[10px] tracking-wide uppercase">Schedule</span>
  </a>
  <a
    href="/calendar"
    class="flex flex-col items-center gap-0.5 px-6 py-1 text-[var(--text-3)]
           hover:text-[var(--text-1)] aria-[current=page]:text-[var(--text-1)] transition-colors"
  >
    <CalendarDays size={20} />
    <span class="text-[10px] tracking-wide uppercase">Calendar</span>
  </a>
</nav>
