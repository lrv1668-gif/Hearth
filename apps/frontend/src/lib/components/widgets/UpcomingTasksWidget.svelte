<script lang="ts">
    import type { Task, CalendarItem, Item } from '#lib/api.ts';
    import { formatTime, eventDateKey, stripHtml, localDateKey as dateKey } from '#lib/utils.ts';
    import { Calendar as CalendarIcon, ExternalLink, RefreshCw, X } from '@lucide/svelte';
    import ProviderIcon from '#lib/components/ProviderIcon.svelte';
    import TaskCheckbox from '#lib/components/TaskCheckbox.svelte';
    import Button from '#lib/components/ui/Button.svelte';
    import IconButton from '#lib/components/ui/IconButton.svelte';

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
    <p class="type-body py-4 text-center text-(--text-2)">Nothing scheduled.</p>
{:else}
    <div class="space-y-4">
        {#each groups as group (group.key)}
            <div class="space-y-1">
                <h2 class="eyebrow whitespace-nowrap {group.key === 'today' ? 'text-(--text-1)' : ''}">
                    {group.label}
                </h2>
                <ul class="space-y-2">
                    {#each group.items as item (item.kind === 'task' ? `t-${item.data.id}` : `e-${item.data.id}`)}
                        {#if item.kind === 'task'}
                            {@const task = item.data}
                            <li class="group flex items-center gap-2 rounded-lg py-2 sm:gap-3 sm:px-3 sm:py-2.5">
                                <TaskCheckbox checked={task.done} title={task.title} ontoggle={() => onToggle(task)} />

                                {#if task.due_time}
                                    <span
                                        class="type-label hidden w-14 flex-shrink-0 text-(--text-2) tabular-nums sm:block"
                                    >
                                        {formatTime(task.due_time)}
                                    </span>
                                {:else}
                                    <span class="hidden w-14 flex-shrink-0 sm:block"></span>
                                {/if}

                                <button
                                    onclick={() => onEdit(task)}
                                    class="min-w-0 flex-1 text-left transition-colors hover:opacity-80"
                                >
                                    <!-- Phones: the time sits above the title rather than taking a column from it -->
                                    {#if task.due_time}
                                        <span class="type-label block text-(--text-2) tabular-nums sm:hidden">
                                            {formatTime(task.due_time)}
                                        </span>
                                    {/if}
                                    <div class="flex min-w-0 items-center gap-2">
                                        <span
                                            class="type-body transition-colors max-sm:line-clamp-2 sm:truncate
                                                   {task.done ? 'text-(--done) line-through' : 'text-(--text-1)'}"
                                        >
                                            {task.title}
                                        </span>
                                        {#if task.assignee}
                                            <span
                                                class="type-label flex-shrink-0 rounded bg-(--surface-hi) px-1.5 py-0.5 text-(--text-2)"
                                            >
                                                {task.assignee}
                                            </span>
                                        {/if}
                                        {#if task.recurrence_unit}
                                            <!-- Icon-only on phones, where the title needs the room -->
                                            <span
                                                class="type-label flex flex-shrink-0 items-center gap-0.5 text-(--text-2)"
                                                title={recurrenceLabel(task)}
                                            >
                                                <RefreshCw class="icon-xs" aria-label={recurrenceLabel(task)} />
                                                <span class="hidden sm:inline">{recurrenceLabel(task)}</span>
                                            </span>
                                        {/if}
                                    </div>
                                    {#if task.description}
                                        <p class="type-label mt-0.5 truncate text-(--text-2)">
                                            {task.description}
                                        </p>
                                    {/if}
                                </button>

                                {#if confirmDeleteId === task.id}
                                    <div class="flex flex-shrink-0 items-center gap-1">
                                        <Button
                                            size="sm"
                                            onclick={() => {
                                                onDelete(task.id);
                                                confirmDeleteId = null;
                                            }}
                                        >
                                            Just this
                                        </Button>
                                        <Button
                                            size="sm"
                                            onclick={() => {
                                                onDelete(task.id, true);
                                                confirmDeleteId = null;
                                            }}
                                        >
                                            All future
                                        </Button>
                                        <IconButton
                                            label="Cancel delete"
                                            size="sm"
                                            onclick={() => (confirmDeleteId = null)}
                                        >
                                            <X class="icon-sm" />
                                        </IconButton>
                                    </div>
                                {:else}
                                    <IconButton
                                        label="Delete {task.title}"
                                        size="sm"
                                        reveal
                                        class="-my-2"
                                        onclick={() => {
                                            if (task.series_id !== null) {
                                                confirmDeleteId = task.id;
                                            } else {
                                                onDelete(task.id);
                                            }
                                        }}
                                    >
                                        <X class="icon-md" />
                                    </IconButton>
                                {/if}
                            </li>
                        {:else}
                            {@const calItem = item.data}
                            {#if calItem.kind === 'task'}
                                <li class="group flex items-center gap-2 rounded-lg py-2 sm:gap-3 sm:px-3 sm:py-2.5">
                                    <div class="relative flex-shrink-0">
                                        <TaskCheckbox
                                            checked={calItem.is_completed ?? false}
                                            title={calItem.title}
                                            ontoggle={() => {
                                                if (calItem.task_list_id)
                                                    onToggleCalendarTask?.(
                                                        calItem.task_list_id,
                                                        calItem.id,
                                                        !calItem.is_completed
                                                    );
                                            }}
                                        />
                                        <div class="absolute -right-1 -bottom-1">
                                            <ProviderIcon provider={calItem.provider} />
                                        </div>
                                    </div>
                                    <span class="hidden w-14 flex-shrink-0 sm:block"></span>
                                    <div class="min-w-0 flex-1">
                                        <span
                                            class="type-body max-sm:line-clamp-2 sm:block sm:truncate
                                                   {calItem.is_completed
                                                ? 'text-(--done) line-through'
                                                : 'text-(--text-1)'}"
                                        >
                                            {calItem.title}
                                        </span>
                                        {#if calItem.description}
                                            <p class="type-label mt-0.5 truncate text-(--text-2)">
                                                {calItem.description}
                                            </p>
                                        {/if}
                                    </div>
                                    {#if calItem.html_link}
                                        <a
                                            href={calItem.html_link}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            class="flex-shrink-0 text-(--text-3) opacity-0 transition group-hover:opacity-100 hover:text-(--accent) focus-visible:opacity-100 pointer-coarse:opacity-100"
                                            aria-label="Open in Google Tasks"
                                            onclick={(e) => e.stopPropagation()}
                                        >
                                            <ExternalLink class="icon-sm" />
                                        </a>
                                    {/if}
                                </li>
                            {:else}
                                <li class="flex items-center gap-2 rounded-lg py-2 sm:gap-3 sm:px-3 sm:py-2.5">
                                    <div class="relative flex-shrink-0">
                                        <CalendarIcon class="h-4 w-4 text-(--accent)" aria-hidden="true" />
                                        <div class="absolute -right-1 -bottom-1">
                                            <ProviderIcon provider={calItem.provider} />
                                        </div>
                                    </div>
                                    {#if !calItem.is_all_day}
                                        <span
                                            class="type-label hidden w-14 flex-shrink-0 text-(--text-2) tabular-nums sm:block"
                                        >
                                            {formatTime(timeKey(item) ?? '')}
                                        </span>
                                    {:else}
                                        <span class="hidden w-14 flex-shrink-0 sm:block"></span>
                                    {/if}
                                    <button
                                        onclick={() => onEventClick?.(calItem)}
                                        class="min-w-0 flex-1 text-left transition-opacity hover:opacity-70"
                                    >
                                        {#if !calItem.is_all_day}
                                            <span class="type-label block text-(--text-2) tabular-nums sm:hidden">
                                                {formatTime(timeKey(item) ?? '')}
                                            </span>
                                        {/if}
                                        <span class="type-body text-(--text-1) max-sm:line-clamp-2 sm:block sm:truncate"
                                            >{calItem.title}</span
                                        >
                                        {#if calItem.description}
                                            <p class="type-label mt-0.5 truncate text-(--text-2)">
                                                {stripHtml(calItem.description)}
                                            </p>
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
