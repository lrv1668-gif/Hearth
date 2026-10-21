<script lang="ts">
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';
    import { forecastBars } from '$lib/frame';
    import { localDateKey } from '$lib/utils';

    let { now }: { now: Date } = $props();

    const days = $derived(weatherStore.forecast.slice(0, 5));
    const bars = $derived(forecastBars(days));
    const todayKey = $derived(localDateKey(now));
</script>

<!--
@component
The Daily forecast band — the frame's only --surface fill. Each day is a weekday and
icon over a solid low–high bar on a hatched track; every bar shares one temperature
scale, so a day's spread reads against the week. Today is marked with an --accent dot.
-->
{#if days.length > 0}
    <div class="-mx-16 grid grid-cols-5 gap-x-10 border-t-3 border-(--border) bg-(--surface) px-16 pt-7 pb-9">
        {#each days as day, i (day.date)}
            {@const Icon = weatherIcon(day.weather_code)}
            <div class="flex min-w-0 flex-col gap-4">
                <div class="flex items-center gap-4">
                    <span
                        class="type-label inline-flex items-center gap-2 leading-none font-semibold tracking-[0.16em] uppercase"
                    >
                        {#if day.date === todayKey}<span class="h-3.5 w-3.5 rounded-full bg-(--accent)"></span>{/if}
                        {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'short' })}
                    </span>
                    <Icon class="icon-md text-(--text-2)" />
                </div>
                <div class="type-label grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2">
                    <span class="text-(--text-2)">{Math.round(day.temp_min_f)}°</span>
                    <span class="hatch relative h-4.5">
                        <span
                            class="absolute inset-y-0 bg-(--text-1)"
                            style="left: {bars[i].left}%; width: {bars[i].width}%"
                        ></span>
                    </span>
                    <span>{Math.round(day.temp_max_f)}°</span>
                </div>
            </div>
        {/each}
    </div>
{/if}
