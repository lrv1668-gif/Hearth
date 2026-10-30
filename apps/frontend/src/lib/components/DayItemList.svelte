<script lang="ts">
    import type { Task, CalendarItem, Item } from '#lib/api.ts';
    import { formatTime, providerLabel } from '#lib/utils.ts';
    import { Calendar as CalendarIcon } from '@lucide/svelte';
    import TaskCheckbox from './TaskCheckbox.svelte';

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
                <TaskCheckbox variant="dot" checked={task.done} title={task.title} ontoggle={() => onToggle(task)} />
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
                    <TaskCheckbox
                        variant="dot"
                        external
                        checked={calItem.is_completed ?? false}
                        title={calItem.title}
                        ontoggle={() => {
                            if (calItem.task_list_id)
                                onToggleCalendarTask?.(calItem.task_list_id, calItem.id, !calItem.is_completed);
                        }}
                    />
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
