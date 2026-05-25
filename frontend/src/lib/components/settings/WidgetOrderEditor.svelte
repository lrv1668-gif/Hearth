<script lang="ts">
    import { dndzone } from 'svelte-dnd-action';
    import { Grip, GripVertical } from '@lucide/svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
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

    // Re-sync when widgetColumns is changed externally (e.g. from the visibility toggles).
    // During drag, only leftItems/rightItems change — settings.widgetColumns stays stable — so
    // this effect does not fire mid-drag and does not disturb the dndzone state.
    $effect(() => {
        leftItems = toItems(settings.widgetColumns.left);
        rightItems = toItems(settings.widgetColumns.right);
    });
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
        settings.widgetColumns = {
            left: leftItems.map((i: DragItem) => i.id),
            right: rightItems.map((i: DragItem) => i.id),
        };
    }

    function onfinalizeRight(e: CustomEvent) {
        rightItems = e.detail.items;
        settings.widgetColumns = {
            left: leftItems.map((i: DragItem) => i.id),
            right: rightItems.map((i: DragItem) => i.id),
        };
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
        settings.leftColumnWidth = Math.round(previewWidth);
    }
</script>

<div
    bind:this={containerEl}
    role="presentation"
    class="mb-4 flex overflow-hidden rounded border-2 border-[var(--border)]"
    class:cursor-col-resize={dragging}
    onpointermove={onDrag}
    onpointerup={endDrag}
>
    <div class="shrink-0 p-3" style="width: {previewWidth}%">
        <p class="type-label mb-2 text-[var(--text-1)]">Left column</p>
        <div
            use:dndzone={{ items: leftItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderLeft}
            onfinalize={onfinalizeLeft}
            class="min-h-16 space-y-1"
        >
            {#each leftItems as item (item.id)}
                <div class="flex cursor-grab select-none items-center gap-2 py-1.5 active:cursor-grabbing">
                    <Grip class="icon-sm shrink-0 text-[var(--text-2)]" />
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
        class="flex cursor-col-resize items-center justify-center bg-[var(--accent)] text-[var(--text-4)] transition-colors"
        onpointerdown={startDrag}
    >
        <GripVertical class="icon-sm" />
    </div>

    <div class="flex-1 p-3">
        <p class="type-label mb-2 text-[var(--text-1)]">Right column</p>
        <div
            use:dndzone={{ items: rightItems, type: 'widget', flipDurationMs: 150 }}
            onconsider={onconsiderRight}
            onfinalize={onfinalizeRight}
            class="min-h-16 space-y-1"
        >
            {#each rightItems as item (item.id)}
                <div class="flex cursor-grab select-none items-center gap-2 py-1.5 active:cursor-grabbing">
                    <Grip class="icon-sm shrink-0 text-[var(--text-2)]" />
                    <span class="type-body text-[var(--text-1)]">{item.label}</span>
                </div>
            {/each}
        </div>
    </div>
</div>
