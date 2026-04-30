<script lang="ts">
  import { tasks, toggleTask, removeTask } from '$lib/TaskStore';
  import Schedule from '$lib/components/Schedule.svelte';
  import NowPlaying from '$lib/components/NowPlaying.svelte';
  import WeatherWidget from '$lib/components/WeatherWidget.svelte';
</script>

<svelte:head>
  <title>Hearth — Schedule</title>
</svelte:head>

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 md:py-8">
  <div class="grid grid-cols-1 md:grid-cols-[3fr_2fr] gap-8 items-start">
    <!-- Left column: schedule -->
    <div class="flex flex-col gap-4">
      <h2 class="font-serif text-xl font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3">Upcoming Tasks</h2>
      <Schedule tasks={$tasks} onToggle={toggleTask} onDelete={removeTask} />
    </div>

    <!-- Right column: date display + music + weather + calendar teaser -->
    <div class="hidden md:flex flex-col gap-8">
      <div>
        <h2 class="font-serif text-xl font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">Today's Date</h2>
        <div class="text-right">
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
      </div>
      
      <div>
        <h2 class="font-serif text-xl font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">Weather Forecast</h2>
        <WeatherWidget />
      </div>
      
      <div>
        <h2 class="font-serif text-xl font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">Now Playing</h2>
        <NowPlaying />
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
