<script lang="ts">
    import type { Snippet } from 'svelte';
    import { ChevronRight } from '@lucide/svelte';

    interface Props {
        open?: boolean;
        /** The row that's always shown — usually the current choice. */
        summary: Snippet;
        children: Snippet;
        class?: string;
    }

    let { open = $bindable(false), summary, children, class: extra = '' }: Props = $props();
</script>

<!--
@component
A fold: a summary row (usually the current choice) that opens to show the full set
of options, so long pickers and set-once settings stay out of the way until wanted.
A native <details>, so the keyboard and screen readers handle it for free.
-->
<details bind:open class="group/disclosure {extra}">
    <summary
        class="flex min-h-10 cursor-pointer list-none items-center gap-2 text-(--text-2) transition-colors hover:text-(--text-1) [&::-webkit-details-marker]:hidden"
    >
        <ChevronRight
            class="icon-sm shrink-0 transition-transform group-open/disclosure:rotate-90"
            aria-hidden="true"
        />
        <span class="flex min-w-0 flex-1 items-center gap-3">{@render summary()}</span>
    </summary>
    <div class="mt-3">{@render children()}</div>
</details>
