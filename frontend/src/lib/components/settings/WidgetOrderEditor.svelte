<script lang="ts">
    import { dndzone } from 'svelte-dnd-action';
    import { GripVertical } from '@lucide/svelte';
    import { settings, reorderWidgets } from '$lib/stores/SettingsStore.svelte.ts';
    import { allWidgets, type AllWidgetId } from '$lib/constants/widgets';

    type DragItem = { id: AllWidgetId; label: string };

    function toItems(order: AllWidgetId[]): DragItem[] {
        return order.map((id) => ({
            id,
            label: allWidgets.find((w) => w.id === id)?.label ?? id,
        }));
    }

    let items = $state(toItems(settings.widgetOrder));

    function onconsider(e: CustomEvent) {
        items = e.detail.items;
    }

    function onfinalize(e: CustomEvent) {
        items = e.detail.items;
        reorderWidgets(items.map((i: DragItem) => i.id));
    }
</script>

<div
    use:dndzone={{ items, flipDurationMs: 150 }}
    {onconsider}
    {onfinalize}
    class="space-y-1"
>
    {#each items as item (item.id)}
        <div class="flex items-center gap-2 py-1.5 cursor-grab active:cursor-grabbing select-none">
            <GripVertical class="icon-sm text-[var(--text-3)] shrink-0" />
            <span class="type-body text-[var(--text-1)]">{item.label}</span>
        </div>
    {/each}
</div>
