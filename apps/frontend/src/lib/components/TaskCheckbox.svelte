<script lang="ts">
    import { Check } from '@lucide/svelte';

    interface Props {
        checked: boolean;
        /** The item's title — its accessible name, read out with the checked state. */
        title: string;
        ontoggle: (e: MouseEvent) => void;
        /** box: a ticked square (lists). dot: a small dot for the month grid and day lists. */
        variant?: 'box' | 'dot';
        /** A calendar-provider task — its open dot is the accent rather than grey. */
        external?: boolean;
    }

    let { checked, title, ontoggle, variant = 'box', external = false }: Props = $props();
</script>

<!--
@component
The done checkbox for a task or calendar task. A real checkbox to assistive tech
(role and checked state), drawn small to fit dense lists.
-->
{#if variant === 'box'}
    <button
        type="button"
        role="checkbox"
        aria-checked={checked}
        aria-label={title}
        onclick={ontoggle}
        class="flex h-4 w-4 shrink-0 items-center justify-center rounded border transition-colors
            {checked ? 'border-(--done-bg) bg-(--done-bg)' : 'border-(--text-3) hover:border-(--text-1)'}"
    >
        {#if checked}
            <Check class="icon-xs text-(--bg)" aria-hidden="true" />
        {/if}
    </button>
{:else}
    <!-- The visible dot is 8px; the ::after widens the tap target to 24px without moving the layout -->
    <button
        type="button"
        role="checkbox"
        aria-checked={checked}
        aria-label={title}
        onclick={ontoggle}
        class="relative h-2 w-2 shrink-0 rounded-full transition-colors after:absolute after:-inset-2 after:content-['']
            {checked
            ? 'bg-(--done-bg)'
            : external
              ? 'bg-(--accent) hover:opacity-80'
              : 'bg-(--text-3) hover:bg-(--text-1)'}"
    ></button>
{/if}
