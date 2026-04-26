<script lang="ts">
  import { tasks, toggleTask, removeTask } from '$lib/TaskStore';
  import Schedule from '$lib/components/Schedule.svelte';
  import NowPlaying from '$lib/components/NowPlaying.svelte';
</script>

<svelte:head>
  <title>Hearth — Schedule</title>
</svelte:head>

<main class="max-w-3xl mx-auto px-8 py-8 flex flex-col gap-6">
  <NowPlaying />
  <Schedule tasks={$tasks} onToggle={toggleTask} onDelete={removeTask} />
</main>
