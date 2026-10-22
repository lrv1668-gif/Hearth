<script lang="ts">
    import type { Task, CalendarItem, Item } from '$lib/api';
    import { formatTime, providerLabel } from '$lib/utils';
    import { Calendar as CalendarIcon } from '@lucide/svelte';

    interface Props {
        items: Item[];
        onToggle: (task: Task) => void;
        onEdit: (task: Task) => void;
        onEventClick?: (event: CalendarItem) => void;
        onToggleCalendarTask?: (taskListId: string, taskId: string, completed: boolean) => void;
        /** Called after a row opens something else (an edit or event modal) — lets a dialog close itself. */
        onOpen?: () => void;
    }

    let { items, onToggle, onEdit, onEventClick, onToggleCalendarTask, onOpen }: Props = $props();

    // The visible dot is 8px; the ::after widens the tap target to 24px without moving the layout.
    const dot =
        "relative h-2 w-2 flex-shrink-0 rounded-full transition-colors after:absolute after:-inset-2 after:content-['']";
</script>

<!--
@component
One day's tasks and calendar items as rows — shared by the day overflow modal and the mobile calendar's day agenda.
-->
<ul class="space-y-2">
    {#each items as item (item.kind === 'task' ? `t-${item.data.id}` : `e-${item.data.id}`)}
        {#if item.kind === 'task'}
            {@const task = item.data}
            <li class="flex min-w-0 items-center gap-2">
                <button
                    onclick={() => onToggle(task)}
                    class="{dot} {task.done ? 'bg-(--done-bg)' : 'bg-(--text-3) hover:bg-(--text-1)'}"
                    aria-label="Toggle {task.title}"
                ></button>
                <button
                    onclick={() => {
                        onEdit(task);
                        onOpen?.();
                    }}
                    class="type-body min-w-0 flex-1 truncate text-left transition-colors hover:underline
                       {task.done ? 'text-(--done) line-through' : 'text-(--text-1)'}"
                >
                    {#if task.due_time}
                        <span class="type-label mr-1 text-(--text-2)">{formatTime(task.due_time)}</span>
                    {/if}
                    {task.title}
                </button>
            </li>
        {:else}
            {@const calItem = item.data}
            {#if calItem.kind === 'task'}
                <li class="flex min-w-0 items-center gap-2">
                    <button
                        onclick={() => {
                            if (calItem.task_list_id)
                                onToggleCalendarTask?.(calItem.task_list_id, calItem.id, !calItem.is_completed);
                        }}
                        class="{dot} {calItem.is_completed ? 'bg-(--done-bg)' : 'bg-(--accent) hover:opacity-80'}"
                        aria-label="Toggle {calItem.title}"
                    ></button>
                    <span
                        class="type-body min-w-0 flex-1 truncate
                               {calItem.is_completed ? 'text-(--done) line-through' : 'text-(--text-1)'}"
                    >
                        {calItem.title}
                    </span>
                    <span class="type-label flex-shrink-0 text-(--text-2)">{providerLabel(calItem.provider)}</span>
                </li>
            {:else}
                <li class="flex min-w-0 items-center gap-2">
                    <CalendarIcon class="icon-xs flex-shrink-0 text-(--accent)" aria-hidden="true" />
                    <button
                        onclick={() => {
                            onOpen?.();
                            onEventClick?.(calItem);
                        }}
                        class="min-w-0 flex-1 text-left transition-opacity hover:opacity-70"
                    >
                        <span class="type-body block truncate text-(--text-1)">
                            {#if !calItem.is_all_day && calItem.start}
                                <span class="type-label mr-1 text-(--text-2)">
                                    {formatTime(calItem.start.slice(11, 16))}
                                </span>
                            {/if}
                            {calItem.title}
                        </span>
                        <span class="type-label text-(--text-2)">{providerLabel(calItem.provider)}</span>
                    </button>
                </li>
            {/if}
        {/if}
    {/each}
</ul>
