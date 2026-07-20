<script lang="ts">
    import { onMount } from 'svelte';
    import { Snowflake, Sprout, Sun } from '@lucide/svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { api, type AlmanacResponse } from '$lib/api';

    let almanac = $state<AlmanacResponse | null>(null);
    let error = $state(false);
    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = (async () => {
            try {
                almanac = await api.almanac.today();
                if (!almanac) error = true;
            } catch {
                error = true;
            }
        })();
    });

    const percent = $derived(almanac ? Math.round(almanac.season.progress * 100) : 0);

    function shortDate(iso: string): string {
        // "YYYY-MM-DD" without a time parses as UTC — split to keep it a local date
        const [y, m, d] = iso.split('-').map(Number);
        return new Date(y, m - 1, d).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
    }

    function trendText(perDay: number): string {
        if (perDay === 0) return 'Day length is holding steady';
        const amount = Math.abs(perDay) < 1 ? 'under a minute' : `~${Math.round(Math.abs(perDay))} min`;
        return `${perDay < 0 ? 'Losing' : 'Gaining'} ${amount} of daylight each day`;
    }

    function frostTiming(days: number): string {
        if (days === 0) return 'today';
        if (days <= 21) return `in ${days} day${days === 1 ? '' : 's'}`;
        return `in ~${Math.round(days / 7)} weeks`;
    }
</script>

<SkeletonLoader promise={loadPromise}>
    {#if error || !almanac}
        <p class="type-label text-[var(--text-3)]">Almanac unavailable.</p>
    {:else}
        <div class="flex flex-col gap-3">
            <div class="flex flex-col gap-1.5">
                <div class="flex items-baseline justify-between gap-2">
                    <p class="type-body font-medium text-[var(--text-1)]">{almanac.season.label}</p>
                    <p class="type-label text-[var(--text-3)]">
                        day {almanac.season.day_of_season} of {almanac.season.total_days}
                    </p>
                </div>
                <div class="h-1 w-full overflow-hidden rounded-full bg-[var(--surface-hi)]">
                    <div class="h-full rounded-full bg-[var(--accent)]" style="width: {percent}%"></div>
                </div>
                <p class="type-label text-[var(--text-2)]">
                    {almanac.season.next_marker} in {almanac.season.days_until_marker} days
                </p>
            </div>

            {#if almanac.daylight}
                {@const milestone = almanac.daylight.milestones[0]}
                <div class="flex items-start gap-2">
                    <Sun class="icon-sm mt-0.5 shrink-0 text-[var(--accent)]" />
                    <p class="type-label text-[var(--text-2)]">
                        {trendText(almanac.daylight.trend_minutes_per_day) +
                            (milestone ? ` · ${milestone.label} ${shortDate(milestone.date)}` : '')}
                    </p>
                </div>
            {/if}

            {#if almanac.frost}
                <div class="flex items-start gap-2">
                    <Snowflake class="icon-sm mt-0.5 shrink-0 text-[var(--text-3)]" />
                    <p class="type-label text-[var(--text-2)]">
                        {almanac.frost.label} around {shortDate(almanac.frost.date)} · {frostTiming(
                            almanac.frost.days_until
                        )}
                    </p>
                </div>
            {/if}

            {#if almanac.note}
                <div class="flex items-start gap-2">
                    <Sprout class="icon-sm mt-0.5 shrink-0 text-[var(--text-3)]" />
                    <p class="type-label text-[var(--text-2)]">{almanac.note}</p>
                </div>
            {/if}
        </div>
    {/if}
</SkeletonLoader>
