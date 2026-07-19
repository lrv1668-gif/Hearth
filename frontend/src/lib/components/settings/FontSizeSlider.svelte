<script lang="ts">
    import { fontSizeStore, setFontSize, MIN_FONT_SCALE, MAX_FONT_SCALE } from '$lib/stores/FontSizeStore.svelte.ts';

    function scrollParentOf(el: HTMLElement): Element {
        let node = el.parentElement;
        while (node) {
            const { overflowY } = getComputedStyle(node);
            if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight) {
                return node;
            }
            node = node.parentElement;
        }
        return document.scrollingElement ?? document.documentElement;
    }

    // Rescaling grows the content above the slider, which would push it away from
    // the pointer mid-drag — compensate the scroll position so it stays put.
    function handleInput(e: Event & { currentTarget: HTMLInputElement }) {
        const input = e.currentTarget;
        const before = input.getBoundingClientRect().top;
        setFontSize(Number(input.value));
        scrollParentOf(input).scrollTop += input.getBoundingClientRect().top - before;
    }
</script>

<div class="flex items-center gap-4">
    <span aria-hidden="true" class="type-label text-[var(--text-3)]">A</span>
    <input
        type="range"
        min={MIN_FONT_SCALE}
        max={MAX_FONT_SCALE}
        step="0.05"
        value={fontSizeStore.scale}
        oninput={handleInput}
        aria-label="Text size"
        class="h-1 flex-1 cursor-pointer accent-[var(--accent)]"
    />
    <span aria-hidden="true" class="type-subtitle text-[var(--text-3)]">A</span>
    <span class="type-label w-12 text-right tabular-nums text-[var(--text-2)]">
        {Math.round(fontSizeStore.scale * 100)}%
    </span>
</div>
