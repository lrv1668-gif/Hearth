<script lang="ts">
    import { Droplet } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import FrameAlert from './FrameAlert.svelte';
    import { weatherIcon } from '#lib/constants/weather.ts';
    import { forecastBars, rainChance } from '#lib/frame/sky.ts';
    import { localDateKey } from '#lib/utils.ts';

    let { now }: { now: Date } = $props();

    const days = $derived(weatherStore.forecast.slice(0, 5));
    const bars = $derived(forecastBars(days));
    const todayKey = $derived(localDateKey(now));
    const chances = $derived(days.map(rainChance));
</script>

<!--
@component
The Daily forecast band — the frame's only --surface fill. The Weather service's most
pressing alert (FrameAlert), if there is one, leads it: that's where wind, UV and air
quality show, only on the days they're worth a line. Each day is a weekday and icon
over a solid low–high bar on a hatched track; every bar shares one temperature scale,
so a day's spread reads against the week. Today is marked with an --accent dot. A day
likely to see rain or snow adds its chance at the end of the weekday row, or on a line
under it where the column is too narrow.
-->
{#if days.length > 0 || weatherStore.alerts.length > 0}
    <div class="-mx-16 border-t-3 border-(--border) bg-(--surface) px-16 pt-6 pb-7">
        <FrameAlert class="type-body mb-6 leading-none" />
        {#if days.length > 0}
            <!-- Each day spans two rows of a shared subgrid, so when a narrow column (portrait,
                 a large text size) wraps its rain chance onto a line of its own, every
                 day's bar still sits on the same line. -->
            <div class="grid grid-cols-5 gap-x-10 gap-y-4">
                {#each days as day, i (day.date)}
                    {@const Icon = weatherIcon(day.weather_code)}
                    <div class="row-span-2 grid min-w-0 grid-rows-subgrid">
                        <div class="flex flex-wrap content-start items-center gap-x-4 gap-y-3">
                            <span
                                class="type-label inline-flex items-center gap-2 leading-none font-semibold tracking-[0.16em] uppercase"
                            >
                                {#if day.date === todayKey}<span class="h-3.5 w-3.5 rounded-full bg-(--accent)"
                                    ></span>{/if}
                                {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'short' })}
                            </span>
                            <Icon class="icon-md shrink-0 text-(--text-2)" />
                            {#if chances[i] !== null}
                                <span class="type-label ml-auto flex items-center gap-1 leading-none text-(--text-2)">
                                    <Droplet class="icon-xs" />
                                    <span class="font-semibold text-(--text-1)">{chances[i]}%</span>
                                </span>
                            {/if}
                        </div>
                        <div class="type-label grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2">
                            <span class="text-(--text-2)">{Math.round(day.temp_min)}°</span>
                            <span class="hatch relative h-4.5">
                                <span
                                    class="absolute inset-y-0 bg-(--text-1)"
                                    style="left: {bars[i].left}%; width: {bars[i].width}%"
                                ></span>
                            </span>
                            <span>{Math.round(day.temp_max)}°</span>
                        </div>
                    </div>
                {/each}
            </div>
        {/if}
    </div>
{/if}
