<script lang="ts">
    import type { Snippet } from 'svelte';

    interface Props {
        left: Snippet;
        right: Snippet;
    }

    let { left, right }: Props = $props();
</script>

<!--
@component
Two independent stacks of cards once the container is wide enough — each column only grows with its own
cards, so a short card never leaves a hole beside a tall one (as a row-aligned grid does). Narrower, the
left stack sits above the right.
-->
<div class="@container">
    <div class="flex flex-col gap-4 @4xl:flex-row @4xl:items-start">
        <div class="flex min-w-0 flex-col gap-4 @4xl:flex-1">{@render left()}</div>
        <div class="flex min-w-0 flex-col gap-4 @4xl:flex-1">{@render right()}</div>
    </div>
</div>
