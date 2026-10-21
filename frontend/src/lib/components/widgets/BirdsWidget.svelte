<script lang="ts">
    import { onMount } from 'svelte';
    import { MINUTE, poll } from '$lib/utils';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import ScrollFadeList from '$lib/components/ScrollFadeList.svelte';
    import { birdsStore, loadBirds } from '$lib/stores/BirdsStore.svelte.ts';
    import BirdSightingsView from './views/BirdSightingsView.svelte';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadBirds();
        return poll(loadBirds, 60 * MINUTE);
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if birdsStore.error}
        <p class="type-body text-(--text-2)">Bird sightings unavailable.</p>
    {:else}
        <ScrollFadeList>
            <BirdSightingsView sightings={birdsStore.sightings} />
        </ScrollFadeList>
    {/if}
</SkeletonLoader>
