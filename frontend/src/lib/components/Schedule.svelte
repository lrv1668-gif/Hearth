<script lang="ts">
  import type { Task } from '$lib/api';
  import { formatTime } from '$lib/utils';
  import { Check, X } from '@lucide/svelte';

  interface Props {
    tasks: Task[];
    onToggle: (task: Task) => void;
    onDelete: (id: number) => void;
  }

  let { tasks, onToggle, onDelete }: Props = $props();

  function dateKey(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  function formatGroupLabel(key: string): string {
    // Parse as local date by appending T00:00 to avoid UTC offset shifting the day
    const d = new Date(`${key}T00:00`);
    return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  }

  interface Group {
    key: string;
    label: string;
    labelClass: string;
    tasks: Task[];
  }

  const timeComparator = (a: Task, b: Task) => {
    if (!a.due_time && !b.due_time) return 0;
    if (!a.due_time) return 1;
    if (!b.due_time) return -1;
    return a.due_time.localeCompare(b.due_time);
  };

  let groups = $derived.by((): Group[] => {
    const today = new Date();
    const todayKey = dateKey(today);
    const tomorrow = new Date(today);
    tomorrow.setDate(today.getDate() + 1);
    const tomorrowKey = dateKey(tomorrow);

    const overdue: Task[] = [];
    const todayTasks: Task[] = [];
    const tomorrowTasks: Task[] = [];
    const futureMap: Map<string, Task[]> = new Map();
    const undated: Task[] = [];

    for (const t of tasks) {
      if (!t.due_date) {
        undated.push(t);
      } else {
        const key = dateKey(new Date(t.due_date));
        if (key < todayKey) {
          overdue.push(t);
        } else if (key === todayKey) {
          todayTasks.push(t);
        } else if (key === tomorrowKey) {
          tomorrowTasks.push(t);
        } else {
          const bucket = futureMap.get(key) ?? [];
          bucket.push(t);
          futureMap.set(key, bucket);
        }
      }
    }

    const result: Group[] = [];

    if (overdue.length > 0) {
      result.push({
        key: 'overdue',
        label: 'Overdue',
        labelClass: 'text-[var(--text-2)]',
        tasks: overdue.sort(timeComparator),
      });
    }
    if (todayTasks.length > 0) {
      result.push({
        key: 'today',
        label: 'Today',
        labelClass: 'text-[var(--text-3)]',
        tasks: todayTasks.sort(timeComparator),
      });
    }
    if (tomorrowTasks.length > 0) {
      result.push({
        key: 'tomorrow',
        label: 'Tomorrow',
        labelClass: 'text-[var(--text-3)]',
        tasks: tomorrowTasks.sort(timeComparator),
      });
    }

    const sortedFutureKeys = [...futureMap.keys()].sort();
    for (const key of sortedFutureKeys) {
      result.push({
        key,
        label: formatGroupLabel(key),
        labelClass: 'text-[var(--text-3)]',
        tasks: futureMap.get(key)!.sort(timeComparator),
      });
    }

    if (undated.length > 0) {
      result.push({
        key: 'undated',
        label: 'No due date',
        labelClass: 'text-[var(--text-3)]',
        tasks: undated,
      });
    }

    return result;
  });
</script>

{#if groups.length === 0}
  <p class="text-center text-sm text-[var(--text-4)] py-16">Nothing scheduled.</p>
{:else}
  <div class="space-y-8">
    {#each groups as group (group.key)}
      <div class="space-y-2">
        <div class="">
          <div class="flex items-center gap-3 pt-1">
            <h2 class="font-serif text-sm font-semibold italic whitespace-nowrap {group.labelClass}">
              {group.label}
            </h2>
            <div class="border-dotted border-t-2 border-[var(--text-2)] w-full"></div>
          </div>
        </div>
        <ul class="space-y-1.5">
          {#each group.tasks as task (task.id)}
            <li class="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-[var(--surface)] group/row">
              <button
                onclick={() => onToggle(task)}
                class="w-4 h-4 rounded border flex-shrink-0 flex items-center justify-center transition-colors
                       {task.done
                  ? 'bg-[var(--done-bg)] border-[var(--done-bg)]'
                  : 'border-[var(--text-3)] hover:border-[var(--text-1)]'}"
                aria-label="Toggle {task.title}"
              >
                {#if task.done}
                  <Check size="10" class="text-[var(--bg)]" />
                {/if}
              </button>

              {#if task.due_time}
                <span class="text-xs text-[var(--text-3)] w-14 flex-shrink-0 tabular-nums">
                  {formatTime(task.due_time)}
                </span>
              {:else}
                <span class="w-14 flex-shrink-0"></span>
              {/if}

              <span
                class="flex-1 text-sm transition-colors
                       {task.done ? 'line-through text-[var(--done)]' : 'text-[var(--text-1)]'}"
              >
                {task.title}
              </span>

              <button
                onclick={() => onDelete(task.id)}
                class="opacity-0 group-hover/row:opacity-100 text-[var(--text-3)] hover:text-[var(--text-1)] transition"
                aria-label="Delete {task.title}"
              >
                <X size="14" />
              </button>
            </li>
          {/each}
        </ul>
      </div>
    {/each}
  </div>
{/if}
