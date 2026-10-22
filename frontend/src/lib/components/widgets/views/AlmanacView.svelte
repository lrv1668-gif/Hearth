<script lang="ts">
    import { Snowflake, Sprout, Sun } from '@lucide/svelte';
    import type { AlmanacResponse } from '$lib/api';
    import { frostTiming, shortDate, trendText } from '$lib/almanac';

    /** `lines`: the frame's line budget — each extra fact appears only if there's room
     *  (the season block needs ~3.3 lines; the daylight line can wrap to two in a
     *  narrow column). Omit to show every fact. */
    let { almanac, lines }: { almanac: AlmanacResponse | null; lines?: number } = $props();

    const fits = (needed: number) => lines === undefined || lines >= needed;
    const percent = $derived(almanac ? Math.round(almanac.season.progress * 100) : 0);
</script>

<!--
@component
Season progress, the daylight trend, frost and a seasonal note — shared by the
dashboard widget and the e-paper frame. The season bar is a .meter: rounded and tinted
on the dashboard, an outlined pure-ink bar on the frame (see routes/frame).
-->
{#if !almanac}
    <p class="type-label text-(--text-2)">Almanac unavailable.</p>
{:else}
    <div class="type-body flex flex-col gap-[0.6em]">
        <div>
            <div class="flex items-baseline justify-between gap-4">
                <p class="font-semibold text-(--text-1)">{almanac.season.label}</p>
                <p class="type-label text-(--text-2)">
                    day {almanac.season.day_of_season} of {almanac.season.total_days}
                </p>
            </div>
            <div class="meter mt-[0.4em]"><div style="width: {percent}%"></div></div>
            <p class="type-label mt-[0.4em] text-(--text-2)">
                {almanac.season.next_marker} in {almanac.season.days_until_marker} days
            </p>
        </div>
        {#if almanac.daylight && fits(5)}
            {@const milestone = almanac.daylight.milestones[0]}
            <p class="type-label flex items-start gap-[0.6em] text-(--text-2)">
                <Sun class="icon-sm mt-0.5 shrink-0 text-(--text-3)" />
                {trendText(almanac.daylight.trend_minutes_per_day) +
                    (milestone ? ` · ${milestone.label} ${shortDate(milestone.date)}` : '')}
            </p>
        {/if}
        {#if almanac.frost && fits(6.2)}
            <p class="type-label flex items-start gap-[0.6em] text-(--text-2)">
                <Snowflake class="icon-sm mt-0.5 shrink-0 text-(--text-3)" />
                {almanac.frost.label} around {shortDate(almanac.frost.date)} · {frostTiming(almanac.frost.days_until)}
            </p>
        {/if}
        {#if almanac.note && fits(8)}
            <p class="type-label flex items-start gap-[0.6em] text-(--text-2)">
                <Sprout class="icon-sm mt-0.5 shrink-0 text-(--text-3)" />
                {almanac.note}
            </p>
        {/if}
    </div>
{/if}
