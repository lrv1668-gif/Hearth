<script lang="ts">
    import type { Snippet } from 'svelte';
    import { fitFontSize, getFrameScale } from '#lib/frame.ts';

    interface Props {
        /** Size bounds in px at the 1600×1200 reference panel (scaled with the frame). */
        min: number;
        max: number;
        /** Re-fit when this changes — the text being fitted. */
        key: unknown;
        class?: string;
        children: Snippet;
    }

    let { min, max, key, class: extra = '', children }: Props = $props();

    const scale = getFrameScale();
    let box = $state<HTMLElement>();
    let content = $state<HTMLElement>();
    let size = $state(0);

    function fit() {
        if (!box || !content) return;
        const el = content;
        const area = box;
        size = fitFontSize(
            (px) => {
                el.style.fontSize = `${px}px`;
                return el.scrollHeight <= area.clientHeight && el.scrollWidth <= area.clientWidth;
            },
            min * scale(),
            max * scale()
        );
        el.style.fontSize = `${size}px`;
    }

    // Refit when the text changes, once web fonts have loaded, and when the box resizes
    // (the Settings preview swaps the font preset in place).
    $effect(() => {
        void key;
        fit();
        document.fonts.ready.then(fit);
        const observer = new ResizeObserver(fit);
        if (box) observer.observe(box);
        return () => observer.disconnect();
    });
</script>

<!--
@component
Text sized to fill its box: the largest size in [min, max] at which the children fit,
found by measuring (fitFontSize), as Note mode does for the whole panel. The box takes
its parent's full size, so give the parent a definite height (a template slot's body).
Children size in em off it.
-->
<div bind:this={box} class="flex h-full w-full min-w-0 items-center overflow-hidden {extra}">
    <div bind:this={content} class="w-full" style:font-size={size ? `${size}px` : undefined}>
        {@render children()}
    </div>
</div>
