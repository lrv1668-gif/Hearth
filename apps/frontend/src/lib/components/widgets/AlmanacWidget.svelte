<script lang="ts">
    import { onMount } from 'svelte';
    import SkeletonLoader from '#lib/components/SkeletonLoader.svelte';
    import { almanacStore, loadAlmanac } from '#lib/stores/AlmanacStore.svelte.ts';
    import AlmanacView from './views/AlmanacView.svelte';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadAlmanac();
    });
</script>

<SkeletonLoader promise={loadPromise}>
    <AlmanacView almanac={almanacStore.error ? null : almanacStore.almanac} />
</SkeletonLoader>
