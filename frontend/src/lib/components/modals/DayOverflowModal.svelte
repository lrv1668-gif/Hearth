<script lang="ts">
    import type { Task } from '$lib/api';
    import { formatTime } from '$lib/utils';
    import { X } from '@lucide/svelte';

    interface Props {
        dayKey?: string | null;
        tasks: Task[];
        onToggle: (task: Task) => void;
        onEdit: (task: Task) => void;
    }

    let { dayKey = $bindable(null), tasks, onToggle, onEdit }: Props = $props();

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
    class="bg-transparent p-0 max-w-sm w-full backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    {#if dayKey}
        <div class="bg-[var(--bg)] border border-[var(--border)] rounded-xl shadow-xl p-5 mx-4 space-y-4">
            <div class="flex items-center justify-between">
                <h2 class="type-body font-semibold text-[var(--text-1)]">{dateLabel(dayKey)}</h2>
                <button
                    onclick={close}
                    class="text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors w-6 h-6 flex items-center justify-center"
                    aria-label="Close"
                >
                    <X size={12} class="icon-sm" />
                </button>
            </div>
            <ul class="space-y-2">
                {#each tasks as task (task.id)}
                    <li class="flex items-center gap-2 min-w-0">
                        <button
                            onclick={() => onToggle(task)}
                            class="w-2 h-2 rounded-full flex-shrink-0 transition-colors
                               {task.done ? 'bg-[var(--done-bg)]' : 'bg-[var(--text-3)] hover:bg-[var(--text-1)]'}"
                            aria-label="Toggle {task.title}"
                        ></button>
                        <button
                            onclick={() => {
                                onEdit(task);
                                close();
                            }}
                            class="type-body text-left transition-colors hover:underline min-w-0 flex-1 truncate
                               {task.done ? 'line-through text-[var(--done)]' : 'text-[var(--text-1)]'}"
                        >
                            {#if task.due_time}
                                <span class="type-label text-[var(--text-3)] mr-1">{formatTime(task.due_time)}</span>
                            {/if}
                            {task.title}
                        </button>
                    </li>
                {/each}
            </ul>
        </div>
    {/if}
</dialog>
