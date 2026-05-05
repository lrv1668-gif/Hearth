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
        class="group flex w-full items-center justify-between border-b border-[var(--border)] pb-2"
    >
        {@render header()}
        {#if open}
            <ChevronUp class="icon-sm text-[var(--text-3)] transition-colors group-hover:text-[var(--text-1)]" />
        {:else}
            <ChevronDown class="icon-sm text-[var(--text-3)] transition-colors group-hover:text-[var(--text-1)]" />
        {/if}
    </button>

    {#if open}
        {@render children()}
    {/if}
</section>
