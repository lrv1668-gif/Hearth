<script lang="ts">
    import { onMount } from 'svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { birdsStore, loadBirds } from '$lib/stores/BirdsStore.svelte.ts';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));
    let listEl = $state<HTMLDivElement | null>(null);
    let atBottom = $state(false);

    onMount(() => {
        loadPromise = loadBirds();
    });

    function updateAtBottom() {
        if (!listEl) return;
        atBottom = listEl.scrollTop + listEl.clientHeight >= listEl.scrollHeight - 1;
    }

    // Recheck when the list renders or its contents change, so the fade only
    // shows when there actually is more to scroll to.
    $effect(() => {
        birdsStore.sightings;
        updateAtBottom();
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
        return mi < 0.1 ? 'here' : `${mi} miles`;
    }

    // eBird location names are often verbose — full street addresses
    // ("123 Main St, Springfield, IL, US") or hotspot paths with subsites
    // ("Riverside Park--North Meadow (restricted access)"). Keep only the
    // most specific segment, without trailing parentheticals.
    function shortLocation(loc: string): string {
        const specific = loc.split('--').pop() ?? loc;
        return specific
            .split(',')[0]
            .replace(/\s*\(.*\)\s*$/, '')
            .trim();
    }
</script>

<SkeletonLoader promise={loadPromise}>
    {#if birdsStore.error}
        <p class="type-label text-(--text-3)">Bird sightings unavailable.</p>
    {:else if birdsStore.sightings.length === 0}
        <p class="type-label text-(--text-3)">No sightings reported nearby this week.</p>
    {:else}
        <div class="relative">
            <div
                bind:this={listEl}
                class="scroll-thin flex max-h-[min(25vh,320px)] flex-col overflow-y-auto"
                onscroll={updateAtBottom}
            >
                <ul class="flex flex-col gap-2.5">
                    {#each birdsStore.sightings as sighting (sighting.species_code)}
                        <li class="flex items-start gap-2.5">
                            <div class="flex min-w-0 flex-col">
                                <p class="type-body text-(--text-1)">
                                    {sighting.common_name}
                                    {#if sighting.is_notable}
                                        <span class="type-caption text-(--accent)">rare</span>
                                    {/if}
                                </p>
                                <div class="type-label flex gap-2 truncate text-(--text-2)" title={sighting.location}>
                                    <p>{shortLocation(sighting.location)}</p>
                                    <p class="text-(--text-3)">
                                        {distanceLabel(sighting.distance_mi)}, {dayLabel(sighting.observed_at)}
                                    </p>
                                </div>
                            </div>
                        </li>
                    {/each}
                </ul>
            </div>
            {#if !atBottom}
                <div
                    class="pointer-events-none absolute right-0 bottom-0 left-0 h-8"
                    style="background: linear-gradient(to bottom, transparent, var(--bg))"
                ></div>
            {/if}
        </div>
    {/if}
</SkeletonLoader>
