<script lang="ts">
    import type { Task, CalendarItem, Item } from '$lib/api';
    import { X } from '@lucide/svelte';
    import DayItemList from '../DayItemList.svelte';

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
        <div class="mx-4 space-y-4 rounded-2xl border border-(--border) bg-(--bg) p-5 shadow-xl">
            <div class="flex items-center justify-between">
                <h2 class="type-body font-semibold text-(--text-1)">{dateLabel(dayKey)}</h2>
                <button
                    onclick={close}
                    class="flex h-6 w-6 items-center justify-center text-(--text-2) transition-colors hover:text-(--text-1)"
                    aria-label="Close"
                >
                    <X class="icon-sm" />
                </button>
            </div>
            <DayItemList {items} {onToggle} {onEdit} {onEventClick} {onToggleCalendarTask} onOpen={close} />
        </div>
    {/if}
</dialog>
