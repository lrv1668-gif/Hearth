<script lang="ts">
    import type { Component, Snippet } from 'svelte';
    import InlineMessage from './InlineMessage.svelte';

    interface Props {
        icon: Component<{ class?: string }>;
        name: string;
        /** One line on what the connection is doing. */
        status: string;
        error?: string | null;
        /** Connect / disconnect buttons. */
        actions: Snippet;
    }

    let { icon: Icon, name, status, error, actions }: Props = $props();
</script>

<!--
@component
An outside service Hearth reads from: its icon, name and status, with its buttons.
-->
<div class="flex items-center gap-4">
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-(--surface-hi)">
        <Icon class="icon-md text-(--text-2)" />
    </div>
    <div class="flex min-w-0 flex-1 flex-wrap items-center justify-between gap-x-4 gap-y-2">
        <div class="min-w-0">
            <p class="type-body font-medium text-(--text-1)">{name}</p>
            <p class="type-label text-(--text-2)">{status}</p>
            {#if error}<InlineMessage tone="error" class="mt-1">{error}</InlineMessage>{/if}
        </div>
        <div class="flex flex-wrap items-center gap-2">{@render actions()}</div>
    </div>
</div>
