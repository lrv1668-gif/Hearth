<script lang="ts">
    import type { Task, CalendarItem } from '#lib/api.ts';
    import { onMount } from 'svelte';
    import { MINUTE, poll } from '#lib/utils.ts';
    import { taskStore, toggleTask, removeTask, editTask } from '#lib/stores/TaskStore.svelte.ts';
    import { calendarStore, toggleCalendarTask } from '#lib/stores/CalendarStore.svelte.ts';
    import { loadWeather, weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '#lib/constants/weather.ts';
    import { kioskStore } from '#lib/stores/KioskStore.svelte.ts';
    import UpcomingTasksWidget from '#lib/components/widgets/UpcomingTasksWidget.svelte';
    import TaskModal from '#lib/components/modals/TaskModal.svelte';
    import MoonPhaseWidget from '#lib/components/widgets/MoonPhaseWidget.svelte';
    import CountdownWidget from '#lib/components/widgets/CountdownWidget.svelte';
    import NewsFeedWidget from '#lib/components/widgets/NewsFeedWidget.svelte';
    import DailyQuoteWidget from '#lib/components/widgets/DailyQuoteWidget.svelte';
    import DayArcWidget from '#lib/components/widgets/DayArcWidget.svelte';
    import BirdsWidget from '#lib/components/widgets/BirdsWidget.svelte';
    import AlmanacWidget from '#lib/components/widgets/AlmanacWidget.svelte';
    import TrainsWidget from '#lib/components/widgets/TrainsWidget.svelte';
    import SportsWidget from '#lib/components/widgets/SportsWidget.svelte';
    import WeatherWidget from '#lib/components/widgets/WeatherWidget.svelte';
    import WidgetContainer from '#lib/components/widgets/WidgetContainer.svelte';
    import EventDetailModal from '#lib/components/modals/EventDetailModal.svelte';
    import DashboardHeader from '#lib/components/DashboardHeader.svelte';
    import DashboardFooter from '#lib/components/DashboardFooter.svelte';
    import { settings } from '#lib/stores/SettingsStore.svelte.ts';
    import type { AllWidgetId } from '#lib/constants/widgets.ts';
    import { MediaQuery } from 'svelte/reactivity';
    import { Music } from '@lucide/svelte';
    import { spotifyStore } from '#lib/stores/SpotifyStore.svelte.ts';

    // Loaded here rather than in WeatherWidget: the header, footer and day arc read it too.
    onMount(() => {
        loadWeather();
        return poll(loadWeather, 30 * MINUTE);
    });

    // The phone header's date; rechecked each minute so an always-on screen rolls over at midnight.
    let now = $state(new Date());
    onMount(() => poll(() => (now = new Date()), MINUTE));
    const today = $derived(now.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' }));

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);
    let selectedEvent = $state<CalendarItem | null>(null);

    // Matches Tailwind's `lg` — the breakpoint the nav, footer and layout padding switch at.
    const isWide = new MediaQuery('(min-width: 1024px)');

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
    <title>Hearth — Dashboard</title>
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
    {:else if id === 'day-arc'}
        <WidgetContainer title="Today's Arc" associatedWidgetId="day-arc">
            <DayArcWidget />
        </WidgetContainer>
    {:else if id === 'birds'}
        <WidgetContainer title="Birds Nearby" associatedWidgetId="birds">
            <BirdsWidget />
        </WidgetContainer>
    {:else if id === 'almanac'}
        <WidgetContainer title="Almanac" associatedWidgetId="almanac">
            <AlmanacWidget />
        </WidgetContainer>
    {:else if id === 'trains'}
        <WidgetContainer title="Trains & Buses" associatedWidgetId="trains">
            <TrainsWidget />
        </WidgetContainer>
    {:else if id === 'sports'}
        <WidgetContainer title="Sports" associatedWidgetId="sports">
            <SportsWidget />
        </WidgetContainer>
    {/if}
{/snippet}

<div class="flex min-h-0 flex-1 flex-col">
    {#if kioskStore.isKiosk}
        <DashboardHeader />
    {/if}

    <main class="mx-auto min-h-0 w-full max-w-7xl overflow-x-hidden overflow-y-auto px-4 py-4 sm:px-6 sm:py-6 lg:px-8">
        <!-- Below lg there's no header outside kiosk mode, so open on a one-line date and conditions -->
        {#if !kioskStore.isKiosk && !isWide.current}
            <div class="mb-4 flex min-w-0 items-baseline justify-between gap-3">
                <p class="eyebrow whitespace-nowrap">
                    {today}
                </p>
                {#if weatherStore.current}
                    {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
                    <p class="type-body flex min-w-0 items-center gap-1.5 text-(--text-2)">
                        <WeatherIcon class="icon-sm shrink-0 self-center" aria-hidden="true" />
                        <span class="font-semibold text-(--text-1)">
                            {Math.round(weatherStore.current.temperature_f)}°
                        </span>
                        <span class="truncate">{weatherStore.current.description}</span>
                    </p>
                {/if}
            </div>
        {/if}
        <!-- Below lg the footer is hidden, so Now Playing moves up here (DashboardFooter still does the polling) -->
        {#if spotifyStore.nowPlaying}
            <p
                class="type-label mb-4 flex min-w-0 items-center gap-1.5 rounded-lg bg-(--surface) p-2 text-(--text-2) lg:hidden"
            >
                <Music class="icon-sm flex-shrink-0" />
                <span class="truncate">
                    <span class="font-bold text-(--text-1)">{spotifyStore.nowPlaying.title}</span>
                    {spotifyStore.nowPlaying.artist}
                </span>
            </p>
        {/if}

        <div class="flex w-full gap-8">
            {#if isWide.current}
                <div class="flex min-w-0 flex-col gap-6" style="flex: {settings.leftColumnWidth} 1 0%">
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
                <div class="flex w-full min-w-0 flex-col gap-6">
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
