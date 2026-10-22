<script lang="ts">
    import type { Quote } from '$lib/api';

    /** `lines`: the frame's line budget — clamps the quote so it never pushes the next
     *  widget off the panel. Omit to show it whole. */
    let { quote, lines }: { quote: Quote | null; lines?: number } = $props();

    // Quote lines are ~0.92 of a body line; the attribution takes ~1.1.
    const clamp = $derived(
        lines === undefined ? undefined : Math.min(6, Math.max(2, Math.floor((lines - 1.1) / 0.92)))
    );
</script>

<!--
@component
The daily quote and its author — shared by the dashboard widget and the e-paper frame.
-->
{#if !quote}
    <p class="type-label text-(--text-2)">Quote unavailable.</p>
{:else}
    <figure class="flex flex-col gap-3">
        <blockquote
            class="type-body leading-snug text-(--text-1) italic {clamp ? 'line-clamp-6' : ''}"
            style={clamp ? `-webkit-line-clamp: ${clamp}` : undefined}
        >
            “{quote.q}”
        </blockquote>
        <figcaption class="type-label text-(--text-2)">— {quote.a}</figcaption>
    </figure>
{/if}
