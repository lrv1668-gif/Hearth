<script lang="ts">
    import { Leaf, SunMedium, Wind } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '#lib/constants/weather.ts';
    import { forecastBars } from '#lib/frame.ts';
    import { localDateKey } from '#lib/utils.ts';

    // compact: portrait's narrower columns — the conditions drop their words and show
    // only the readings, so nothing truncates.
    let { now, compact = false }: { now: Date; compact?: boolean } = $props();

    const days = $derived(weatherStore.forecast.slice(0, 5));
    const bars = $derived(forecastBars(days));
    const todayKey = $derived(localDateKey(now));
    // Today's peak UV rather than the current reading: the frame redraws on a schedule,
    // and a dawn render would otherwise show UV 0 all morning.
    const today = $derived(weatherStore.forecast.find((f) => f.date === todayKey) ?? null);
    const current = $derived(weatherStore.current);
    const hasConditions = $derived(current !== null || today?.uv_index_max != null);
</script>

<!--
@component
The Daily forecast band — the frame's only --surface fill. A conditions line (the air quality,
today's peak UV and wind now, each in words) sits over the days on the same five
columns, starting over the first, third and fifth days — wind, the shortest, takes the
single last column. In portrait (`compact`) the words drop and only the readings show.
Each day is a weekday and icon over a solid low–high bar on a hatched
track; every bar shares one temperature scale, so a day's spread reads against the week.
Today is marked with an --accent dot.
-->
{#if days.length > 0 || hasConditions}
    <div class="-mx-16 border-t-3 border-(--border) bg-(--surface) px-16 pt-7 pb-9">
        {#if hasConditions}
            <div class="type-label mb-6 grid grid-cols-5 items-center gap-x-10 leading-none text-(--text-2)">
                {#if current?.us_aqi != null}
                    <span class="col-span-2 col-start-1 flex min-w-0 items-center gap-3 whitespace-nowrap">
                        <Leaf class="icon-xs" />{#if !compact}Air{/if}
                        <span class="truncate font-semibold text-(--text-1)">
                            {compact
                                ? current.us_aqi
                                : [current.aqi_category, current.us_aqi].filter((v) => v != null).join(' · ')}
                        </span>
                    </span>
                {/if}
                {#if today?.uv_index_max != null}
                    <span class="col-span-2 col-start-3 flex min-w-0 items-center gap-3 whitespace-nowrap">
                        <SunMedium class="icon-xs" />{#if !compact}Peak UV{/if}
                        <span class="truncate font-semibold text-(--text-1)">
                            {compact
                                ? Math.round(today.uv_index_max)
                                : [Math.round(today.uv_index_max), today.uv_max_category]
                                      .filter((v) => v != null)
                                      .join(' · ')}
                        </span>
                    </span>
                {/if}
                {#if current}
                    <span class="col-start-5 flex min-w-0 items-center gap-3 whitespace-nowrap">
                        <Wind class="icon-xs" />{#if !compact}Wind{/if}
                        <span class="truncate font-semibold text-(--text-1)">{Math.round(current.wind_mph)} mph</span>
                    </span>
                {/if}
            </div>
        {/if}
        {#if days.length > 0}
            <div class="grid grid-cols-5 gap-x-10">
                {#each days as day, i (day.date)}
                    {@const Icon = weatherIcon(day.weather_code)}
                    <div class="flex min-w-0 flex-col gap-4">
                        <div class="flex items-center gap-4">
                            <span
                                class="type-label inline-flex items-center gap-2 leading-none font-semibold tracking-[0.16em] uppercase"
                            >
                                {#if day.date === todayKey}<span class="h-3.5 w-3.5 rounded-full bg-(--accent)"
                                    ></span>{/if}
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
    </div>
{/if}
