<script lang="ts">
  import { browser } from "$app/environment";
  import TaskList from "$lib/components/TaskList.svelte";
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

  async function handleAdd(title: string, dueDate?: string) {
    const task = await createTask(title, dueDate);
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

    <section>
      <TaskList onAdd={handleAdd} />
    </section>

    <section>
      <Calendar {tasks} onToggle={handleToggle} onDelete={handleDelete} />
    </section>
  </div>
</main>
