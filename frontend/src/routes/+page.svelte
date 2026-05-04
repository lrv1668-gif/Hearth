<script lang="ts">
    import type { Task } from '$lib/api';
    import { taskStore, toggleTask, removeTask, editTask } from '$lib/stores/TaskStore.svelte.ts';
    import UpcomingTasksWidget from '$lib/components/widgets/UpcomingTasksWidget.svelte';
    import TaskModal from '$lib/components/modals/TaskModal.svelte';
    import NowPlayingWidget from '$lib/components/widgets/NowPlayingWidget.svelte';
    import WeatherWidget from '$lib/components/widgets/WeatherWidget.svelte';
    import MoonPhaseWidget from '$lib/components/widgets/MoonPhaseWidget.svelte';
    import CountdownWidget from '$lib/components/widgets/CountdownWidget.svelte';
    import TodaysDateWidget from '$lib/components/widgets/TodaysDateWidget.svelte';
    import WidgetContainer from '$lib/components/widgets/WidgetContainer.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';

    const gridCols = $derived(
        settings.enabledWidgets.length >= 2
            ? 'md:grid-cols-[3fr_2fr]'
            : 'sm:grid-cols-[3fr_2fr]'
    );

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);

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
</script>

<svelte:head>
    <title>Hearth — Schedule</title>
</svelte:head>

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 md:py-4">
    <div class="grid {gridCols} gap-8 items-start">
        <WidgetContainer title="Upcoming Tasks">
            <UpcomingTasksWidget
                tasks={taskStore.tasks.filter((t) => !t.is_countdown)}
                onToggle={toggleTask}
                onDelete={removeTask}
                onEdit={openEditTask}
            />
        </WidgetContainer>

        <WidgetContainer title="Countdowns" associatedWidgetId="countdowns">
            <CountdownWidget tasks={taskStore.tasks} onEdit={openEditTask} />
        </WidgetContainer>

        <WidgetContainer title="Now Playing" associatedWidgetId="now-playing">
            <NowPlayingWidget />
        </WidgetContainer>

        <WidgetContainer title="Today's Date">
            <TodaysDateWidget />
        </WidgetContainer>

        <WidgetContainer title="Weather Forecast" associatedWidgetId="weather">
            <WeatherWidget />
        </WidgetContainer>

        <WidgetContainer title="Moon Phase" associatedWidgetId="moon-phase">
            <MoonPhaseWidget />
        </WidgetContainer>
    </div>
</main>

<TaskModal
    bind:open={modalOpen}
    task={editingTask}
    onSave={handleSave}
    onDelete={(id, series) => {
        removeTask(id, series);
        editingTask = null;
    }}
/>
