<script lang="ts">
    import { Droplet } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import FrameAlert from './FrameAlert.svelte';
    import { weatherIcon } from '#lib/constants/weather.ts';
    import { forecastBars, rainChance } from '#lib/frame/sky.ts';
    import { localDateKey } from '#lib/utils.ts';

    // Weather mode reuses the band: `count` days, its own alerts above (`alert` off), and
    // in portrait one day per row (`rows`), where seven columns would be too narrow.
    let {
        now,
        count = 5,
        alert = true,
        rows = false,
    }: { now: Date; count?: number; alert?: boolean; rows?: boolean } = $props();

    const days = $derived(weatherStore.forecast.slice(0, count));
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
{#snippet weekday(day: (typeof days)[number])}
    <span class="type-label inline-flex items-center gap-2 leading-none font-semibold tracking-[0.16em] uppercase">
        {#if day.date === todayKey}<span class="h-3.5 w-3.5 rounded-full bg-(--accent)"></span>{/if}
        {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'short' })}
    </span>
{/snippet}

<!-- The chance, and what's expected when there's enough to say ("0.3 in", "2 in snow");
     the amount wraps under the chance where the column is narrow. -->
{#snippet chance(value: number | null, expected: string | null)}
    <span class="type-label flex flex-wrap items-center gap-x-2 gap-y-1 leading-none text-(--text-2)">
        <span class="flex items-center gap-1">
            <Droplet class="icon-xs" />
            {#if value !== null}<span class="font-semibold text-(--text-1)">{value}%</span>{/if}
        </span>
        {#if expected}<span class="whitespace-nowrap">{expected}</span>{/if}
    </span>
{/snippet}

{#snippet range(day: (typeof days)[number], bar: { left: number; width: number })}
    <span class="text-(--text-2)">{Math.round(day.temp_min)}°</span>
    <span class="hatch relative h-4.5">
        <span class="absolute inset-y-0 bg-(--text-1)" style="left: {bar.left}%; width: {bar.width}%"></span>
    </span>
    <span>{Math.round(day.temp_max)}°</span>
{/snippet}

{#if days.length > 0 || (alert && weatherStore.alerts.length > 0)}
    <div class="-mx-16 border-t-3 border-(--border) bg-(--surface) px-16 pt-6 pb-7">
        {#if alert}<FrameAlert class="type-body mb-6 leading-none" />{/if}
        {#if days.length > 0 && rows}
            <!-- One day per row on a shared grid, so the weekdays, chances and bars line up. -->
            <div
                class="type-label grid grid-cols-[auto_auto_minmax(0,0.6fr)_auto_minmax(0,2fr)_auto] items-center gap-x-6 gap-y-5"
            >
                {#each days as day, i (day.date)}
                    {@const Icon = weatherIcon(day.weather_code)}
                    {@render weekday(day)}
                    <Icon class="icon-md shrink-0 text-(--text-2)" />
                    <span
                        >{#if chances[i] !== null || day.expected}{@render chance(chances[i], day.expected)}{/if}</span
                    >
                    {@render range(day, bars[i])}
                {/each}
            </div>
        {:else if days.length > 0}
            <!-- Each day spans two rows of a shared subgrid, so when a narrow column (portrait,
                 a large text size) wraps its rain chance onto a line of its own, every
                 day's bar still sits on the same line. -->
            <div class="grid gap-x-10 gap-y-4" style="grid-template-columns: repeat({days.length}, minmax(0, 1fr))">
                {#each days as day, i (day.date)}
                    {@const Icon = weatherIcon(day.weather_code)}
                    <div class="row-span-2 grid min-w-0 grid-rows-subgrid">
                        <div class="flex flex-wrap content-start items-center gap-x-4 gap-y-3">
                            {@render weekday(day)}
                            <Icon class="icon-md shrink-0 text-(--text-2)" />
                            {#if chances[i] !== null || day.expected}
                                <span class="ml-auto">{@render chance(chances[i], day.expected)}</span>
                            {/if}
                        </div>
                        <div class="type-label grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2">
                            {@render range(day, bars[i])}
                        </div>
                    </div>
                {/each}
            </div>
        {/if}
    </div>
{/if}
