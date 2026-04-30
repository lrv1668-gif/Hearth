<script lang="ts">
    import type { Task } from '$lib/api';
    import { tasks, addTask, toggleTask, removeTask, editTask } from '$lib/TaskStore';
    import Calendar from '$lib/components/Calendar.svelte';
    import TaskModal from '$lib/components/TaskModal.svelte';

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);

    function openNewTask() {
        editingTask = null;
        modalOpen = true;
    }

    function openEditTask(task: Task) {
        editingTask = task;
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

<main class="max-w-6xl mx-auto px-8 py-8">
    <Calendar
        tasks={$tasks}
        onToggle={toggleTask}
        onDelete={removeTask}
        onNewTask={openNewTask}
        onEdit={openEditTask}
    />
</main>

<TaskModal
    bind:open={modalOpen}
    task={editingTask}
    onAdd={addTask}
    onSave={handleSave}
    onDelete={(id, series) => { removeTask(id, series); editingTask = null; }}
/>
