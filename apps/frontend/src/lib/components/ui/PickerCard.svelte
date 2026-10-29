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
A large option card in a preview grid (the typeface and frame theme pickers): ringed in
the accent while chosen, slightly faded otherwise.
-->
<button
    {type}
    aria-pressed={pressed}
    class="flex flex-col overflow-hidden rounded-xl border-2 bg-(--surface) text-left outline-offset-2 transition-all
        {pressed ? 'border-(--accent) outline-2 outline-(--accent)' : 'border-(--border) opacity-85 hover:opacity-100'}
        {extra}"
    {...rest}
>
    {@render children()}
</button>
