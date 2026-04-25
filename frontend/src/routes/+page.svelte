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
  import { HouseHeart } from '@lucide/svelte'

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
    
      <div class="flex flex-row gap-2 items-center text-[var(--text-2)]">
        <HouseHeart />
        <h1
          class="text-xl font-light tracking-[0.3em] text-[var(--text-2)] uppercase"
        >
          Hearth
        </h1>
      </div>
      <ThemeSwitcher {theme} onChange={(id) => (theme = id)} />
    </header>

    <section>
      <Calendar {tasks} onToggle={handleToggle} onDelete={handleDelete} onNewTask={() => (modalOpen = true)} />
    </section>
  </div>
</main>

<TaskModal bind:open={modalOpen} onAdd={handleAdd} />
