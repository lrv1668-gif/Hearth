<script lang="ts">
    import type { Quote } from '$lib/api';

    /** `lines`: the frame's line budget — clamps the quote so it never pushes the next
     *  widget off the panel. Omit to show it whole. `feature`: set it as a pull quote
     *  in the display face (a frame template's L slot). */
    let { quote, lines, feature = false }: { quote: Quote | null; lines?: number; feature?: boolean } = $props();

    // Quote lines are ~0.92 of a body line (~1.4 as a pull quote); the attribution takes ~1.1.
    const lineLines = $derived(feature ? 1.4 : 0.92);
    const clamp = $derived(
        lines === undefined ? undefined : Math.min(feature ? 8 : 6, Math.max(2, Math.floor((lines - 1.1) / lineLines)))
    );
</script>

<!--
@component
The daily quote and its author — shared by the dashboard widget and the e-paper frame.
-->
{#if !quote}
    <p class="type-body text-(--text-2)">Quote unavailable.</p>
{:else}
    <figure class="flex flex-col {feature ? 'gap-5' : 'gap-3'}">
        <blockquote
            class="leading-snug text-(--text-1) italic {feature
                ? 'type-title font-(family-name:--display-family) font-medium tracking-[-0.01em] text-balance'
                : 'type-body'} {clamp ? 'line-clamp-6' : ''}"
            style={clamp ? `-webkit-line-clamp: ${clamp}` : undefined}
        >
            “{quote.q}”
        </blockquote>
        <figcaption class="type-label text-(--text-2)">— {quote.a}</figcaption>
    </figure>
{/if}
