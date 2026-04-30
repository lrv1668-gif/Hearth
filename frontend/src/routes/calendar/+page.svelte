<script lang="ts">
    import { tasks, addTask, toggleTask, removeTask } from '$lib/TaskStore';
    import Calendar from '$lib/components/Calendar.svelte';
    import TaskModal from '$lib/components/TaskModal.svelte';

    let modalOpen = $state(false);
</script>

<svelte:head>
    <title>Hearth — Calendar</title>
</svelte:head>

<main class="max-w-6xl mx-auto px-8 py-8">
    <Calendar
        tasks={$tasks}
        onToggle={toggleTask}
        onDelete={removeTask}
        onNewTask={() => (modalOpen = true)}
    />
</main>

<TaskModal bind:open={modalOpen} onAdd={addTask} />
