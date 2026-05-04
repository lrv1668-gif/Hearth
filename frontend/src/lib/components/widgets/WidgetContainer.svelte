<script lang="ts">
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import type { WidgetId } from '$lib/constants/widgets';
    import type { Snippet } from 'svelte';

    interface Props {
        children: Snippet;
        title: String;
        associatedWidgetId?: WidgetId;
        align?: 'left' | 'right';
    }

    let { children, title, associatedWidgetId, align = 'left' }: Props = $props();
</script>

<!--
@component
This is a widget container Component, used to display widgets on the Schedule page with shared styling. 

It contents a title, children content, and an associated widget ID to be rendered on the Schedule page.

NOTE:If no widget ID is passed, then this widget will default to being displayed.
-->
{#if !associatedWidgetId || settings.enabledWidgets.includes(associatedWidgetId)}
    <div class="min-w-0">
        <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] mb-4 pb-2 {align === 'right' ? 'text-right' : ''}">{title}</h2>
        {@render children()}
    </div>
{/if}
