<script lang="ts">
    import { onMount } from 'svelte';
    import { Bird } from '@lucide/svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { birdsStore, loadBirds } from '$lib/stores/BirdsStore.svelte.ts';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadBirds();
    });

    function dayLabel(observedAt: string): string {
        // "YYYY-MM-DD HH:MM" → local Date; no timezone suffix so it parses as local time
        const observed = new Date(observedAt.replace(' ', 'T'));
        const today = new Date();
        const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
        const daysAgo = Math.round((startOfDay(today) - startOfDay(observed)) / 86_400_000);

        if (daysAgo <= 0) return 'today';
        if (daysAgo === 1) return 'yesterday';
        return observed.toLocaleDateString(undefined, { weekday: 'short' });
    }

    function distanceLabel(mi: number): string {
        return mi < 0.1 ? 'here' : `${mi} mi`;
    }

    // eBird location names are often verbose — full street addresses
    // ("123 Main St, Springfield, IL, US") or hotspot paths with subsites
    // ("Riverside Park--North Meadow (restricted access)"). Keep only the
    // most specific segment, without trailing parentheticals.
    function shortLocation(loc: string): string {
        const specific = loc.split('--').pop() ?? loc;
        return specific.split(',')[0].replace(/\s*\(.*\)\s*$/, '').trim();
    }
</script>

<SkeletonLoader promise={loadPromise}>
    {#if birdsStore.error}
        <p class="type-label text-[var(--text-3)]">Bird sightings unavailable.</p>
    {:else if birdsStore.sightings.length === 0}
        <p class="type-label text-[var(--text-3)]">No sightings reported nearby this week.</p>
    {:else}
        <ul class="flex flex-col gap-2.5">
            {#each birdsStore.sightings.slice(0, 6) as sighting (sighting.species_code)}
                <li class="flex items-start gap-2.5">
                    <Bird
                        class="icon-sm mt-1 flex-shrink-0 {sighting.is_notable
                            ? 'text-[var(--accent)]'
                            : 'text-[var(--text-3)]'}"
                    />
                    <div class="flex min-w-0 flex-col">
                        <p class="type-body text-[var(--text-1)]">
                            {sighting.common_name}
                            {#if sighting.is_notable}
                                <span class="type-caption text-[var(--accent)]">rare</span>
                            {/if}
                        </p>
                        <p class="type-label truncate text-[var(--text-3)]" title={sighting.location}>
                            {shortLocation(sighting.location)} · {distanceLabel(sighting.distance_mi)} · {dayLabel(
                                sighting.observed_at
                            )}
                        </p>
                    </div>
                </li>
            {/each}
        </ul>
    {/if}
</SkeletonLoader>
