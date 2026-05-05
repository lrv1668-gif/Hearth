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
    class="w-full max-w-sm bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    {#if dayKey}
        <div class="mx-4 space-y-4 rounded-xl border border-[var(--border)] bg-[var(--bg)] p-5 shadow-xl">
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
                {#each tasks as task (task.id)}
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
                                <span class="type-label mr-1 text-[var(--text-3)]">{formatTime(task.due_time)}</span>
                            {/if}
                            {task.title}
                        </button>
                    </li>
                {/each}
            </ul>
        </div>
    {/if}
</dialog>
