<script lang="ts">
    import { onThisDayStore, yearLabel } from '#lib/stores/OnThisDayStore.svelte.ts';
    import { itemsThatFit } from '#lib/frame.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FitText from '../FitText.svelte';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `lines`: the line budget; `size`: the template slot's size class, omitted in the free columns.
    let { lines, size }: { lines: number; size?: FrameSize } = $props();
    const small = $derived(size === undefined || size === 's');

    // An event (year, then up to three lines of text) is ~4.4 lines with its gap; the
    // Wikipedia credit, with the space above it, takes about 1.4.
    const ITEM_LINES = 4.4;
    const CREDIT_LINES = 1.4;
    // M and L lead with one event in the display face; it takes this share of the slot.
    const LEAD_SHARE = $derived(size === 'l' ? 0.55 : 0.5);

    const events = $derived(onThisDayStore.day?.events ?? []);
    const lead = $derived(small ? undefined : events[0]);
    const listed = $derived(
        small
            ? events.slice(0, Math.min(3, itemsThatFit(lines - CREDIT_LINES, ITEM_LINES)))
            : events.slice(
                  1,
                  1 + Math.min(4, itemsThatFit((lines - CREDIT_LINES) * (1 - LEAD_SHARE), ITEM_LINES - 1.2))
              )
    );
</script>

{#snippet credit()}
    <p class="type-caption mt-auto shrink-0 pt-[0.6em] text-(--text-2)">From Wikipedia · CC BY-SA</p>
{/snippet}

<!--
@component
The e-paper frame's On This Day: events from Wikipedia's "On this day" for the household's
date, spread across the centuries and held all day by the History service. S slots and the
free columns list them, year first; M and L lead with one in the display face. Wikipedia's
text is CC BY-SA, so every size carries the credit.
-->
{#if onThisDayStore.error}
    <FrameEmpty text="On this day unavailable" {small} />
{:else if events.length === 0}
    <FrameEmpty text="Nothing for today's date" {small} />
{:else}
    <!-- Fills a template slot so the credit sits at its foot; in the free columns it
         takes its content's height (the section's height includes the kicker). -->
    <div class="flex flex-col {size ? 'h-full' : ''}">
        {#if lead}
            <div class="min-h-0" style:flex-basis="{LEAD_SHARE * 100}%">
                <FitText min={30} max={size === 'l' ? 96 : 64} key={lead.text}>
                    <p
                        class="font-(family-name:--display-family) text-[1.6em] leading-none font-semibold text-(--accent) tabular-nums"
                    >
                        {yearLabel(lead.year)}
                    </p>
                    <p class="mt-[0.3em] font-(family-name:--display-family) leading-[1.15] font-medium text-balance">
                        {lead.text}
                    </p>
                </FitText>
            </div>
            {#if listed.length > 0}
                <div class="my-6 h-[3px] shrink-0 bg-(--border)"></div>
            {/if}
        {/if}
        <ul class="type-body flex min-h-0 flex-col gap-[0.6em] overflow-hidden">
            {#each listed as e (`${e.year}:${e.text}`)}
                <li class="min-w-0">
                    <p class="font-(family-name:--display-family) font-semibold text-(--text-1) tabular-nums">
                        {yearLabel(e.year)}
                    </p>
                    <p class="text-(--text-1) {small ? 'line-clamp-3' : 'line-clamp-2'}">{e.text}</p>
                </li>
            {/each}
        </ul>
        {@render credit()}
    </div>
{/if}
