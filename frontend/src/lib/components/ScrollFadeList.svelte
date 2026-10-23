<script lang="ts">
    import type { Snippet } from 'svelte';
    import { MediaQuery } from 'svelte/reactivity';

    interface Props {
        /** Extra classes appended to the scrollable list container. */
        class?: string;
        children: Snippet;
    }

    let { class: className = '', children }: Props = $props();

    // Wide screens scroll the list in place. Narrower ones (Tailwind's `lg`, where the dashboard
    // drops to one scrolling column) clip it with a "Show more" toggle instead, so a swipe down
    // the page never gets caught in a list scrolling inside it.
    const isWide = new MediaQuery('(min-width: 1024px)');
    let expanded = $state(false);

    let listEl = $state<HTMLDivElement | null>(null);
    let atBottom = $state(false);
    let overflowing = $state(false);

    function updateAtBottom() {
        if (!listEl) return;
        atBottom = listEl.scrollTop + listEl.clientHeight >= listEl.scrollHeight - 1;
        overflowing = listEl.scrollHeight > listEl.clientHeight + 1;
    }

    // Content is a dynamic, store-driven list of children (count and size unknown up front),
    // so a ResizeObserver on the container alone won't catch it overflowing/shrinking — a
    // MutationObserver on the subtree recomputes the fade whenever items are added or removed.
    $effect(() => {
        if (!listEl) return;
        const resizeObserver = new ResizeObserver(updateAtBottom);
        resizeObserver.observe(listEl);
        const mutationObserver = new MutationObserver(updateAtBottom);
        mutationObserver.observe(listEl, { childList: true, subtree: true, characterData: true });
        updateAtBottom();
        return () => {
            resizeObserver.disconnect();
            mutationObserver.disconnect();
        };
    });

    // Recheck once the clip changes (breakpoint crossed, or expanded/collapsed).
    $effect(() => {
        void isWide.current;
        void expanded;
        updateAtBottom();
    });
</script>

<div class="relative">
    <div
        bind:this={listEl}
        class="scroll-thin flex flex-col {isWide.current
            ? 'max-h-[min(25vh,320px)] overflow-y-auto'
            : expanded
              ? ''
              : 'max-h-56 overflow-hidden'} {className}"
        onscroll={updateAtBottom}
        onfocusin={() => {
            // Tabbing into a clipped item opens the list rather than scrolling it out of sight
            if (!isWide.current) expanded = true;
        }}
    >
        {@render children()}
    </div>
    {#if !atBottom && (isWide.current || !expanded)}
        <div
            class="pointer-events-none absolute right-0 left-0 h-8 {isWide.current || !overflowing
                ? 'bottom-0'
                : 'bottom-11'}"
            style="background: linear-gradient(to bottom, transparent, var(--bg))"
        ></div>
    {/if}
    {#if !isWide.current && (overflowing || expanded)}
        <button
            type="button"
            onclick={() => (expanded = !expanded)}
            aria-expanded={expanded}
            class="type-label mt-1 flex min-h-10 items-center font-medium text-(--text-2) transition-colors hover:text-(--text-1)"
        >
            {expanded ? 'Show less' : 'Show more'}
        </button>
    {/if}
</div>
