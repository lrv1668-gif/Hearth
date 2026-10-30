<script lang="ts">
    import { onMount } from 'svelte';
    import { MINUTE, poll } from '#lib/utils.ts';
    import SkeletonLoader from '#lib/components/SkeletonLoader.svelte';
    import { dailyQuoteStore, loadDailyQuote } from '#lib/stores/DailyQuoteStore.svelte.ts';
    import DailyQuoteView from './views/DailyQuoteView.svelte';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadDailyQuote();
        return poll(loadDailyQuote, 60 * MINUTE);
    });
</script>

<SkeletonLoader promise={loadPromise}>
    <DailyQuoteView quote={dailyQuoteStore.error ? null : dailyQuoteStore.quote} />
</SkeletonLoader>
