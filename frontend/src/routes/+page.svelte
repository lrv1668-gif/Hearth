<script lang="ts">
  import { tasks, toggleTask, removeTask } from '$lib/TaskStore';
  import Schedule from '$lib/components/Schedule.svelte';
  import NowPlaying from '$lib/components/NowPlaying.svelte';
</script>

<svelte:head>
  <title>Hearth — Schedule</title>
</svelte:head>

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 md:py-8">
  <div class="grid grid-cols-1 md:grid-cols-[3fr_2fr] gap-8 items-start">
    <!-- Left column: music + schedule -->
    <div class="flex flex-col gap-6">
      <NowPlaying />
      <Schedule tasks={$tasks} onToggle={toggleTask} onDelete={removeTask} />
    </div>

    <!-- Right column: date display + calendar teaser -->
    <div class="hidden md:flex flex-col gap-6">
      <div class="text-right border-b border-[var(--border)] pb-4">
        <p class="font-serif text-5xl font-bold text-[var(--text-1)] leading-tight">
          {new Date().toLocaleDateString('en-US', { day: 'numeric' })}
        </p>
        <p class="font-serif text-xl text-[var(--text-2)] mt-1">
          {new Date().toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}
        </p>
        <p class="text-xs tracking-widest uppercase text-[var(--text-3)] mt-2">
          {new Date().toLocaleDateString('en-US', { weekday: 'long' })}
        </p>
      </div>

      <div>
        <a
          href="/calendar"
          class="block text-xs tracking-widest uppercase text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors"
        >
          View Calendar →
        </a>
      </div>
    </div>
  </div>
</main>
