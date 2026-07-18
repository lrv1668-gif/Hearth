<script lang="ts">
    import type { Task, CalendarItem, Item } from '$lib/api';
    import { formatTime, eventDateKey, stripHtml } from '$lib/utils';
    import { Calendar as CalendarIcon, Check, ExternalLink, RefreshCw, X } from '@lucide/svelte';
    import ProviderIcon from '$lib/components/ProviderIcon.svelte';

    interface Props {
        tasks: Task[];
        calItems: CalendarItem[];
        onToggle: (task: Task) => void;
        onDelete: (id: number, series?: boolean) => void;
        onEdit: (task: Task) => void;
        onEventClick?: (event: CalendarItem) => void;
        onToggleCalendarTask?: (taskListId: string, taskId: string, completed: boolean) => void;
    }

    let { tasks, calItems, onToggle, onDelete, onEdit, onEventClick, onToggleCalendarTask }: Props = $props();

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
        items: Item[];
    }

    // Returns "HH:MM" in local time for sorting, or null for all-day / undated items (sort last).
    function timeKey(item: Item): string | null {
        if (item.kind === 'task') return item.data.due_time;
        if (item.data.is_all_day || !item.data.start) return null;
        const d = new Date(item.data.start);
        return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    }

    const itemComparator = (a: Item, b: Item): number => {
        const ta = timeKey(a);
        const tb = timeKey(b);
        if (!ta && !tb) return 0;
        if (!ta) return 1;
        if (!tb) return -1;
        return ta.localeCompare(tb);
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

        const todayItems: Item[] = [];
        const tomorrowItems: Item[] = [];
        const futureMap: Map<string, Item[]> = new Map();
        const undated: Item[] = [];

        for (const t of tasks) {
            const item: Item = { kind: 'task', data: t };
            if (!t.due_date) {
                undated.push(item);
            } else {
                const key = t.due_date.slice(0, 10);
                if (key < todayKey || key >= cutoffKey) {
                    // outside 3-day window — skip
                } else if (key === todayKey) {
                    todayItems.push(item);
                } else if (key === tomorrowKey) {
                    tomorrowItems.push(item);
                } else {
                    const bucket = futureMap.get(key) ?? [];
                    bucket.push(item);
                    futureMap.set(key, bucket);
                }
            }
        }

        for (const e of calItems) {
            if (!e.start) continue;
            const item: Item = { kind: 'event', data: e };
            const key = eventDateKey(e);
            if (key < todayKey || key >= cutoffKey) continue;
            if (key === todayKey) {
                todayItems.push(item);
            } else if (key === tomorrowKey) {
                tomorrowItems.push(item);
            } else {
                const bucket = futureMap.get(key) ?? [];
                bucket.push(item);
                futureMap.set(key, bucket);
            }
            // All-day calendar events always have a date string — never go into undated.
        }

        const result: Group[] = [];

        if (todayItems.length > 0) {
            result.push({ key: 'today', label: 'Today', items: todayItems.sort(itemComparator) });
        }
        if (tomorrowItems.length > 0) {
            result.push({ key: 'tomorrow', label: 'Tomorrow', items: tomorrowItems.sort(itemComparator) });
        }

        for (const key of [...futureMap.keys()].sort()) {
            result.push({ key, label: formatGroupLabel(key), items: futureMap.get(key)!.sort(itemComparator) });
        }

        if (undated.length > 0) {
            result.push({ key: 'undated', label: 'No due date', items: undated });
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
                        <h2 class="type-label whitespace-nowrap font-semibold uppercase tracking-wider text-[var(--text-2)]">
                            {group.label}
                        </h2>
                    </div>
                </div>
                <ul class="space-y-2">
                    {#each group.items as item (item.kind === 'task' ? `t-${item.data.id}` : `e-${item.data.id}`)}
                        {#if item.kind === 'task'}
                            {@const task = item.data}
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
                        {:else}
                            {@const calItem = item.data}
                            {#if calItem.kind === 'task'}
                                <li class="group/cal flex items-center gap-3 rounded-lg px-3 py-2.5">
                                    <div class="relative flex-shrink-0">
                                        <button
                                            onclick={() => {
                                                if (calItem.task_list_id)
                                                    onToggleCalendarTask?.(calItem.task_list_id, calItem.id, !calItem.is_completed);
                                            }}
                                            class="flex h-4 w-4 items-center justify-center rounded border transition-colors
                                                   {calItem.is_completed
                                                ? 'border-[var(--done-bg)] bg-[var(--done-bg)]'
                                                : 'border-[var(--text-3)] hover:border-[var(--text-1)]'}"
                                            aria-label="Toggle {calItem.title}"
                                        >
                                            {#if calItem.is_completed}
                                                <Check class="icon-xs text-[var(--bg)]" />
                                            {/if}
                                        </button>
                                        <div class="absolute -bottom-1 -right-1">
                                            <ProviderIcon provider={calItem.provider} />
                                        </div>
                                    </div>
                                    <span class="w-14 flex-shrink-0"></span>
                                    <div class="min-w-0 flex-1">
                                        <span
                                            class="type-body block truncate
                                                   {calItem.is_completed ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                                        >
                                            {calItem.title}
                                        </span>
                                        {#if calItem.description}
                                            <p class="type-label mt-0.5 truncate text-[var(--text-2)]">{calItem.description}</p>
                                        {/if}
                                    </div>
                                    {#if calItem.html_link}
                                        <a
                                            href={calItem.html_link}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            class="flex-shrink-0 text-[var(--text-3)] opacity-0 transition hover:text-[var(--accent)] group-hover/cal:opacity-100"
                                            aria-label="Open in Google Tasks"
                                            onclick={(e) => e.stopPropagation()}
                                        >
                                            <ExternalLink class="icon-sm" />
                                        </a>
                                    {/if}
                                </li>
                            {:else}
                                <li class="flex items-center gap-3 rounded-lg px-3 py-2.5">
                                    <div class="relative flex-shrink-0">
                                        <CalendarIcon class="h-4 w-4 text-[var(--accent)]" aria-hidden="true" />
                                        <div class="absolute -bottom-1 -right-1">
                                            <ProviderIcon provider={calItem.provider} />
                                        </div>
                                    </div>
                                    {#if !calItem.is_all_day}
                                        <span class="type-label w-14 flex-shrink-0 tabular-nums text-[var(--text-2)]">
                                            {formatTime(timeKey(item) ?? '')}
                                        </span>
                                    {:else}
                                        <span class="w-14 flex-shrink-0"></span>
                                    {/if}
                                    <button
                                        onclick={() => onEventClick?.(calItem)}
                                        class="min-w-0 flex-1 text-left transition-opacity hover:opacity-70"
                                    >
                                        <span class="type-body block truncate text-[var(--text-1)]">{calItem.title}</span>
                                        {#if calItem.description}
                                            <p class="type-label mt-0.5 truncate text-[var(--text-2)]">{stripHtml(calItem.description)}</p>
                                        {/if}
                                    </button>
                                </li>
                            {/if}
                        {/if}
                    {/each}
                </ul>
            </div>
        {/each}
    </div>
{/if}
