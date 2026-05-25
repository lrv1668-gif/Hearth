<script lang="ts">
    import { onMount } from 'svelte';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';
    import { dailyQuoteStore, loadDailyQuote } from '$lib/stores/DailyQuoteStore.svelte.ts';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = loadDailyQuote();
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if dailyQuoteStore.error || !dailyQuoteStore.quote}
        <p class="type-body text-[var(--text-2)]">Quote unavailable.</p>
    {:else}
        <div class="flex flex-col gap-3">
            <p class="type-body italic leading-relaxed text-[var(--text-1)]">
                "{dailyQuoteStore.quote.q}"
            </p>
            <p class="type-label text-[var(--text-2)]">— {dailyQuoteStore.quote.a}</p>
        </div>
    {/if}
</SkeletonLoader>
