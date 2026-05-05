<script lang="ts">
    import type { Task } from '$lib/api';
    import { taskStore, addTask, toggleTask, removeTask, editTask } from '$lib/stores/TaskStore.svelte.ts';
    import Calendar from '$lib/components/Calendar.svelte';
    import TaskModal from '$lib/components/modals/TaskModal.svelte';

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);
    let initialDate = $state<string | undefined>(undefined);

    function openNewTask() {
        editingTask = null;
        initialDate = undefined;
        modalOpen = true;
    }

    function openEditTask(task: Task) {
        editingTask = task;
        initialDate = undefined;
        modalOpen = true;
    }

    function openDateTask(date: string) {
        editingTask = null;
        initialDate = date;
        modalOpen = true;
    }

    function handleSave(title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) {
        if (editingTask) {
            editTask(editingTask, title, dueDate, dueTime, description, assignee);
            editingTask = null;
        }
    }

    function handleModalClose() {
        editingTask = null;
    }
</script>

<svelte:head>
    <title>Hearth — Calendar</title>
</svelte:head>

<main class="mx-auto max-w-6xl px-8 py-8">
    <Calendar
        tasks={taskStore.tasks}
        onToggle={toggleTask}
        onDelete={removeTask}
        onNewTask={openNewTask}
        onEdit={openEditTask}
        onDateClick={openDateTask}
    />
</main>

<TaskModal
    bind:open={modalOpen}
    task={editingTask}
    {initialDate}
    onAdd={addTask}
    onSave={handleSave}
    onDelete={(id, series) => {
        removeTask(id, series);
        editingTask = null;
    }}
/>
