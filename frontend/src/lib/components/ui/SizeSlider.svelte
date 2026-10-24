<script lang="ts">
    interface Props {
        /** The size as a multiplier — 1 shows as 100%. */
        value: number;
        min: number;
        max: number;
        step?: number;
        label?: string;
        oninput: (value: number) => void;
    }

    let { value, min, max, step = 0.05, label = 'Text size', oninput }: Props = $props();

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

    // Rescaling the page grows the content above the slider, which would push it away
    // from the pointer mid-drag — compensate the scroll position so it stays put.
    function handleInput(e: Event & { currentTarget: HTMLInputElement }) {
        const input = e.currentTarget;
        const before = input.getBoundingClientRect().top;
        oninput(Number(input.value));
        scrollParentOf(input).scrollTop += input.getBoundingClientRect().top - before;
    }
</script>

<!--
@component
A text-size range slider between a small and a large "A", with the size as a percent.
-->
<div class="flex items-center gap-4">
    <span aria-hidden="true" class="type-label text-(--text-3)">A</span>
    <input
        type="range"
        {min}
        {max}
        {step}
        {value}
        oninput={handleInput}
        aria-label={label}
        class="h-1 flex-1 cursor-pointer accent-(--accent)"
    />
    <span aria-hidden="true" class="type-subtitle text-(--text-3)">A</span>
    <span class="type-label w-12 text-right text-(--text-2) tabular-nums">
        {Math.round(value * 100)}%
    </span>
</div>
