<script lang="ts">
    import { dndzone } from 'svelte-dnd-action';
    import { Grip, GripVertical } from '@lucide/svelte';
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
    let dragging = $state(false);
    let dragStartX = 0;
    let dragStartWidth = 0;
    let containerEl: HTMLElement;

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

    function startDrag(e: PointerEvent) {
        dragging = true;
        dragStartX = e.clientX;
        dragStartWidth = previewWidth;
        (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    }

    function onDrag(e: PointerEvent) {
        if (!dragging) return;
        const containerWidth = containerEl.getBoundingClientRect().width;
        const deltaPct = ((e.clientX - dragStartX) / containerWidth) * 100;
        previewWidth = Math.min(70, Math.max(30, dragStartWidth + deltaPct));
    }

    function endDrag() {
        if (!dragging) return;
        dragging = false;
        updateColumnWidth(Math.round(previewWidth));
    }
</script>

<div
    bind:this={containerEl}
    role="presentation"
    class="flex border-2 border-[var(--border)] rounded overflow-hidden mb-4"
    class:cursor-col-resize={dragging}
    onpointermove={onDrag}
    onpointerup={endDrag}
>
    <div class="p-3 shrink-0" style="width: {previewWidth}%">
        <p class="type-label text-[var(--text-1)] mb-2">Left column</p>
        <div
            use:dndzone={{ items: leftItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderLeft}
            onfinalize={onfinalizeLeft}
            class="space-y-1 min-h-16"
        >
            {#each leftItems as item (item.id)}
                <div class="flex items-center gap-2 py-1.5 cursor-grab active:cursor-grabbing select-none">
                    <Grip class="icon-sm text-[var(--text-2)] shrink-0" />
                    <span class="type-body text-[var(--text-1)]">{item.label}</span>
                </div>
            {/each}
        </div>
    </div>

    <div
        role="separator"
        aria-valuenow={Math.round(previewWidth)}
        aria-valuemin={30}
        aria-valuemax={70}
        class="flex items-center justify-center bg-[var(--accent)] cursor-col-resize text-[var(--text-4)] transition-colors"
        onpointerdown={startDrag}
    >
        <GripVertical class="icon-sm" />
    </div>

    <div class="p-3 flex-1">
        <p class="type-label text-[var(--text-1)] mb-2">Right column</p>
        <div
            use:dndzone={{ items: rightItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderRight}
            onfinalize={onfinalizeRight}
            class="space-y-1 min-h-16"
        >
            {#each rightItems as item (item.id)}
                <div class="flex items-center gap-2 py-1.5 cursor-grab active:cursor-grabbing select-none">
                    <Grip class="icon-sm text-[var(--text-2)] shrink-0" />
                    <span class="type-body text-[var(--text-1)]">{item.label}</span>
                </div>
            {/each}
        </div>
    </div>
</div>
