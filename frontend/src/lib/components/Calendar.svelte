<script lang="ts">
  import type { Task } from "$lib/api";
  import { Check, ChevronLeft, ChevronRight, X } from "@lucide/svelte";

  interface Props {
    tasks: Task[];
    onToggle: (task: Task) => void;
    onDelete: (id: number) => void;
  }

  let { tasks, onToggle, onDelete }: Props = $props();

  const MONTH_NAMES = [
    "January",
    "February",
    "March",
    "April",
    "May",
    "June",
    "July",
    "August",
    "September",
    "October",
    "November",
    "December",
  ];
  const DAY_NAMES = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

  const today = new Date();

  function dateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
  }

  const todayKey = dateKey(today);

  let viewYear = $state(today.getFullYear());
  let viewMonth = $state(today.getMonth());

  let isCurrentMonth = $derived(
    viewYear === today.getFullYear() && viewMonth === today.getMonth(),
  );

  function prevMonth() {
    if (viewMonth === 0) {
      viewYear--;
      viewMonth = 11;
    } else viewMonth--;
  }

  function nextMonth() {
    if (viewMonth === 11) {
      viewYear++;
      viewMonth = 0;
    } else viewMonth++;
  }

  function goToToday() {
    viewYear = today.getFullYear();
    viewMonth = today.getMonth();
  }

  let undatedTasks = $derived(tasks.filter((t) => !t.due_date));

  let tasksByDate = $derived.by(() => {
    const map: Record<string, Task[]> = {};
    for (const t of tasks) {
      if (!t.due_date) continue;
      const key = dateKey(new Date(t.due_date));
      (map[key] ??= []).push(t);
    }
    for (const key in map) {
      map[key].sort((a, b) => {
        if (!a.due_time && !b.due_time) return 0;
        if (!a.due_time) return 1;
        if (!b.due_time) return -1;
        return a.due_time.localeCompare(b.due_time);
      });
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
    return `${viewYear}-${String(viewMonth + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
  }
</script>

<div class="space-y-6">
  <!-- Month navigation -->
  <div
    class="grid items-center gap-3"
    style="grid-template-columns: auto 1fr auto"
  >
    <!-- Left: prev + next arrows -->
    <div class="flex items-center gap-1">
      <button
        onclick={prevMonth}
        class="p-1.5 text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors"
        aria-label="Previous month"
      >
        <ChevronLeft size="16" />
      </button>
      <button
        onclick={nextMonth}
        class="p-1.5 text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors"
        aria-label="Next month"
      >
        <ChevronRight size="16" />
      </button>
    </div>

    <!-- Center: month + year -->
    <h2
      class="text-center text-xs font-medium tracking-widest text-[var(--text-3)] uppercase select-none"
    >
      {MONTH_NAMES[viewMonth]}
      {viewYear}
    </h2>

    <!-- Right: today -->
    <button
      onclick={goToToday}
      disabled={isCurrentMonth}
      class="text-xs tracking-wide transition-colors px-2 py-1 rounded
             {isCurrentMonth
        ? 'text-[var(--text-4)] cursor-default'
        : 'text-[var(--text-3)] hover:text-[var(--text-1)] hover:bg-[var(--surface)]'}"
    >
      Today
    </button>
  </div>

  <!-- Calendar grid — gap-px + bg-[var(--border)] creates hairline grid lines -->
  <div
    class="grid grid-cols-7 gap-px bg-[var(--border)] border border-[var(--border)] rounded-lg overflow-hidden"
  >
    <!-- Day-of-week headers -->
    {#each DAY_NAMES as day}
      <div
        class="bg-[var(--bg)] text-center text-xs text-[var(--text-4)] font-medium tracking-wider py-2"
      >
        {day}
      </div>
    {/each}

    <!-- Day cells -->
    {#each calendarCells as day, i (i)}
      {@const key = day ? cellKey(day) : ""}
      {@const isToday = key === todayKey}
      {@const dayTasks = day ? (tasksByDate[key] ?? []) : []}

      <div
        class="min-h-20 p-2
                  {day
          ? isToday
            ? 'bg-[var(--surface)]'
            : 'bg-[var(--bg)]'
          : 'bg-[var(--bg)] pointer-events-none'}"
      >
        {#if day}
          <span
            class="block text-xs leading-none mb-1.5
                       {isToday
              ? 'text-[var(--text-1)] font-semibold'
              : 'text-[var(--text-4)]'}"
          >
            {day}
          </span>

          {#if dayTasks.length > 0}
            <ul class="space-y-1">
              {#each dayTasks.slice(0, 3) as task (task.id)}
                <li class="flex items-center gap-1 min-w-0">
                  <button
                    onclick={() => onToggle(task)}
                    class="w-1.5 h-1.5 rounded-full flex-shrink-0 transition-colors
                           {task.done
                      ? 'bg-[var(--done-bg)]'
                      : 'bg-[var(--text-3)] hover:bg-[var(--text-1)]'}"
                    aria-label="Toggle {task.title}"
                  ></button>
                  <span class="text-xs truncate min-w-0
                               {task.done ? 'line-through text-[var(--done)]' : 'text-[var(--text-2)]'}">
                    {#if task.due_time}
                      <span class="text-[var(--text-4)] mr-0.5">{task.due_time}</span>
                    {/if}{task.title}
                  </span>
                </li>
              {/each}
              {#if dayTasks.length > 3}
                <li class="text-xs text-[var(--text-4)]">
                  +{dayTasks.length - 3} more
                </li>
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
      <h3
        class="text-xs font-medium tracking-widest text-[var(--text-4)] uppercase"
      >
        No due date
      </h3>
      <ul class="space-y-1.5">
        {#each undatedTasks as task (task.id)}
          <li
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-[var(--surface)] group"
          >
            <button
              onclick={() => onToggle(task)}
              class="w-4 h-4 rounded border flex-shrink-0 flex items-center justify-center transition-colors
                     {task.done
                ? 'bg-[var(--done-bg)] border-[var(--done-bg)]'
                : 'border-[var(--text-4)] hover:border-[var(--text-2)]'}"
              aria-label="Toggle {task.title}"
            >
              {#if task.done}
                <Check class="text-[var(--bg)]"/>
              {/if}
            </button>

            <span
              class="flex-1 text-sm transition-colors
                         {task.done
                ? 'line-through text-[var(--done)]'
                : 'text-[var(--text-1)]'}"
            >
              {task.title}
            </span>

            <button
              onclick={() => onDelete(task.id)}
              class="opacity-0 group-hover:opacity-100 text-[var(--text-4)] hover:text-[var(--text-2)]
                     text-lg leading-none transition"
              aria-label="Delete {task.title}"
            >
              <X />
            </button>
          </li>
        {/each}
      </ul>
    </div>
  {/if}
</div>
