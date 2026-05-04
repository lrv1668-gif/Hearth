<script lang="ts">
    import type { Snippet } from 'svelte';
    import { untrack } from 'svelte';
    import { ChevronDown, ChevronUp } from '@lucide/svelte';

    interface Props {
        title: string;
        description?: string;
        children: Snippet;
        defaultOpen?: boolean;
    }

    let { title, children, description = '', defaultOpen = false }: Props = $props();
    let open = $state(untrack(() => defaultOpen));
</script>

<section class="space-y-5">
    <button
        onclick={() => (open = !open)}
        class="w-full flex items-center justify-between border-b border-[var(--border)] pb-2 group"
    >
        <h2 class="type-subtitle tracking-widest uppercase text-[var(--text-1)]">{title}</h2>
        {#if open}
            <ChevronUp class="icon-sm text-[var(--text-3)] group-hover:text-[var(--text-1)] transition-colors" />
        {:else}
            <ChevronDown class="icon-sm text-[var(--text-3)] group-hover:text-[var(--text-1)] transition-colors" />
        {/if}
    </button>

    {#if open}
        {#if description}
            <p class="type-body text-[var(--text-1)]">{description}</p>
        {/if}

        {@render children()}
    {/if}
</section>
