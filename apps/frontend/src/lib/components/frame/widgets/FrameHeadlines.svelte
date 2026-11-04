<script lang="ts">
    import { headlinesStore } from '#lib/stores/HeadlinesStore.svelte.ts';
    import { itemsThatFit } from '#lib/frame.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FitText from '../FitText.svelte';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `lines`: the line budget; `size`: the template slot's size class, omitted in the free columns.
    let { lines, size }: { lines: number; size?: FrameSize } = $props();
    const small = $derived(size === undefined || size === 's');

    // A story (up to two title lines plus its source) is ~3.4 lines with its gap.
    const ITEM_LINES = 3.4;
    // M and L lead with one story in the display face; it takes this share of the slot.
    const LEAD_SHARE = $derived(size === 'l' ? 0.45 : 0.4);

    const all = $derived(headlinesStore.headlines);
    const listed = $derived(
        small
            ? all.slice(0, Math.min(6, itemsThatFit(lines, ITEM_LINES)))
            : all.slice(1, 1 + Math.min(5, itemsThatFit(lines * (1 - LEAD_SHARE), ITEM_LINES)))
    );
    const lead = $derived(small ? undefined : all[0]);
</script>

{#snippet story(title: string, source: string)}
    <li class="min-w-0">
        <p class="line-clamp-2 font-medium text-(--text-1)">{title}</p>
        <p class="type-label truncate font-semibold tracking-[0.08em] text-(--text-2) uppercase">{source}</p>
    </li>
{/snippet}

<!--
@component
The e-paper frame's Headlines: the newest stories across the household's feeds, as the
Rss service picks them once per clock hour, so the panel redraws for news at most hourly.
S slots and the free columns list them; M and L lead with the top story in the display
face, broadsheet-style, above the rest.
-->
{#if headlinesStore.error}
    <FrameEmpty text="Headlines unavailable" {small} />
{:else if all.length === 0}
    <FrameEmpty text="Add a feed in Settings to show headlines" {small} />
{:else if small}
    <ul class="type-body flex flex-col gap-[0.6em]">
        {#each listed as h, i (`${i}:${h.title}`)}
            {@render story(h.title, h.feed_title)}
        {/each}
    </ul>
{:else if lead}
    <div class="flex h-full flex-col">
        <div class="min-h-0" style:flex-basis="{LEAD_SHARE * 100}%">
            <FitText min={32} max={size === 'l' ? 110 : 72} key={lead.title}>
                <p
                    class="font-(family-name:--display-family) leading-[1.1] font-medium tracking-[-0.01em] text-balance"
                >
                    {lead.title}
                </p>
                <p class="mt-[0.35em] text-[0.32em] font-semibold tracking-[0.16em] text-(--text-2) uppercase">
                    {lead.feed_title}
                </p>
            </FitText>
        </div>
        {#if listed.length > 0}
            <div class="my-6 h-[3px] shrink-0 bg-(--border)"></div>
            <ul class="type-body flex min-h-0 flex-col gap-[0.6em] overflow-hidden">
                {#each listed as h, i (`${i}:${h.title}`)}
                    {@render story(h.title, h.feed_title)}
                {/each}
            </ul>
        {/if}
    </div>
{/if}
