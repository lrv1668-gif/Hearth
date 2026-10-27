<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { HTMLButtonAttributes } from 'svelte/elements';

    interface Props extends HTMLButtonAttributes {
        pressed: boolean;
        children: Snippet;
    }

    let { pressed, type = 'button', class: extra = '', children, ...rest }: Props = $props();
</script>

<!--
@component
One option in a set of choices: an outlined pill, accent-filled while chosen. Use
ChoiceChips for a pick-one row; reach for Chip directly only when each option needs
its own attributes (e.g. the typeface chips' data-font).
-->
<button
    {type}
    aria-pressed={pressed}
    class="type-label inline-flex min-h-10 items-center gap-1.5 rounded-full border px-4 transition-colors disabled:pointer-events-none disabled:opacity-50
        {pressed
        ? 'border-(--accent) bg-(--accent) font-semibold text-(--accent-fg)'
        : 'border-(--border) text-(--text-2) hover:border-(--text-2) hover:text-(--text-1)'}
        {extra}"
    {...rest}
>
    {@render children()}
</button>
