<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { HTMLButtonAttributes } from 'svelte/elements';

    interface Props extends HTMLButtonAttributes {
        /** The accessible name — required, since the button shows only an icon. */
        label: string;
        /** sm is 32px, md (default) a 40px tap target. */
        size?: 'sm' | 'md';
        /** Hidden until its row (a `group`) is hovered or it has focus; always shown on touch. */
        reveal?: boolean;
        children: Snippet;
    }

    let { label, size = 'md', reveal = false, type = 'button', class: extra = '', children, ...rest }: Props = $props();
</script>

<button
    {type}
    aria-label={label}
    class="inline-flex shrink-0 items-center justify-center rounded-lg text-(--text-2) transition hover:text-(--text-1) disabled:pointer-events-none disabled:opacity-50
        {size === 'sm' ? 'h-8 w-8' : 'h-10 w-10'}
        {reveal ? 'opacity-0 group-hover:opacity-100 focus-visible:opacity-100 pointer-coarse:opacity-100' : ''}
        {extra}"
    {...rest}
>
    {@render children()}
</button>
