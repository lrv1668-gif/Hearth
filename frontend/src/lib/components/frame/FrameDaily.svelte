<script lang="ts">
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';
    import { frameWidgets, type FrameLayout, type FrameWidgetId } from '$lib/constants/frameWidgets';
    import { allocateLines, COLUMN_LINES } from '$lib/frame';
    import { localDateKey } from '$lib/utils';
    import WidgetContainer from '$lib/components/widgets/WidgetContainer.svelte';
    import DayArcWidget from '$lib/components/widgets/DayArcWidget.svelte';
    import FrameToday from './widgets/FrameToday.svelte';
    import FrameComingUp from './widgets/FrameComingUp.svelte';
    import FrameWeek from './widgets/FrameWeek.svelte';
    import FrameCountdowns from './widgets/FrameCountdowns.svelte';
    import FrameAlmanac from './widgets/FrameAlmanac.svelte';
    import FrameMoon from './widgets/FrameMoon.svelte';
    import FrameQuote from './widgets/FrameQuote.svelte';
    import FrameBirds from './widgets/FrameBirds.svelte';

    interface Props {
        now: Date;
        orientation: 'landscape' | 'portrait';
        layout: FrameLayout;
        /** Left column's width, in percent (landscape only). */
        split: number;
    }

    let { now, orientation, layout, split }: Props = $props();

    // Landscape: the layout's left and right columns side by side at `split`% (one
    // full-width column if the other is empty). Portrait: everything stacked
    // full-width, since two 568px columns would truncate most titles.
    const columns = $derived.by((): FrameWidgetId[][] => {
        if (orientation === 'portrait') return [[...layout.left, ...layout.right]];
        return [layout.left, layout.right].filter((c) => c.length > 0);
    });
    const gridColumns = $derived(columns.length === 2 ? `${split}fr ${100 - split}fr` : '1fr');
    // Each widget's share of its column, in lines of body text — lists size themselves
    // to it. Measured from the real layout (a hidden body-text probe gives the line
    // height under the current font preset); the constant is only the first-paint fallback.
    let middleHeight = $state(0);
    let lineHeight = $state(0);
    const totalLines = $derived(middleHeight && lineHeight ? middleHeight / lineHeight : COLUMN_LINES[orientation]);
    const lines = $derived(new Map(columns.flatMap((c) => [...allocateLines(c, totalLines)])));

    const weekday = $derived(now.toLocaleDateString('en-US', { weekday: 'long' }));
    const date = $derived(now.toLocaleDateString('en-US', { month: 'long', day: 'numeric' }));
    const todayKey = $derived(localDateKey(now));
    const today = $derived(weatherStore.forecast.find((f) => f.date === todayKey) ?? null);

    const label = (id: FrameWidgetId) => frameWidgets.find((w) => w.id === id)!.label;
</script>

{#snippet widget(id: FrameWidgetId)}
    {@const n = lines.get(id) ?? 0}
    <!-- Safety net: if a widget still outgrows its share, it clips inside its own
         box rather than pushing the next widget off the panel. -->
    <div class="min-h-0 shrink overflow-hidden">
        <WidgetContainer title={label(id)}>
            {#if id === 'arc'}
                <DayArcWidget />
            {:else if id === 'today'}
                <FrameToday {now} lines={n} />
            {:else if id === 'coming-up'}
                <FrameComingUp {now} lines={n} />
            {:else if id === 'week'}
                <FrameWeek {now} lines={n} />
            {:else if id === 'countdowns'}
                <FrameCountdowns lines={n} />
            {:else if id === 'almanac'}
                <FrameAlmanac lines={n} />
            {:else if id === 'moon'}
                <FrameMoon {now} />
            {:else if id === 'quote'}
                <FrameQuote lines={n} />
            {:else if id === 'birds'}
                <FrameBirds {now} lines={n} />
            {/if}
        </WidgetContainer>
    </div>
{/snippet}

<!--
@component
Daily Mode for the e-paper frame: a fixed masthead (date and weather) and forecast
band, with the configurable widget columns from `layout` in between (see
constants/frameWidgets.ts). Zones are separated by rules rather than tinted panels,
which would dither into speckle.
-->
<div class="grid h-full grid-rows-[auto_minmax(0,1fr)_auto] gap-10 px-16 pt-12 pb-10">
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

    <!-- Middle: the configurable widget slots -->
    <div
        bind:clientHeight={middleHeight}
        class="relative grid min-h-0 gap-16 overflow-hidden"
        style="grid-template-columns: {gridColumns}"
    >
        <p bind:clientHeight={lineHeight} class="type-body invisible absolute" aria-hidden="true">X</p>
        {#each columns as column, c (c)}
            <div class="flex min-w-0 flex-col gap-10">
                {#each column as id (id)}
                    {@render widget(id)}
                {/each}
            </div>
        {/each}
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
