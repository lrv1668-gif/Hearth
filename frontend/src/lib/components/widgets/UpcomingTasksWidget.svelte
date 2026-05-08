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
    <p class="type-body py-4 text-center text-[var(--text-2)]">Nothing scheduled.</p>
{:else}
    <div class="space-y-4">
        {#each groups as group (group.key)}
            <div class="space-y-1">
                <div class="">
                    <div class="flex items-center gap-3">
                        <h2 class="type-body whitespace-nowrap font-semibold text-[var(--text-1)]">
                            {group.label}
                        </h2>
                    </div>
                </div>
                <ul class="space-y-2">
                    {#each group.tasks as task (task.id)}
                        <li class="group/row flex items-center gap-3 rounded-lg px-3 py-2.5">
                            <button
                                onclick={() => onToggle(task)}
                                class="flex h-4 w-4 flex-shrink-0 items-center justify-center rounded border transition-colors
                       {task.done
                                    ? 'border-[var(--done-bg)] bg-[var(--done-bg)]'
                                    : 'border-[var(--text-3)] hover:border-[var(--text-1)]'}"
                                aria-label="Toggle {task.title}"
                            >
                                {#if task.done}
                                    <Check class="icon-xs text-[var(--bg)]" />
                                {/if}
                            </button>

                            {#if task.due_time}
                                <span class="type-label w-14 flex-shrink-0 tabular-nums text-[var(--text-2)]">
                                    {formatTime(task.due_time)}
                                </span>
                            {:else}
                                <span class="w-14 flex-shrink-0"></span>
                            {/if}

                            <button
                                onclick={() => onEdit(task)}
                                class="min-w-0 flex-1 text-left transition-colors hover:opacity-80"
                            >
                                <div class="flex min-w-0 items-center gap-2">
                                    <span
                                        class="type-body truncate transition-colors
                                               {task.done ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                                    >
                                        {task.title}
                                    </span>
                                    {#if task.assignee}
                                        <span
                                            class="type-label flex-shrink-0 rounded bg-[var(--surface-hi)] px-1.5 py-0.5 text-[var(--text-2)]"
                                        >
                                            {task.assignee}
                                        </span>
                                    {/if}
                                    {#if task.recurrence_unit}
                                        <span
                                            class="type-label flex flex-shrink-0 items-center gap-0.5 text-[var(--text-2)]"
                                        >
                                            <RefreshCw class="icon-xs" />
                                            {recurrenceLabel(task)}
                                        </span>
                                    {/if}
                                </div>
                                {#if task.description}
                                    <p class="type-label mt-0.5 truncate text-[var(--text-2)]">
                                        {task.description}
                                    </p>
                                {/if}
                            </button>

                            {#if confirmDeleteId === task.id}
                                <div class="flex flex-shrink-0 items-center gap-1">
                                    <button
                                        onclick={() => {
                                            onDelete(task.id);
                                            confirmDeleteId = null;
                                        }}
                                        class="type-label rounded bg-[var(--surface-hi)] px-2 py-0.5 text-[var(--text-2)] transition hover:text-[var(--text-1)]"
                                    >
                                        Just this
                                    </button>
                                    <button
                                        onclick={() => {
                                            onDelete(task.id, true);
                                            confirmDeleteId = null;
                                        }}
                                        class="type-label rounded bg-[var(--surface-hi)] px-2 py-0.5 text-[var(--text-2)] transition hover:text-[var(--text-1)]"
                                    >
                                        All future
                                    </button>
                                    <button
                                        onclick={() => (confirmDeleteId = null)}
                                        class="text-[var(--text-4)] transition hover:text-[var(--text-2)]"
                                        aria-label="Cancel"
                                    >
                                        <X class="icon-sm" />
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
                                    class="flex-shrink-0 text-[var(--text-3)] opacity-0 transition hover:text-[var(--text-1)] group-hover/row:opacity-100"
                                    aria-label="Delete {task.title}"
                                >
                                    <X class="icon-md" />
                                </button>
                            {/if}
                        </li>
                    {/each}
                </ul>
            </div>
        {/each}
    </div>
{/if}
