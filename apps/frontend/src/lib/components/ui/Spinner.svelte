<script lang="ts">
    import { LoaderCircle } from '@lucide/svelte';

    let { class: extra = 'icon-xs' }: { class?: string } = $props();
</script>

<!--
@component
A small spinner for a short in-place wait: a status line, a busy button, a sub-list.
Decorative — put the words beside it ("Checking…") so that's what's announced.
Takes the surrounding text colour, so it reads on a filled button too. Stops spinning
under prefers-reduced-motion. Size it with an `icon-*` class.
-->
<LoaderCircle class="shrink-0 motion-safe:animate-spin {extra}" aria-hidden="true" />
