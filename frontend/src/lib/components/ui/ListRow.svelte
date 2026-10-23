<script lang="ts">
    import type { Snippet } from 'svelte';
    import { Trash2 } from '@lucide/svelte';
    import IconButton from './IconButton.svelte';

    interface Props {
        title: string;
        subtitle?: string;
        /** Shows a remove button, revealed on hover (always on touch). */
        onremove?: () => void;
        removeLabel?: string;
        /** Extra lines under the subtitle. */
        meta?: Snippet;
        /** Buttons before the remove button. */
        actions?: Snippet;
    }

    let { title, subtitle, onremove, removeLabel = `Remove ${title}`, meta, actions }: Props = $props();
</script>

<!--
@component
A row in a settings list — a title, a truncated subtitle, and its own actions.
-->
<div class="group flex items-center gap-1 rounded-lg py-1 pr-1 pl-3 hover:bg-(--surface-hi)">
    <div class="min-w-0 flex-1 py-1">
        <p class="type-body font-medium text-(--text-1)">{title}</p>
        {#if subtitle}<p class="type-label truncate text-(--text-2)">{subtitle}</p>{/if}
        {@render meta?.()}
    </div>
    {@render actions?.()}
    {#if onremove}
        <IconButton label={removeLabel} reveal onclick={onremove}>
            <Trash2 class="icon-sm" />
        </IconButton>
    {/if}
</div>
