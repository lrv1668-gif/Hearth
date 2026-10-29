<script lang="ts">
    import type { Snippet } from 'svelte';
    import type { HTMLButtonAttributes } from 'svelte/elements';
    import { buttonClass, type ButtonSize, type ButtonVariant } from './button';

    interface Props extends HTMLButtonAttributes {
        variant?: ButtonVariant;
        size?: ButtonSize;
        /** Renders an <a> styled as a button. */
        href?: string;
        target?: string;
        rel?: string;
        children: Snippet;
    }

    let {
        variant = 'secondary',
        size = 'md',
        href,
        target,
        rel,
        type = 'button',
        class: extra = '',
        children,
        ...rest
    }: Props = $props();

    const cls = $derived(`${buttonClass(variant, size)} ${extra}`);
</script>

<!--
@component
The app's button: `primary` (accent fill), `secondary` (outlined pill, the default),
`link` (accent text) or `ghost` (quiet text). Pass `href` for a link that looks like one.
Defaults to type="button", so it never submits a surrounding form by accident.
-->
{#if href}
    <a {href} {target} {rel} class={cls} {...rest as Record<string, unknown>}>{@render children()}</a>
{:else}
    <button {type} class={cls} {...rest}>{@render children()}</button>
{/if}
