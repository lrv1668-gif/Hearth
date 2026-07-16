<script lang="ts">
    import type { Task, CalendarItem } from '$lib/api';
    import { onMount } from 'svelte';
    import { taskStore, toggleTask, removeTask, editTask } from '$lib/stores/TaskStore.svelte.ts';
    import { calendarStore, toggleCalendarTask } from '$lib/stores/CalendarStore.svelte.ts';
    import { loadWeather } from '$lib/stores/WeatherStore.svelte.ts';
    import { kioskStore } from '$lib/stores/KioskStore.svelte.ts';
    import UpcomingTasksWidget from '$lib/components/widgets/UpcomingTasksWidget.svelte';
    import TaskModal from '$lib/components/modals/TaskModal.svelte';
    import MoonPhaseWidget from '$lib/components/widgets/MoonPhaseWidget.svelte';
    import CountdownWidget from '$lib/components/widgets/CountdownWidget.svelte';
    import NewsFeedWidget from '$lib/components/widgets/NewsFeedWidget.svelte';
    import DailyQuoteWidget from '$lib/components/widgets/DailyQuoteWidget.svelte';
    import WeatherWidget from '$lib/components/widgets/WeatherWidget.svelte';
    import WidgetContainer from '$lib/components/widgets/WidgetContainer.svelte';
    import EventDetailModal from '$lib/components/modals/EventDetailModal.svelte';
    import DashboardHeader from '$lib/components/DashboardHeader.svelte';
    import DashboardFooter from '$lib/components/DashboardFooter.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import type { AllWidgetId } from '$lib/constants/widgets';
    import { MediaQuery } from 'svelte/reactivity';

    onMount(() => {
        loadWeather();
    });

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);
    let selectedEvent = $state<CalendarItem | null>(null);

    const isMobile = new MediaQuery('(max-width: 800px)');

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
{#snippet renderWidget(id: AllWidgetId)}
    {#if id === 'weather'}
        <WidgetContainer title="Weather" associatedWidgetId="weather">
            <WeatherWidget />
        </WidgetContainer>
    {:else if id === 'upcoming-tasks'}
        <WidgetContainer title="Agenda">
            <UpcomingTasksWidget
                tasks={taskStore.tasks.filter((t) => !t.is_countdown)}
                calItems={calendarStore.items}
                onToggle={toggleTask}
                onDelete={removeTask}
                onEdit={openEditTask}
                onEventClick={(e) => e.kind === 'event' && (selectedEvent = e)}
                onToggleCalendarTask={toggleCalendarTask}
            />
        </WidgetContainer>
    {:else if id === 'countdowns'}
        <WidgetContainer title="Countdowns" associatedWidgetId="countdowns">
            <CountdownWidget tasks={taskStore.tasks} onEdit={openEditTask} />
        </WidgetContainer>
    {:else if id === 'moon-phase'}
        <WidgetContainer title="Moon Phase" associatedWidgetId="moon-phase">
            <MoonPhaseWidget />
        </WidgetContainer>
    {:else if id === 'rss-feeds'}
        <WidgetContainer title="Today's News" associatedWidgetId="rss-feeds">
            <NewsFeedWidget />
        </WidgetContainer>
    {:else if id === 'daily-quote'}
        <WidgetContainer title="Daily Quote" associatedWidgetId="daily-quote">
            <DailyQuoteWidget />
        </WidgetContainer>
    {/if}
{/snippet}

<div class="flex min-h-0 flex-1 flex-col">
    {#if kioskStore.isKiosk}
        <DashboardHeader />
    {/if}

    <main class="mx-auto min-h-0 w-full max-w-7xl overflow-y-auto overflow-x-hidden px-6 py-6 md:px-8">
        <div class="flex w-full gap-8">
            {#if !isMobile.current}
                <div class="flex min-w-0 flex-col gap-4" style="flex: {settings.leftColumnWidth} 1 0%">
                    {#each settings.widgetColumns.left as id}
                        {@render renderWidget(id)}
                    {/each}
                </div>

                <div class="flex min-w-0 flex-col gap-6" style="flex: {100 - settings.leftColumnWidth} 1 0%">
                    {#each settings.widgetColumns.right as id}
                        {@render renderWidget(id)}
                    {/each}
                </div>
            {:else}
                <div class="flex w-full min-w-0 flex-col gap-4">
                    {#each settings.widgetColumns.left.concat(settings.widgetColumns.right) as id}
                        {@render renderWidget(id)}
                    {/each}
                </div>
            {/if}
        </div>
    </main>

    <DashboardFooter class="mt-auto" />
</div>

<TaskModal
    bind:open={modalOpen}
    task={editingTask}
    onSave={handleSave}
    onDelete={(id, series) => {
        removeTask(id, series);
        editingTask = null;
    }}
/>

<EventDetailModal event={selectedEvent} onClose={() => (selectedEvent = null)} />
