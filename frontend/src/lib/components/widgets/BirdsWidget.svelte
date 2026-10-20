<script lang="ts">
    import { onMount } from 'svelte';
    import { MINUTE, poll } from '$lib/utils';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import ScrollFadeList from '$lib/components/ScrollFadeList.svelte';
    import { birdsStore, loadBirds } from '$lib/stores/BirdsStore.svelte.ts';
    import { dayLabel, distanceLabel, shortLocation } from '$lib/birds';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadBirds();
        return poll(loadBirds, 60 * MINUTE);
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if birdsStore.error}
        <p class="type-label text-(--text-2)">Bird sightings unavailable.</p>
    {:else if birdsStore.sightings.length === 0}
        <p class="type-label text-(--text-2)">No sightings reported nearby this week.</p>
    {:else}
        <ScrollFadeList>
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
                                <p class="text-(--text-2)">
                                    {distanceLabel(sighting.distance_mi)}, {dayLabel(sighting.observed_at)}
                                </p>
                            </div>
                        </div>
                    </li>
                {/each}
            </ul>
        </ScrollFadeList>
    {/if}
</SkeletonLoader>
