<script lang="ts">
    import type { Snippet } from 'svelte';
    import { untrack } from 'svelte';
    import { ChevronDown, ChevronUp } from '@lucide/svelte';

    interface Props {
        header: Snippet;
        children: Snippet;
        defaultOpen?: boolean;
    }

    let { header, children, defaultOpen }: Props = $props();
    let open = $state(untrack(() => defaultOpen));
</script>

<section class="space-y-5">
    <button
        onclick={() => (open = !open)}
        class="w-full flex items-center justify-between border-b border-[var(--border)] pb-2 group"
    >
        {@render header()}
        {#if open}
            <ChevronUp class="icon-sm text-[var(--text-3)] group-hover:text-[var(--text-1)] transition-colors" />
        {:else}
            <ChevronDown class="icon-sm text-[var(--text-3)] group-hover:text-[var(--text-1)] transition-colors" />
        {/if}
    </button>

    {#if open}
        {@render children()}
    {/if}
</section>
