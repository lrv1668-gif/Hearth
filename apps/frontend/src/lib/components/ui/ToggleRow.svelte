<script lang="ts">
    import type { Snippet } from 'svelte';
    import Toggle from './Toggle.svelte';

    interface Props {
        checked: boolean;
        onchange: () => void;
        title: string;
        description?: string;
        /** Rich description (links, code) in place of `description`. */
        children?: Snippet;
        disabled?: boolean;
    }

    let { checked, onchange, title, description, children, disabled = false }: Props = $props();

    const id = $props.id();
</script>

<!--
@component
A switch with its title and description, the whole row clickable. The switch is named
by the title and described by the description, so a screen reader announces both.
-->
<label class="flex items-center gap-3 {disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer'}">
    <Toggle {checked} {onchange} {disabled} labelledby="{id}-title" describedby="{id}-desc" />
    <span class="flex flex-col">
        <span id="{id}-title" class="type-body text-(--text-1) select-none">{title}</span>
        {#if children || description}
            <span id="{id}-desc" class="type-label text-(--text-2) select-none">
                {#if children}{@render children()}{:else}{description}{/if}
            </span>
        {/if}
    </span>
</label>
