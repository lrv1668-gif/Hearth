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

    const ALWAYS_ON = new Set(['upcoming-tasks', 'todays-date']);

    const renderedWidgetOrder = $derived(
        settings.widgetOrder.filter(
            (id) => ALWAYS_ON.has(id) || (settings.enabledWidgets as string[]).includes(id)
        )
    );

    const gridCols = $derived(
        settings.enabledWidgets.length >= 2 ? 'md:grid-cols-[3fr_2fr]' : 'sm:grid-cols-[3fr_2fr]'
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

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 overflow-x-hidden">
    <div class="grid {gridCols} gap-8 items-start">
        {#each settings.widgetOrder as id (id)}
            {@const align = renderedWidgetOrder.indexOf(id) % 2 === 1 ? 'right' : 'left'}
            {#if id === 'upcoming-tasks'}
                <WidgetContainer title="Upcoming Tasks" {align}>
                    <UpcomingTasksWidget
                        tasks={taskStore.tasks.filter((t) => !t.is_countdown)}
                        onToggle={toggleTask}
                        onDelete={removeTask}
                        onEdit={openEditTask}
                    />
                </WidgetContainer>
            {:else if id === 'countdowns'}
                <WidgetContainer title="Countdowns" associatedWidgetId="countdowns" {align}>
                    <CountdownWidget tasks={taskStore.tasks} onEdit={openEditTask} {align} />
                </WidgetContainer>
            {:else if id === 'now-playing'}
                <WidgetContainer title="Now Playing" associatedWidgetId="now-playing" {align}>
                    <NowPlayingWidget {align} />
                </WidgetContainer>
            {:else if id === 'todays-date'}
                <WidgetContainer title="Today's Date" {align}>
                    <TodaysDateWidget {align} />
                </WidgetContainer>
            {:else if id === 'weather'}
                <WidgetContainer title="Weather Forecast" associatedWidgetId="weather" {align}>
                    <WeatherWidget {align} />
                </WidgetContainer>
            {:else if id === 'moon-phase'}
                <WidgetContainer title="Moon Phase" associatedWidgetId="moon-phase" {align}>
                    <MoonPhaseWidget {align} />
                </WidgetContainer>
            {/if}
        {/each}
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
