<script lang="ts">
    import { Music } from '@lucide/svelte';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import { spotifyStore } from '$lib/stores/SpotifyStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';
    import { formatTime, localDateKey } from '$lib/utils';
    import { nextEvents, relativeDayLabel, todayTasks } from '$lib/frame';
    import WidgetContainer from '$lib/components/widgets/WidgetContainer.svelte';
    import DayArcWidget from '$lib/components/widgets/DayArcWidget.svelte';
    import CountdownWidget from '$lib/components/widgets/CountdownWidget.svelte';

    let { now, orientation }: { now: Date; orientation: 'landscape' | 'portrait' } = $props();

    // Portrait stacks the middle zones in one full-width column (two 568px columns
    // would truncate most event titles at frame type sizes), so each list gets less height.
    const portrait = $derived(orientation === 'portrait');
    const maxTasks = $derived(portrait ? 6 : 7);

    const weekday = $derived(now.toLocaleDateString('en-US', { weekday: 'long' }));
    const date = $derived(now.toLocaleDateString('en-US', { month: 'long', day: 'numeric' }));
    const todayKey = $derived(localDateKey(now));
    const today = $derived(weatherStore.forecast.find((f) => f.date === todayKey) ?? null);

    const tasks = $derived(todayTasks(taskStore.tasks, now));
    const events = $derived(nextEvents(calendarStore.items, now, portrait ? 4 : 3));
    const hasCountdowns = $derived(taskStore.tasks.some((t) => t.is_countdown && !t.done));

    function eventTime(start: string, allDay: boolean): string {
        if (allDay) return 'All day';
        return new Date(start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }
</script>

<!--
@component
Daily Mode for the e-paper frame (PRODUCT.md "Daily Mode" zones, plus Today's Arc and
countdowns): date and weather on top, the day's arc, today's tasks on the left, what's
coming up on the right (stacked below the tasks in portrait), and a 5-day forecast along
the bottom. Zones are separated by rules rather than tinted panels, which would dither
into speckle.
-->
<div class="grid h-full grid-rows-[auto_auto_minmax(0,1fr)_auto] gap-10 px-16 pt-12 pb-10">
    <!-- Top: date + current weather -->
    <header class="flex items-end justify-between border-b-2 border-(--border) pb-6">
        <div>
            <p class="type-display leading-none font-bold">{weekday}</p>
            <p class="type-subtitle mt-3 text-(--text-2)">{date}</p>
        </div>
        {#if weatherStore.current}
            {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
            <div class="flex items-center gap-6">
                <WeatherIcon class="h-24 w-24 text-(--text-2)" />
                <div class="text-right">
                    <p class="type-display leading-none font-semibold">
                        {Math.round(weatherStore.current.temperature_f)}°
                    </p>
                    <p class="type-label mt-2 text-(--text-2)">
                        {weatherStore.current.description}
                        {#if today}
                            <span class="ml-2"
                                >H {Math.round(today.temp_max_f)}° · L {Math.round(today.temp_min_f)}°</span
                            >
                        {/if}
                    </p>
                </div>
            </div>
        {/if}
    </header>

    <DayArcWidget />

    <!-- Middle: today's tasks | what's coming up (stacked in portrait) -->
    <div class="grid min-h-0 overflow-hidden {portrait ? 'grid-cols-1 content-start gap-10' : 'grid-cols-2 gap-16'}">
        <WidgetContainer title="Today">
            {#if tasks.length === 0}
                <p class="type-body text-(--text-2)">Nothing due today.</p>
            {:else}
                <ul class="space-y-4">
                    {#each tasks.slice(0, maxTasks) as task (task.id)}
                        <li class="flex items-baseline gap-4">
                            <span class="h-6 w-6 shrink-0 translate-y-0.5 border-2 border-(--border)"></span>
                            <span class="type-body min-w-0 flex-1 truncate">{task.title}</span>
                            {#if task.due_time}
                                <span class="type-label text-(--text-2)">{formatTime(task.due_time)}</span>
                            {/if}
                        </li>
                    {/each}
                </ul>
                {#if tasks.length > maxTasks}
                    <p class="type-label mt-4 text-(--text-2)">+{tasks.length - maxTasks} more</p>
                {/if}
            {/if}
        </WidgetContainer>

        <div class="flex min-w-0 flex-col gap-8">
            <WidgetContainer title="Coming Up">
                {#if events.length === 0}
                    <p class="type-body text-(--text-2)">No upcoming events.</p>
                {:else}
                    <ul class="space-y-4">
                        {#each events as event (event.id)}
                            <li class="min-w-0">
                                <p class="type-body truncate">{event.title}</p>
                                <p class="type-label text-(--text-2)">
                                    {relativeDayLabel(event, now)} · {eventTime(event.start!, event.is_all_day)}
                                </p>
                            </li>
                        {/each}
                    </ul>
                {/if}
            </WidgetContainer>

            {#if hasCountdowns}
                <WidgetContainer title="Countdowns">
                    <CountdownWidget tasks={taskStore.tasks} onEdit={() => {}} limit={3} />
                </WidgetContainer>
            {/if}

            {#if spotifyStore.nowPlaying?.is_playing}
                <p class="type-label flex min-w-0 items-center gap-3 text-(--text-2)">
                    <Music class="icon-sm shrink-0" />
                    <span class="truncate">
                        <span class="font-semibold text-(--text-1)">{spotifyStore.nowPlaying.title}</span>
                        · {spotifyStore.nowPlaying.artist}
                    </span>
                </p>
            {/if}
        </div>
    </div>

    <!-- Bottom: 5-day forecast -->
    {#if weatherStore.forecast.length > 0}
        <div class="-mx-16 -mb-10 grid grid-cols-5 border-t-2 border-(--border) bg-(--surface) px-16 py-8">
            {#each weatherStore.forecast.slice(0, 5) as day, i (day.date)}
                {@const ForecastIcon = weatherIcon(day.weather_code)}
                <div class="flex items-center justify-center gap-4 {i > 0 ? 'border-l-2 border-(--border)' : ''}">
                    <ForecastIcon class="icon-lg text-(--text-2)" />
                    <div>
                        <p class="type-label font-semibold uppercase">
                            {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'short' })}
                        </p>
                        <p class="type-label">
                            {Math.round(day.temp_max_f)}°
                            <span class="text-(--text-2)">{Math.round(day.temp_min_f)}°</span>
                        </p>
                    </div>
                </div>
            {/each}
        </div>
    {/if}
</div>
