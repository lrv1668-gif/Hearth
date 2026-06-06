<script lang="ts">
    import type { Task, CalendarItem, Item } from '$lib/api';
    import { formatTime, providerLabel } from '$lib/utils';
    import { Calendar as CalendarIcon, X } from '@lucide/svelte';

    interface Props {
        dayKey?: string | null;
        items: Item[];
        onToggle: (task: Task) => void;
        onEdit: (task: Task) => void;
        onEventClick?: (event: CalendarItem) => void;
        onToggleCalendarTask?: (taskListId: string, taskId: string, completed: boolean) => void;
    }

    let { dayKey = $bindable(null), items, onToggle, onEdit, onEventClick, onToggleCalendarTask }: Props = $props();

    let dialog = $state<HTMLDialogElement | null>(null);

    $effect(() => {
        if (!dialog) return;
        if (dayKey) dialog.showModal();
        else if (dialog.open) dialog.close();
    });

    function close() {
        dayKey = null;
    }

    function dateLabel(key: string): string {
        const [y, m, d] = key.split('-').map(Number);
        return new Date(y, m - 1, d).toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
        });
    }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={close}
    onclick={(e) => e.target === dialog && close()}
    class="w-full max-w-sm bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    {#if dayKey}
        <div class="mx-4 space-y-4 rounded-2xl border border-[var(--border)] bg-[var(--bg)] p-5 shadow-xl">
            <div class="flex items-center justify-between">
                <h2 class="type-body font-semibold text-[var(--text-1)]">{dateLabel(dayKey)}</h2>
                <button
                    onclick={close}
                    class="flex h-6 w-6 items-center justify-center text-[var(--text-3)] transition-colors hover:text-[var(--text-1)]"
                    aria-label="Close"
                >
                    <X class="icon-sm" />
                </button>
            </div>
            <ul class="space-y-2">
                {#each items as item (item.kind === 'task' ? `t-${item.data.id}` : `e-${item.data.id}`)}
                    {#if item.kind === 'task'}
                        {@const task = item.data}
                        <li class="flex min-w-0 items-center gap-2">
                            <button
                                onclick={() => onToggle(task)}
                                class="h-2 w-2 flex-shrink-0 rounded-full transition-colors
                                   {task.done ? 'bg-[var(--done-bg)]' : 'bg-[var(--text-3)] hover:bg-[var(--text-1)]'}"
                                aria-label="Toggle {task.title}"
                            ></button>
                            <button
                                onclick={() => {
                                    onEdit(task);
                                    close();
                                }}
                                class="type-body min-w-0 flex-1 truncate text-left transition-colors hover:underline
                                   {task.done ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                            >
                                {#if task.due_time}
                                    <span class="type-label mr-1 text-[var(--text-3)]"
                                        >{formatTime(task.due_time)}</span
                                    >
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
                                    class="h-2 w-2 flex-shrink-0 rounded-full transition-colors
                                           {calItem.is_completed ? 'bg-[var(--done-bg)]' : 'bg-[var(--accent)] hover:opacity-80'}"
                                    aria-label="Toggle {calItem.title}"
                                ></button>
                                <span
                                    class="type-body min-w-0 flex-1 truncate
                                           {calItem.is_completed ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                                >
                                    {calItem.title}
                                </span>
                                <span class="type-label flex-shrink-0 text-[var(--text-3)]">{providerLabel(calItem.provider)}</span>
                            </li>
                        {:else}
                            <li class="flex min-w-0 items-center gap-2">
                                <CalendarIcon class="h-2 w-2 flex-shrink-0 text-[var(--accent)]" aria-hidden="true" />
                                <button
                                    onclick={() => { close(); onEventClick?.(calItem); }}
                                    class="min-w-0 flex-1 text-left transition-opacity hover:opacity-70"
                                >
                                    <span class="type-body block truncate text-[var(--text-1)]">
                                        {#if !calItem.is_all_day && calItem.start}
                                            <span class="type-label mr-1 text-[var(--text-3)]">
                                                {formatTime(calItem.start.slice(11, 16))}
                                            </span>
                                        {/if}
                                        {calItem.title}
                                    </span>
                                    <span class="type-label text-[var(--text-3)]">{providerLabel(calItem.provider)}</span>
                                </button>
                            </li>
                        {/if}
                    {/if}
                {/each}
            </ul>
        </div>
    {/if}
</dialog>
