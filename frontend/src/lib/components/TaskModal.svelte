<script lang="ts">
    import type { Task } from '$lib/api';
    import TaskList from './TaskList.svelte';
    import { Trash2, X } from '@lucide/svelte';

    interface Props {
        open?: boolean;
        task?: Task | null;
        onAdd?: (
            title: string,
            dueDate?: string,
            dueTime?: string,
            description?: string,
            assignee?: string,
            recurrenceUnit?: string,
            recurrenceInterval?: number,
            recurrenceDays?: string,
            recurrenceEndDate?: string
        ) => void;
        onSave?: (
            title: string,
            dueDate?: string,
            dueTime?: string,
            description?: string,
            assignee?: string
        ) => void;
        onDelete?: (id: number, series?: boolean) => void;
        initialDate?: string;
    }

    let {
        open = $bindable(false),
        task = null,
        initialDate,
        onAdd,
        onSave,
        onDelete,
    }: Props = $props();

    const isEdit = $derived(!!task);

    let dialog = $state<HTMLDialogElement | null>(null);
    let confirmDelete = $state(false);

    $effect(() => {
        if (!dialog) return;
        if (open) {
            confirmDelete = false;
            dialog.showModal();
        } else if (dialog.open) {
            dialog.close();
        }
    });

    function close() {
        open = false;
        confirmDelete = false;
    }

    function handleBackdropClick(e: MouseEvent) {
        if (e.target === dialog) close();
    }

    function handleAdd(
        title: string,
        dueDate?: string,
        dueTime?: string,
        description?: string,
        assignee?: string,
        recurrenceUnit?: string,
        recurrenceInterval?: number,
        recurrenceDays?: string,
        recurrenceEndDate?: string
    ) {
        onAdd?.(
            title,
            dueDate,
            dueTime,
            description,
            assignee,
            recurrenceUnit,
            recurrenceInterval,
            recurrenceDays,
            recurrenceEndDate
        );
        close();
    }

    function handleSave(
        title: string,
        dueDate?: string,
        dueTime?: string,
        description?: string,
        assignee?: string
    ) {
        onSave?.(title, dueDate, dueTime, description, assignee);
        close();
    }

    function handleDelete(series?: boolean) {
        if (!task) return;
        onDelete?.(task.id, series);
        close();
    }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={close}
    onclick={handleBackdropClick}
    class="bg-transparent p-0 max-w-lg w-full backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    <div
        class="bg-[var(--bg)] border border-[var(--border)] rounded-xl shadow-xl p-6 space-y-5 mx-4"
    >
        <div class="flex items-center justify-between">
            <h2 class="text-[var(--text-1)]">{isEdit ? 'Edit Task' : 'Add a New Task'}</h2>
            <button
                onclick={close}
                class="text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors w-6 h-6 flex items-center justify-center"
                aria-label="Close"
            >
                <X />
            </button>
        </div>

        {#if isEdit}
            <TaskList task={task!} onSave={handleSave} />
        {:else}
            <TaskList onAdd={handleAdd} {initialDate} />
        {/if}

        {#if isEdit}
            <div class="pt-2 border-t border-[var(--border)]">
                {#if confirmDelete}
                    <div class="flex items-center gap-2">
                        <span class="text-xs text-[var(--text-3)] mr-1">Delete:</span>
                        <button
                            onclick={() => handleDelete(false)}
                            class="text-xs px-2.5 py-1 rounded bg-[var(--surface)] text-[var(--text-2)] hover:text-[var(--text-1)] transition"
                        >
                            Just this
                        </button>
                        {#if task?.series_id !== null}
                            <button
                                onclick={() => handleDelete(true)}
                                class="text-xs px-2.5 py-1 rounded bg-[var(--surface)] text-[var(--text-2)] hover:text-[var(--text-1)] transition"
                            >
                                All future
                            </button>
                        {/if}
                        <button
                            onclick={() => (confirmDelete = false)}
                            class="text-[var(--text-4)] hover:text-[var(--text-2)] transition ml-1"
                            aria-label="Cancel delete"
                        >
                            <X size={12} />
                        </button>
                    </div>
                {:else}
                    <button
                        onclick={() => {
                            if (task?.series_id !== null) {
                                confirmDelete = true;
                            } else {
                                handleDelete(false);
                            }
                        }}
                        class="text-[var(--text-3)] hover:text-[var(--text-1)] transition-colors"
                        aria-label="Delete task"
                    >
                        <Trash2 size={18} />
                    </button>
                {/if}
            </div>
        {/if}
    </div>
</dialog>
