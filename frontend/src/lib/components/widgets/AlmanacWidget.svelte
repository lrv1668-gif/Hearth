<script lang="ts">
    import { onMount } from 'svelte';
    import { Snowflake, Sprout, Sun } from '@lucide/svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { almanacStore, loadAlmanac } from '$lib/stores/AlmanacStore.svelte.ts';
    import { frostTiming, shortDate, trendText } from '$lib/almanac';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadAlmanac();
    });

    const almanac = $derived(almanacStore.almanac);
    const percent = $derived(almanac ? Math.round(almanac.season.progress * 100) : 0);
</script>

<SkeletonLoader promise={loadPromise}>
    {#if almanacStore.error || !almanac}
        <p class="type-label text-(--text-2)">Almanac unavailable.</p>
    {:else}
        <div class="flex flex-col gap-3">
            <div class="flex flex-col gap-1.5">
                <div class="flex items-baseline justify-between gap-2">
                    <p class="type-body font-medium text-(--text-1)">{almanac.season.label}</p>
                    <p class="type-label text-(--text-2)">
                        day {almanac.season.day_of_season} of {almanac.season.total_days}
                    </p>
                </div>
                <div class="h-1 w-full overflow-hidden rounded-full bg-(--surface-hi)">
                    <div class="h-full rounded-full bg-(--accent)" style="width: {percent}%"></div>
                </div>
                <p class="type-label text-(--text-2)">
                    {almanac.season.next_marker} in {almanac.season.days_until_marker} days
                </p>
            </div>

            {#if almanac.daylight}
                {@const milestone = almanac.daylight.milestones[0]}
                <div class="flex items-start gap-2">
                    <Sun class="icon-sm mt-0.5 shrink-0 text-(--accent)" />
                    <p class="type-label text-(--text-2)">
                        {trendText(almanac.daylight.trend_minutes_per_day) +
                            (milestone ? ` · ${milestone.label} ${shortDate(milestone.date)}` : '')}
                    </p>
                </div>
            {/if}

            {#if almanac.frost}
                <div class="flex items-start gap-2">
                    <Snowflake class="icon-sm mt-0.5 shrink-0 text-(--text-3)" />
                    <p class="type-label text-(--text-2)">
                        {almanac.frost.label} around {shortDate(almanac.frost.date)} · {frostTiming(
                            almanac.frost.days_until
                        )}
                    </p>
                </div>
            {/if}

            {#if almanac.note}
                <div class="flex items-start gap-2">
                    <Sprout class="icon-sm mt-0.5 shrink-0 text-(--text-3)" />
                    <p class="type-label text-(--text-2)">{almanac.note}</p>
                </div>
            {/if}
        </div>
    {/if}
</SkeletonLoader>
