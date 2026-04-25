<script lang="ts">
  import type { Task } from '$lib/api';

  interface Props {
    tasks: Task[];
    onToggle: (task: Task) => void;
    onDelete: (id: number) => void;
  }

  let { tasks, onToggle, onDelete }: Props = $props();

  const MONTH_NAMES = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December',
  ];
  const DAY_NAMES = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  const today = new Date();

  function dateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  const todayKey = dateKey(today);

  let viewYear = $state(today.getFullYear());
  let viewMonth = $state(today.getMonth());

  function prevMonth() {
    if (viewMonth === 0) { viewYear--; viewMonth = 11; }
    else viewMonth--;
  }

  function nextMonth() {
    if (viewMonth === 11) { viewYear++; viewMonth = 0; }
    else viewMonth++;
  }

  let undatedTasks = $derived(tasks.filter((t) => !t.due_date));

  let tasksByDate = $derived.by(() => {
    const map: Record<string, Task[]> = {};
    for (const t of tasks) {
      if (!t.due_date) continue;
      const key = dateKey(new Date(t.due_date));
      (map[key] ??= []).push(t);
    }
    return map;
  });

  let calendarCells = $derived.by(() => {
    const firstWeekday = new Date(viewYear, viewMonth, 1).getDay();
    const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
    const cells: (number | null)[] = Array(firstWeekday).fill(null);
    for (let d = 1; d <= daysInMonth; d++) cells.push(d);
    while (cells.length % 7 !== 0) cells.push(null);
    return cells;
  });

  function cellKey(day: number): string {
    return `${viewYear}-${String(viewMonth + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }
</script>

<div class="space-y-6">
  <!-- Month navigation -->
  <div class="flex items-center justify-between">
    <button onclick={prevMonth} class="p-1.5 text-stone-500 hover:text-stone-300 transition-colors">
      <svg class="w-4 h-4" viewBox="0 0 16 16" fill="none">
        <path d="M10 12L6 8l4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>
    <h2 class="text-xs font-medium tracking-widest text-stone-500 uppercase">
      {MONTH_NAMES[viewMonth]} {viewYear}
    </h2>
    <button onclick={nextMonth} class="p-1.5 text-stone-500 hover:text-stone-300 transition-colors">
      <svg class="w-4 h-4" viewBox="0 0 16 16" fill="none">
        <path d="M6 4l4 4-4 4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>
  </div>

  <!-- Calendar grid -->
  <div class="grid grid-cols-7 gap-px bg-stone-800 border border-stone-800 rounded-lg overflow-hidden">
    <!-- Day-of-week headers -->
    {#each DAY_NAMES as day}
      <div class="bg-stone-900 text-center text-xs text-stone-600 font-medium tracking-wider py-2">
        {day}
      </div>
    {/each}

    <!-- Day cells -->
    {#each calendarCells as day, i (i)}
      {@const key = day ? cellKey(day) : ''}
      {@const isToday = key === todayKey}
      {@const dayTasks = day ? (tasksByDate[key] ?? []) : []}
      <div
        class="min-h-20 p-2 {day ? (isToday ? 'bg-stone-800' : 'bg-stone-900') : 'bg-stone-900 pointer-events-none'}"
      >
        {#if day}
          <span class="block text-xs leading-none mb-1.5 {isToday ? 'text-stone-200 font-semibold' : 'text-stone-600'}">
            {day}
          </span>
          {#if dayTasks.length > 0}
            <ul class="space-y-1">
              {#each dayTasks.slice(0, 3) as task (task.id)}
                <li class="group flex items-center gap-1 min-w-0">
                  <button
                    onclick={() => onToggle(task)}
                    class="w-1.5 h-1.5 rounded-full flex-shrink-0 transition-colors
                           {task.done ? 'bg-stone-700' : 'bg-stone-500 hover:bg-stone-300'}"
                  ></button>
                  <span class="text-xs truncate {task.done ? 'line-through text-stone-700' : 'text-stone-400'}">
                    {task.title}
                  </span>
                </li>
              {/each}
              {#if dayTasks.length > 3}
                <li class="text-xs text-stone-700">+{dayTasks.length - 3} more</li>
              {/if}
            </ul>
          {/if}
        {/if}
      </div>
    {/each}
  </div>

  <!-- Undated tasks -->
  {#if undatedTasks.length > 0}
    <div class="space-y-3">
      <h3 class="text-xs font-medium tracking-widest text-stone-600 uppercase">No due date</h3>
      <ul class="space-y-1.5">
        {#each undatedTasks as task (task.id)}
          <li class="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-stone-800 group">
            <button
              onclick={() => onToggle(task)}
              class="w-4 h-4 rounded border flex-shrink-0 flex items-center justify-center transition-colors
                     {task.done ? 'bg-stone-600 border-stone-600' : 'border-stone-600 hover:border-stone-400'}"
            >
              {#if task.done}
                <svg class="w-2.5 h-2.5 text-stone-300" viewBox="0 0 10 10" fill="none">
                  <path d="M1.5 5l2.5 2.5 4.5-4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              {/if}
            </button>
            <span class="flex-1 text-sm transition-colors {task.done ? 'line-through text-stone-600' : 'text-stone-300'}">
              {task.title}
            </span>
            <button
              onclick={() => onDelete(task.id)}
              class="opacity-0 group-hover:opacity-100 text-stone-600 hover:text-stone-400 text-lg leading-none transition"
            >
              ×
            </button>
          </li>
        {/each}
      </ul>
    </div>
  {/if}
</div>
