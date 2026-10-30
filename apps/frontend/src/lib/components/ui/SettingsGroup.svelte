<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        /** Small-caps heading; leave out for a group that is only the divider (e.g. an "Add" button). */
        title?: string;
        description?: string;
        class?: string;
        children: Snippet;
    }

    let { title, description, class: extra = '', children }: Props = $props();
</script>

<!--
@component
One group of related settings inside a settings card: a small-caps title, an optional
description, then its controls. Groups sit in a flex column and a hairline divides each
from the one above — the first in its column has none, so keep banners and other
non-groups outside that column. A control that shares a group with another gets a
`type-label` sub-label, not a group of its own.
-->
<section class="border-t border-(--border) pt-5 first:border-t-0 first:pt-0 {extra}">
    {#if title}<p class="eyebrow {description ? 'mb-1' : 'mb-3'}">{title}</p>{/if}
    {#if description}<p class="type-label mb-3 text-(--text-2)">{description}</p>{/if}
    {@render children()}
</section>
