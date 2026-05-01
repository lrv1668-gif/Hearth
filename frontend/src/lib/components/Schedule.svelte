<script lang="ts">
    import type { Task } from '$lib/api';
    import { formatTime } from '$lib/utils';
    import { Check, RefreshCw, X } from '@lucide/svelte';

    interface Props {
        tasks: Task[];
        onToggle: (task: Task) => void;
        onDelete: (id: number, series?: boolean) => void;
        onEdit: (task: Task) => void;
    }

    let { tasks, onToggle, onDelete, onEdit }: Props = $props();

    let confirmDeleteId = $state<number | null>(null);

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
        const cutoff = new Date(today);
        cutoff.setDate(today.getDate() + 3);
        const cutoffKey = dateKey(cutoff);

        const todayTasks: Task[] = [];
        const tomorrowTasks: Task[] = [];
        const futureMap: Map<string, Task[]> = new Map();
        const undated: Task[] = [];

        for (const t of tasks) {
            if (!t.due_date) {
                undated.push(t);
            } else {
                const key = t.due_date.slice(0, 10);
                if (key < todayKey || key >= cutoffKey) {
                    // outside 3-day window — skip
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

        if (todayTasks.length > 0) {
            result.push({
                key: 'today',
                label: 'Today',
                tasks: todayTasks.sort(timeComparator),
            });
        }
        if (tomorrowTasks.length > 0) {
            result.push({
                key: 'tomorrow',
                label: 'Tomorrow',
                tasks: tomorrowTasks.sort(timeComparator),
            });
        }

        const sortedFutureKeys = [...futureMap.keys()].sort();
        for (const key of sortedFutureKeys) {
            result.push({
                key,
                label: formatGroupLabel(key),
                tasks: futureMap.get(key)!.sort(timeComparator),
            });
        }

        if (undated.length > 0) {
            result.push({
                key: 'undated',
                label: 'No due date',
                tasks: undated,
            });
        }

        return result;
    });

    function recurrenceLabel(task: Task): string {
        if (!task.recurrence_unit) return '';
        const n = task.recurrence_interval ?? 1;
        if (task.recurrence_unit === 'day') return n === 1 ? 'Daily' : `Every ${n}d`;
        if (task.recurrence_unit === 'week') {
            if (task.recurrence_days) return task.recurrence_days;
            return (task.recurrence_interval ?? 1) === 2 ? 'Bi-weekly' : 'Weekly';
        }
        if (task.recurrence_unit === 'month') return n === 1 ? 'Monthly' : `Every ${n}mo`;
        return '';
    }
</script>

{#if groups.length === 0}
    <p class="text-center text-sm text-[var(--text-4)] py-16">Nothing scheduled.</p>
{:else}
    <div class="space-y-8">
        {#each groups as group (group.key)}
            <div class="space-y-2">
                <div class="">
                    <div class="flex items-center gap-3 pt-1">
                        <h2
                            class="text-sm font-semibold whitespace-nowrap text-[var(--text-1)]"
                        >
                            {group.label}
                        </h2>
                    </div>
                </div>
                <ul class="space-y-1.5">
                    {#each group.tasks as task (task.id)}
                        <li
                            class="flex items-center gap-3 px-3 py-2.5 rounded-lg bg-[var(--surface)] group/row"
                        >
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
                                <span
                                    class="text-xs text-[var(--text-3)] w-14 flex-shrink-0 tabular-nums"
                                >
                                    {formatTime(task.due_time)}
                                </span>
                            {:else}
                                <span class="w-14 flex-shrink-0"></span>
                            {/if}

                            <button
                                onclick={() => onEdit(task)}
                                class="flex-1 min-w-0 text-left transition-colors hover:opacity-80"
                            >
                                <div class="flex items-center gap-2 min-w-0">
                                    <span
                                        class="text-sm transition-colors truncate
                                               {task.done ? 'line-through text-[var(--done)]' : 'text-[var(--text-1)]'}"
                                    >
                                        {task.title}
                                    </span>
                                    {#if task.assignee}
                                        <span
                                            class="flex-shrink-0 text-xs px-1.5 py-0.5 rounded bg-[var(--surface-hi)] text-[var(--text-3)]"
                                        >
                                            {task.assignee}
                                        </span>
                                    {/if}
                                    {#if task.recurrence_unit}
                                        <span
                                            class="flex-shrink-0 flex items-center gap-0.5 text-xs text-[var(--text-4)]"
                                        >
                                            <RefreshCw size={10} />
                                            {recurrenceLabel(task)}
                                        </span>
                                    {/if}
                                </div>
                                {#if task.description}
                                    <p class="text-xs text-[var(--text-3)] truncate mt-0.5">
                                        {task.description}
                                    </p>
                                {/if}
                            </button>

                            {#if confirmDeleteId === task.id}
                                <div class="flex items-center gap-1 flex-shrink-0">
                                    <button
                                        onclick={() => { onDelete(task.id); confirmDeleteId = null; }}
                                        class="text-xs px-2 py-0.5 rounded bg-[var(--surface-hi)] text-[var(--text-2)] hover:text-[var(--text-1)] transition"
                                    >
                                        Just this
                                    </button>
                                    <button
                                        onclick={() => { onDelete(task.id, true); confirmDeleteId = null; }}
                                        class="text-xs px-2 py-0.5 rounded bg-[var(--surface-hi)] text-[var(--text-2)] hover:text-[var(--text-1)] transition"
                                    >
                                        All future
                                    </button>
                                    <button
                                        onclick={() => (confirmDeleteId = null)}
                                        class="text-[var(--text-4)] hover:text-[var(--text-2)] transition"
                                        aria-label="Cancel"
                                    >
                                        <X size="12" />
                                    </button>
                                </div>
                            {:else}
                                <button
                                    onclick={() => {
                                        if (task.series_id !== null) {
                                            confirmDeleteId = task.id;
                                        } else {
                                            onDelete(task.id);
                                        }
                                    }}
                                    class="opacity-0 group-hover/row:opacity-100 text-[var(--text-3)] hover:text-[var(--text-1)] transition flex-shrink-0"
                                    aria-label="Delete {task.title}"
                                >
                                    <X size="14" />
                                </button>
                            {/if}
                        </li>
                    {/each}
                </ul>
            </div>
        {/each}
    </div>
{/if}
