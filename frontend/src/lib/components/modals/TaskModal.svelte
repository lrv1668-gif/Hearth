<script lang="ts">
    import type { Task } from '$lib/api';
    import TaskAddEdit from '../TaskAddEdit.svelte';
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
            recurrenceEndDate?: string,
            isCountdown?: boolean
        ) => void;
        onSave?: (title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) => void;
        onDelete?: (id: number, series?: boolean) => void;
        initialDate?: string;
    }

    let { open = $bindable(false), task = null, initialDate, onAdd, onSave, onDelete }: Props = $props();

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
        recurrenceEndDate?: string,
        isCountdown?: boolean
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
            recurrenceEndDate,
            isCountdown
        );
        close();
    }

    function handleSave(title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) {
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
    class="w-full max-w-2xl bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    <div class="mx-4 space-y-5 rounded-xl border border-[var(--border)] bg-[var(--bg)] p-6 shadow-xl">
        <div class="flex items-center justify-between">
            <h2 class="text-[var(--text-1)]">{isEdit ? 'Edit Task' : 'Add a New Task'}</h2>
            <button
                onclick={close}
                class="flex h-6 w-6 items-center justify-center text-[var(--text-3)] transition-colors hover:text-[var(--text-1)]"
                aria-label="Close"
            >
                <X class="icon-md" />
            </button>
        </div>

        {#if isEdit}
            <TaskAddEdit task={task!} onSave={handleSave} />
        {:else}
            <TaskAddEdit onAdd={handleAdd} {initialDate} />
        {/if}

        {#if isEdit}
            <div class="border-t border-[var(--border)] pt-2">
                {#if confirmDelete}
                    <div class="flex items-center gap-2">
                        <span class="type-label mr-1 text-[var(--text-3)]">Delete:</span>
                        <button
                            onclick={() => handleDelete(false)}
                            class="type-label rounded bg-[var(--surface)] px-2.5 py-1 text-[var(--text-2)] transition hover:text-[var(--text-1)]"
                        >
                            Just this
                        </button>
                        {#if task?.series_id !== null}
                            <button
                                onclick={() => handleDelete(true)}
                                class="type-label rounded bg-[var(--surface)] px-2.5 py-1 text-[var(--text-2)] transition hover:text-[var(--text-1)]"
                            >
                                All future
                            </button>
                        {/if}
                        <button
                            onclick={() => (confirmDelete = false)}
                            class="ml-1 text-[var(--text-4)] transition hover:text-[var(--text-2)]"
                            aria-label="Cancel delete"
                        >
                            <X class="icon-sm" />
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
                        class="text-[var(--text-3)] transition-colors hover:text-[var(--text-1)]"
                        aria-label="Delete task"
                    >
                        <Trash2 class="icon-md" />
                    </button>
                {/if}
            </div>
        {/if}
    </div>
</dialog>
