<script lang="ts">
    import { dailyQuoteStore } from '#lib/stores/DailyQuoteStore.svelte.ts';
    import FitText from '../FitText.svelte';
    import FrameEmpty from '../FrameEmpty.svelte';

    // M is set left; L is the slot's centrepiece, centred, and may grow to poster size.
    let { large = false }: { large?: boolean } = $props();

    const quote = $derived(dailyQuoteStore.quote);
</script>

<!--
@component
The daily quote for a template's M or L slot: a pull quote in the display face, sized to
fill the slot (FitText), with the author below at half its size. S slots and the free
columns use the shared DailyQuoteView instead.
-->
{#if !quote}
    <FrameEmpty text="Quote unavailable" />
{:else}
    <FitText min={32} max={large ? 150 : 84} key={quote.q} class={large ? 'text-center' : ''}>
        <figure class="px-[0.2em]">
            <blockquote
                class="font-(family-name:--display-family) leading-[1.15] font-medium tracking-[-0.01em] text-balance italic"
            >
                “{quote.q}”
            </blockquote>
            <div class="my-[0.4em] h-[0.08em] w-[1.5em] bg-(--block) {large ? 'mx-auto' : ''}"></div>
            <figcaption class="text-[0.42em] font-semibold tracking-[0.16em] text-(--text-2) uppercase">
                {quote.a}
            </figcaption>
        </figure>
    </FitText>
{/if}
