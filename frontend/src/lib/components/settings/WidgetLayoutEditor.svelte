<script lang="ts">
    import { dndzone } from 'svelte-dnd-action';
    import { GripVertical, Grip, Lock } from '@lucide/svelte';

    type Columns = { left: string[]; right: string[] };
    type DragItem = { id: string; label: string };

    interface Props {
        columns: Columns;
        labelFor: (id: string) => string;
        onchange: (columns: Columns) => void;
        /** Left column's width, in percent (30–70). */
        width: number;
        onwidthchange: (width: number) => void;
        header: readonly { label: string }[];
        footer: readonly { label: string }[];
        /** Most widgets a column accepts; a full column refuses drops from the other. */
        maxPerColumn?: number;
        /** Distinguishes this editor's drag zones from another editor's on the same page. */
        dragType: string;
    }

    let { columns, labelFor, onchange, width, onwidthchange, header, footer, maxPerColumn, dragType }: Props = $props();

    const toItems = (ids: string[]): DragItem[] => ids.map((id) => ({ id, label: labelFor(id) }));

    let leftItems = $state<DragItem[]>([]);
    let rightItems = $state<DragItem[]>([]);

    // Re-sync when the columns change outside the editor (e.g. from the visibility
    // toggles). During a drag only leftItems/rightItems change — `columns` stays
    // stable — so this doesn't fire mid-drag and disturb the dndzone state.
    $effect(() => {
        leftItems = toItems(columns.left);
        rightItems = toItems(columns.right);
    });

    const full = (items: DragItem[]) => maxPerColumn !== undefined && items.length >= maxPerColumn;

    function finalize(side: 'left' | 'right', items: DragItem[]) {
        if (side === 'left') leftItems = items;
        else rightItems = items;
        onchange({ left: leftItems.map((i) => i.id), right: rightItems.map((i) => i.id) });
    }

    let previewWidth = $state(0);
    $effect(() => {
        previewWidth = width;
    });
    let resizing = $state(false);
    let dragStartX = 0;
    let dragStartWidth = 0;
    let containerEl: HTMLElement;

    function startResize(e: PointerEvent) {
        resizing = true;
        dragStartX = e.clientX;
        dragStartWidth = previewWidth;
        (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    }

    function onResize(e: PointerEvent) {
        if (!resizing) return;
        const containerWidth = containerEl.getBoundingClientRect().width;
        const deltaPct = ((e.clientX - dragStartX) / containerWidth) * 100;
        previewWidth = Math.min(70, Math.max(30, dragStartWidth + deltaPct));
    }

    function endResize() {
        if (!resizing) return;
        resizing = false;
        onwidthchange(Math.round(previewWidth));
    }
</script>

<!--
@component
Drag-and-drop two-column layout editor with a resizable column split and locked
header/footer strips — shared by the Dashboard and Frame settings.
-->
{#snippet strip(title: string, items: readonly { label: string }[])}
    <div class="flex items-center gap-2 rounded border border-dashed border-(--border) bg-(--surface) px-3 py-2">
        <Lock class="icon-xs shrink-0 text-(--text-3)" />
        <span class="type-label text-(--text-2)">{title} —</span>
        {#each items as w (w.label)}
            <span class="type-label rounded bg-(--surface-hi) px-2 py-0.5 text-(--text-2)">{w.label}</span>
        {/each}
    </div>
{/snippet}

{#snippet column(title: string, side: 'left' | 'right', items: DragItem[])}
    <p class="type-label mb-2 text-(--text-1)">
        {title}
        {#if maxPerColumn !== undefined}<span class="text-(--text-2)">· {items.length}/{maxPerColumn}</span>{/if}
    </p>
    <div
        use:dndzone={{ items, type: dragType, flipDurationMs: 150, dropFromOthersDisabled: full(items) }}
        onconsider={(e) => (side === 'left' ? (leftItems = e.detail.items) : (rightItems = e.detail.items))}
        onfinalize={(e) => finalize(side, e.detail.items)}
        class="min-h-16 space-y-1"
    >
        {#each items as item (item.id)}
            <div class="flex cursor-grab items-center gap-2 py-1.5 select-none active:cursor-grabbing">
                <Grip class="icon-sm shrink-0 text-(--text-2)" />
                <span class="type-body text-(--text-1)">{item.label}</span>
            </div>
        {/each}
    </div>
{/snippet}

<div
    bind:this={containerEl}
    role="presentation"
    class:cursor-col-resize={resizing}
    onpointermove={onResize}
    onpointerup={endResize}
>
    <div class="mb-1">{@render strip('Header', header)}</div>

    <div class="mb-1 flex overflow-hidden rounded border-2 border-(--border)">
        <div class="shrink-0 p-3" style="width: {previewWidth}%">
            {@render column('Left column', 'left', leftItems)}
        </div>

        <div
            role="separator"
            aria-valuenow={Math.round(previewWidth)}
            aria-valuemin={30}
            aria-valuemax={70}
            class="flex cursor-col-resize items-center justify-center bg-(--accent) text-(--accent-fg) transition-colors"
            onpointerdown={startResize}
        >
            <GripVertical class="icon-sm" />
        </div>

        <div class="flex-1 p-3">
            {@render column('Right column', 'right', rightItems)}
        </div>
    </div>

    {@render strip('Footer', footer)}
</div>
