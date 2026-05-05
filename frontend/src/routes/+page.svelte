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
    import type { AllWidgetId } from '$lib/constants/widgets';
    import { MediaQuery } from 'svelte/reactivity';

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);

    const isMobile = new MediaQuery('(max-width: 800px)');
    console.log(isMobile);

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

<!-- When adding a new widget component, add it here to be rendered on this page. -->
{#snippet renderWidget(id: AllWidgetId, align?: 'left' | 'right')}
    {#if id === 'upcoming-tasks'}
        <WidgetContainer title="Upcoming Tasks">
            <UpcomingTasksWidget
                tasks={taskStore.tasks.filter((t) => !t.is_countdown)}
                onToggle={toggleTask}
                onDelete={removeTask}
                onEdit={openEditTask}
            />
        </WidgetContainer>
    {:else if id === 'countdowns'}
        <WidgetContainer title="Countdowns" associatedWidgetId="countdowns">
            <CountdownWidget tasks={taskStore.tasks} onEdit={openEditTask} {align} />
        </WidgetContainer>
    {:else if id === 'now-playing'}
        <WidgetContainer title="Now Playing" associatedWidgetId="now-playing">
            <NowPlayingWidget {align} />
        </WidgetContainer>
    {:else if id === 'todays-date'}
        <WidgetContainer title="Today's Date">
            <TodaysDateWidget {align} />
        </WidgetContainer>
    {:else if id === 'weather'}
        <WidgetContainer title="Weather Forecast" associatedWidgetId="weather">
            <WeatherWidget {align} />
        </WidgetContainer>
    {:else if id === 'moon-phase'}
        <WidgetContainer title="Moon Phase" associatedWidgetId="moon-phase">
            <MoonPhaseWidget {align} />
        </WidgetContainer>
    {/if}
{/snippet}

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 overflow-x-hidden">
    <div class="flex w-full gap-8">
        {#if !isMobile.current}
            <div class="flex flex-col gap-4 min-w-0" style="flex: {settings.leftColumnWidth} 1 0%">
                {#each settings.widgetColumns.left as id}
                    {@render renderWidget(id, 'left')}
                {/each}
            </div>

            <div class="flex flex-col gap-6 min-w-0" style="flex: {100 - settings.leftColumnWidth} 1 0%">
                {#each settings.widgetColumns.right as id}
                    {@render renderWidget(id, 'right')}
                {/each}
            </div>
        {:else}
            <div class="flex flex-col gap-4 min-w-0 w-full">
                {#each settings.widgetColumns.left.concat(settings.widgetColumns.right) as id}
                    {@render renderWidget(id, 'left')}
                {/each}
            </div>
        {/if}
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
