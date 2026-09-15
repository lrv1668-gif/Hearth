<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        /** Extra classes appended to the scrollable list container. */
        class?: string;
        children: Snippet;
    }

    let { class: className = '', children }: Props = $props();

    let listEl = $state<HTMLDivElement | null>(null);
    let atBottom = $state(false);

    function updateAtBottom() {
        if (!listEl) return;
        atBottom = listEl.scrollTop + listEl.clientHeight >= listEl.scrollHeight - 1;
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
</script>

<div class="relative">
    <div
        bind:this={listEl}
        class="scroll-thin flex max-h-[min(25vh,320px)] flex-col overflow-y-auto {className}"
        onscroll={updateAtBottom}
    >
        {@render children()}
    </div>
    {#if !atBottom}
        <div
            class="pointer-events-none absolute right-0 bottom-0 left-0 h-8"
            style="background: linear-gradient(to bottom, transparent, var(--bg))"
        ></div>
    {/if}
</div>
