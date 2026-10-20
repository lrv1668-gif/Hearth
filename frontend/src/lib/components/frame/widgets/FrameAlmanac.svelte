<script lang="ts">
    import { Snowflake, Sprout, Sun } from '@lucide/svelte';
    import { almanacStore } from '$lib/stores/AlmanacStore.svelte.ts';
    import { frostTiming, shortDate, trendText } from '$lib/almanac';

    let { lines }: { lines: number } = $props();

    // The season block needs ~3.3 lines; each extra fact appears only if there's
    // room for it (the daylight line can wrap to two in a narrow column).

    const almanac = $derived(almanacStore.almanac);
    const percent = $derived(almanac ? Math.round(almanac.season.progress * 100) : 0);
</script>

{#if !almanac}
    <p class="type-body text-(--text-2)">Almanac unavailable.</p>
{:else}
    <div class="flex flex-col gap-4">
        <div>
            <div class="flex items-baseline justify-between gap-4">
                <p class="type-body font-semibold">{almanac.season.label}</p>
                <p class="type-label text-(--text-2)">
                    day {almanac.season.day_of_season} of {almanac.season.total_days}
                </p>
            </div>
            <!-- Outlined track + solid fill: pure inks only, no tinted track to dither -->
            <div class="mt-2 h-4 w-full border-2 border-(--border)">
                <div class="h-full bg-(--accent)" style="width: {percent}%"></div>
            </div>
            <p class="type-label mt-2 text-(--text-2)">
                {almanac.season.next_marker} in {almanac.season.days_until_marker} days
            </p>
        </div>
        {#if almanac.daylight && lines >= 5}
            {@const milestone = almanac.daylight.milestones[0]}
            <p class="type-label flex items-start gap-3 text-(--text-2)">
                <Sun class="icon-sm mt-0.5 shrink-0" />
                {trendText(almanac.daylight.trend_minutes_per_day) +
                    (milestone ? ` · ${milestone.label} ${shortDate(milestone.date)}` : '')}
            </p>
        {/if}
        {#if almanac.frost && lines >= 6.2}
            <p class="type-label flex items-start gap-3 text-(--text-2)">
                <Snowflake class="icon-sm mt-0.5 shrink-0" />
                {almanac.frost.label} around {shortDate(almanac.frost.date)} · {frostTiming(almanac.frost.days_until)}
            </p>
        {/if}
        {#if almanac.note && lines >= 8}
            <p class="type-label flex items-start gap-3 text-(--text-2)">
                <Sprout class="icon-sm mt-0.5 shrink-0" />
                {almanac.note}
            </p>
        {/if}
    </div>
{/if}
