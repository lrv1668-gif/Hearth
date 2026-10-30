<script lang="ts">
    import type { Task } from '#lib/api.ts';
    import TaskAddEdit from '../TaskAddEdit.svelte';
    import Button from '../ui/Button.svelte';
    import IconButton from '../ui/IconButton.svelte';
    import Modal from '../ui/Modal.svelte';
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

    let confirmDelete = $state(false);

    $effect(() => {
        if (open) confirmDelete = false;
    });

    function close() {
        open = false;
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

<Modal bind:open title={isEdit ? 'Edit task' : 'Add a new task'} size="lg">
    <div class="space-y-5">
        {#if isEdit}
            <TaskAddEdit task={task!} onSave={handleSave} />
        {:else}
            <TaskAddEdit onAdd={handleAdd} {initialDate} />
        {/if}

        {#if isEdit}
            <div class="border-t border-(--border) pt-2">
                {#if confirmDelete}
                    <div class="flex flex-wrap items-center gap-2">
                        <span class="type-label mr-1 text-(--text-2)">Delete:</span>
                        <Button size="sm" onclick={() => handleDelete(false)}>Just this</Button>
                        {#if task?.series_id !== null}
                            <Button size="sm" onclick={() => handleDelete(true)}>All future</Button>
                        {/if}
                        <IconButton label="Cancel delete" size="sm" onclick={() => (confirmDelete = false)}>
                            <X class="icon-sm" />
                        </IconButton>
                    </div>
                {:else}
                    <IconButton
                        label="Delete task"
                        class="-ml-2"
                        onclick={() => {
                            if (task?.series_id !== null) {
                                confirmDelete = true;
                            } else {
                                handleDelete(false);
                            }
                        }}
                    >
                        <Trash2 class="icon-md" />
                    </IconButton>
                {/if}
            </div>
        {/if}
    </div>
</Modal>
