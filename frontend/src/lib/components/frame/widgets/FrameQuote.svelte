<script lang="ts">
    import { dailyQuoteStore } from '$lib/stores/DailyQuoteStore.svelte.ts';

    let { lines }: { lines: number } = $props();

    // Quote lines are ~0.92 of a body line; the attribution takes ~1.1.
    const clamp = $derived(Math.min(6, Math.max(2, Math.floor((lines - 1.1) / 0.92))));
</script>

{#if !dailyQuoteStore.quote}
    <p class="type-body text-(--text-2)">Quote unavailable.</p>
{:else}
    <figure class="flex flex-col gap-3">
        <!-- Clamp so a long quote never pushes the next widget off the panel -->
        <blockquote class="type-body line-clamp-6 leading-snug italic" style="-webkit-line-clamp: {clamp}">
            “{dailyQuoteStore.quote.q}”
        </blockquote>
        <figcaption class="type-label text-(--text-2)">— {dailyQuoteStore.quote.a}</figcaption>
    </figure>
{/if}
