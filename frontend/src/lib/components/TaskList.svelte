<script lang="ts">
  import {
    fetchTasks,
    createTask,
    updateTask,
    deleteTask,
    type Task,
  } from "$lib/api";

  let tasks = $state<Task[]>([]);
  let newTitle = $state("");
  let newDueDate = $state("");

  $effect(() => {
    fetchTasks().then((t) => (tasks = t));
  });

  async function handleAdd() {
    const title = newTitle.trim();
    if (!title) return;
    const task = await createTask(title, newDueDate || undefined);
    tasks = [task, ...tasks];
    newTitle = "";
    newDueDate = "";
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

<div class="space-y-3">
  <div class="flex gap-2">
    <input
      bind:value={newTitle}
      onkeydown={(e) => e.key === "Enter" && handleAdd()}
      placeholder="Add a task..."
      class="flex-1 bg-stone-800 text-stone-200 placeholder-stone-600 rounded-lg px-4 py-2.5 text-sm outline-none focus:ring-1 focus:ring-stone-600 transition"
    />
    <input
      type="date"
      bind:value={newDueDate}
      class="bg-stone-800 text-stone-400 rounded-lg px-3 py-2.5 text-sm outline-none focus:ring-1 focus:ring-stone-600 transition"
    />
    <button
      onclick={handleAdd}
      class="px-4 py-2.5 bg-stone-800 hover:bg-stone-700 text-stone-400 hover:text-stone-200 rounded-lg text-sm transition-colors"
    >
      Add
    </button>
  </div>

  {#if tasks.length === 0}
    <p class="text-stone-700 text-sm py-4 text-center">Nothing here yet.</p>
  {:else}
    <ul class="space-y-1.5">
      {#each tasks as task (task.id)}
        <li
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-stone-800 group"
        >
          <button
            onclick={() => handleToggle(task)}
            class="w-4 h-4 rounded border flex-shrink-0 flex items-center justify-center transition-colors
                               {task.done
              ? 'bg-stone-600 border-stone-600'
              : 'border-stone-600 hover:border-stone-400'}"
          >
            {#if task.done}
              <svg
                class="w-2.5 h-2.5 text-stone-300"
                viewBox="0 0 10 10"
                fill="none"
              >
                <path
                  d="M1.5 5l2.5 2.5 4.5-4.5"
                  stroke="currentColor"
                  stroke-width="1.5"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </svg>
            {/if}
          </button>

          <span
            class="flex-1 text-sm transition-colors
                                 {task.done
              ? 'line-through text-stone-600'
              : 'text-stone-300'}"
          >
            {task.title}
          </span>

          {#if task.due_date}
            <span class="text-xs text-stone-500 flex-shrink-0">
              {new Date(task.due_date).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric",
                year: "numeric",
              })}
            </span>
          {/if}

          <button
            onclick={() => handleDelete(task.id)}
            class="opacity-0 group-hover:opacity-100 text-stone-600 hover:text-stone-400 text-lg leading-none transition"
          >
            ×
          </button>
        </li>
      {/each}
    </ul>
  {/if}
</div>
