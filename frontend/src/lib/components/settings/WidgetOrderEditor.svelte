<script lang="ts">
    import { dndzone } from 'svelte-dnd-action';
    import { GripVertical } from '@lucide/svelte';
    import { settings, reorderWidgetColumns, updateColumnWidth } from '$lib/stores/SettingsStore.svelte.ts';
    import { allWidgets, type AllWidgetId } from '$lib/constants/widgets';

    type DragItem = { id: AllWidgetId; label: string };

    function toItems(order: AllWidgetId[]): DragItem[] {
        return order.map((id) => ({
            id,
            label: allWidgets.find((w) => w.id === id)?.label ?? id,
        }));
    }

    let leftItems = $state(toItems(settings.widgetColumns.left));
    let rightItems = $state(toItems(settings.widgetColumns.right));
    let previewWidth = $state(settings.leftColumnWidth);

    function onconsiderLeft(e: CustomEvent) {
        leftItems = e.detail.items;
    }

    function onconsiderRight(e: CustomEvent) {
        rightItems = e.detail.items;
    }

    function onfinalizeLeft(e: CustomEvent) {
        leftItems = e.detail.items;
        reorderWidgetColumns({
            left: leftItems.map((i: DragItem) => i.id),
            right: rightItems.map((i: DragItem) => i.id),
        });
    }

    function onfinalizeRight(e: CustomEvent) {
        rightItems = e.detail.items;
        reorderWidgetColumns({
            left: leftItems.map((i: DragItem) => i.id),
            right: rightItems.map((i: DragItem) => i.id),
        });
    }
</script>

<div class="flex border-2 border-[var(--border)] rounded overflow-hidden mb-4">
    <div class="p-3 border-r-2 border-r-[var(--border)] shrink-0" style="width: {previewWidth}%">
        <p class="type-label text-[var(--text-2)] mb-2">Left column</p>
        <div
            use:dndzone={{ items: leftItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderLeft}
            onfinalize={onfinalizeLeft}
            class="space-y-1 min-h-16"
        >
            {#each leftItems as item (item.id)}
                <div class="flex items-center gap-2 py-1.5 cursor-grab active:cursor-grabbing select-none">
                    <GripVertical class="icon-sm text-[var(--text-2)] shrink-0" />
                    <span class="type-body text-[var(--text-1)]">{item.label}</span>
                </div>
            {/each}
        </div>
    </div>

    <div class="p-3 w-auto flex-1">
        <p class="type-label text-[var(--text-2)] mb-2">Right column</p>
        <div
            use:dndzone={{ items: rightItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderRight}
            onfinalize={onfinalizeRight}
            class="space-y-1 min-h-16"
        >
            {#each rightItems as item (item.id)}
                <div class="flex items-center gap-2 py-1.5 cursor-grab active:cursor-grabbing select-none">
                    <GripVertical class="icon-sm text-[var(--text-2)] shrink-0" />
                    <span class="type-body text-[var(--text-1)]">{item.label}</span>
                </div>
            {/each}
        </div>
    </div>
</div>

<div>
    <p class="type-body tracking-widest uppercase text-[var(--text-1)]">Column Split</p>
    <p class="type-label text-[var(--text-2)] mb-4">Set how wide the left column is relative to the right on desktop</p>
    <div class="flex items-center gap-3">
        <span class="type-label text-[var(--text-2)] w-8 text-right shrink-0">30%</span>
        <input
            type="range"
            min="30"
            max="70"
            step="5"
            value={settings.leftColumnWidth}
            oninput={(e) => (previewWidth = Number(e.currentTarget.value))}
            onchange={(e) => updateColumnWidth(Number(e.currentTarget.value))}
            class="flex-1 accent-[var(--accent)]"
        />
        <span class="type-label text-[var(--text-2)] w-8 shrink-0">70%</span>
        <span class="type-label text-[var(--text-1)] w-10 text-right shrink-0">
            {previewWidth}%
        </span>
    </div>
</div>
