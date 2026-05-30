<script lang="ts">
    import type { Task, CalendarEvent } from '$lib/api';
    import { taskStore, addTask, toggleTask, removeTask, editTask } from '$lib/stores/TaskStore.svelte.ts';
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import Calendar from '$lib/components/Calendar.svelte';
    import TaskModal from '$lib/components/modals/TaskModal.svelte';
    import EventDetailModal from '$lib/components/modals/EventDetailModal.svelte';

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);
    let initialDate = $state<string | undefined>(undefined);
    let selectedEvent = $state<CalendarEvent | null>(null);

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


</script>

<svelte:head>
    <title>Hearth — Calendar</title>
</svelte:head>

<main class="mx-auto flex min-h-0 w-full max-w-7xl flex-col px-8 py-8">
    <Calendar
        tasks={taskStore.tasks}
        events={calendarStore.events}
        onToggle={toggleTask}
        onDelete={removeTask}
        onNewTask={openNewTask}
        onEdit={openEditTask}
        onDateClick={openDateTask}
        onEventClick={(e) => (selectedEvent = e)}
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

<EventDetailModal event={selectedEvent} onClose={() => (selectedEvent = null)} />
