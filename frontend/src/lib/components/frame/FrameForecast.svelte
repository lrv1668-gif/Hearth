<script lang="ts">
    import { Leaf, SunMedium, Wind } from '@lucide/svelte';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';
    import { forecastBars } from '$lib/frame';
    import { localDateKey } from '$lib/utils';

    let { now }: { now: Date } = $props();

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
The Daily forecast band — the frame's only --surface fill. A conditions line (wind now,
today's peak UV and the air quality, each in words) sits over the days. Each day is a
weekday and icon over a solid low–high bar on a hatched track; every bar shares one
temperature scale, so a day's spread reads against the week. Today is marked with an
--accent dot.
-->
{#if days.length > 0 || hasConditions}
    <div class="-mx-16 border-t-3 border-(--border) bg-(--surface) px-16 pt-7 pb-9">
        {#if hasConditions}
            <div class="type-label mb-6 flex flex-wrap items-center gap-x-12 gap-y-3 leading-none text-(--text-2)">
                {#if current}
                    <span class="inline-flex items-center gap-3">
                        <Wind class="icon-xs" />Wind
                        <span class="font-semibold text-(--text-1)">{Math.round(current.wind_mph)} mph</span>
                    </span>
                {/if}
                {#if today?.uv_index_max != null}
                    <span class="inline-flex items-center gap-3">
                        <SunMedium class="icon-xs" />Peak UV
                        <span class="font-semibold text-(--text-1)">
                            {[Math.round(today.uv_index_max), today.uv_max_category]
                                .filter((v) => v != null)
                                .join(' · ')}
                        </span>
                    </span>
                {/if}
                {#if current?.us_aqi != null}
                    <span class="inline-flex items-center gap-3">
                        <Leaf class="icon-xs" />Air
                        <span class="font-semibold text-(--text-1)">
                            {[current.aqi_category, current.us_aqi].filter((v) => v != null).join(' · ')}
                        </span>
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
