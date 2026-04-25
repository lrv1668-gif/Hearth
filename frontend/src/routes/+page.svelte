<script lang="ts">
  import { browser } from "$app/environment";
  import TaskModal from "$lib/components/TaskModal.svelte";
  import Calendar from "$lib/components/Calendar.svelte";
  import ThemeSwitcher from "$lib/components/ThemeSwitcher.svelte";
  import {
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
    type Task,
  } from "$lib/api";

  let tasks = $state<Task[]>([]);
  let modalOpen = $state(false);
  let theme = $state(
    browser ? (localStorage.getItem("hearth-theme") ?? "stone") : "stone",
  );

  $effect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem("hearth-theme", theme);
  });

  $effect(() => {
    fetchTasks().then((t) => (tasks = t));
  });

  async function handleAdd(title: string, dueDate?: string, dueTime?: string) {
    const task = await createTask(title, dueDate, dueTime);
    tasks = [task, ...tasks];
  }

  async function handleToggle(task: Task) {
    const updated = await updateTask(task.id, !task.done);
    tasks = tasks.map((t) => (t.id === task.id ? updated : t));
  }

  async function handleDelete(id: number) {
    await deleteTask(id);
    tasks = tasks.filter((t) => t.id !== id);
  }
</script>

<svelte:head>
  <title>Hearth</title>
</svelte:head>

<main
  class="min-h-screen bg-[var(--bg)] text-[var(--text-1)] p-8 transition-colors duration-300"
>
  <div class="max-w-4xl mx-auto space-y-10">
    <header class="flex items-center justify-between">
      <h1
        class="text-xl font-light tracking-[0.3em] text-[var(--text-3)] uppercase"
      >
        Hearth
      </h1>
      <ThemeSwitcher {theme} onChange={(id) => (theme = id)} />
    </header>

    <section class="space-y-6">
      <div class="flex items-center justify-between">
        <h2 class="text-xs font-medium tracking-widest text-[var(--text-4)] uppercase">
          Tasks
        </h2>
        <button
          onclick={() => (modalOpen = true)}
          class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs
                 text-[var(--text-3)] hover:text-[var(--text-1)]
                 bg-[var(--surface)] hover:bg-[var(--surface-hi)]
                 transition-colors"
        >
          <svg class="w-3 h-3" viewBox="0 0 12 12" fill="none">
            <path d="M6 1v10M1 6h10" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          New Task
        </button>
      </div>

      <Calendar {tasks} onToggle={handleToggle} onDelete={handleDelete} />
    </section>
  </div>
</main>

<TaskModal bind:open={modalOpen} onAdd={handleAdd} />
