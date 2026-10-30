<script lang="ts">
    import { dndzone, TRIGGERS, type DndEvent } from 'svelte-dnd-action';
    import { settleDrop, type Columns, type Side } from '$lib/widgetLayout';
    import { GripVertical, Grip, Lock, Plus, X } from '@lucide/svelte';
    import Chip from '../ui/Chip.svelte';
    import IconButton from '../ui/IconButton.svelte';

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
        /** Most widgets a column holds; dropping into a full column swaps with the
         *  widget it lands on (see settleDrop). */
        maxPerColumn?: number;
        /** Distinguishes this editor's drag zones from another editor's on the same page. */
        dragType: string;
        /** Every widget that can be shown or hidden; those not in a column wait in the Hidden tray. */
        widgets: readonly { id: string; label: string; description: string }[];
        /** Shows a hidden widget (in a column with room) or hides a placed one. */
        ontoggle: (id: string) => void;
    }

    let {
        columns,
        labelFor,
        onchange,
        width,
        onwidthchange,
        header,
        footer,
        maxPerColumn,
        dragType,
        widgets,
        ontoggle,
    }: Props = $props();

    const hidden = $derived(
        widgets
            .filter((w) => !columns.left.includes(w.id) && !columns.right.includes(w.id))
            .toSorted((a, b) => a.label.localeCompare(b.label))
    );
    // With a column limit, a widget can only be added while one column still has room.
    const full = $derived(
        maxPerColumn !== undefined && Math.min(columns.left.length, columns.right.length) >= maxPerColumn
    );

    const toItems = (ids: string[]): DragItem[] => ids.map((id) => ({ id, label: labelFor(id) }));

    let leftItems = $state<DragItem[]>([]);
    let rightItems = $state<DragItem[]>([]);

    // Re-sync when the columns change outside the editor (e.g. a widget shown or
    // hidden). During a drag only leftItems/rightItems change — `columns` stays
    // stable — so this doesn't fire mid-drag and disturb the dndzone state.
    $effect(() => {
        leftItems = toItems(columns.left);
        rightItems = toItems(columns.right);
    });

    // Where the dragged widget started, so a swap can send the displaced one there.
    let origin: { side: Side; index: number } | null = null;
    let settling = false;

    function consider(side: Side, e: CustomEvent<DndEvent<DragItem>>) {
        if (e.detail.info.trigger === TRIGGERS.DRAG_STARTED) {
            const before = side === 'left' ? leftItems : rightItems;
            origin = { side, index: before.findIndex((i) => i.id === e.detail.info.id) };
        }
        if (side === 'left') leftItems = e.detail.items;
        else rightItems = e.detail.items;
    }

    function finalize(side: Side, e: CustomEvent<DndEvent<DragItem>>) {
        if (side === 'left') leftItems = e.detail.items;
        else rightItems = e.detail.items;
        // A move between columns finalizes both zones; settle once, after both.
        if (settling) return;
        settling = true;
        const droppedId = e.detail.info.id;
        queueMicrotask(() => {
            settling = false;
            let next: Columns = { left: leftItems.map((i) => i.id), right: rightItems.map((i) => i.id) };
            if (maxPerColumn !== undefined && origin) next = settleDrop(next, maxPerColumn, droppedId, origin);
            origin = null;
            leftItems = toItems(next.left);
            rightItems = toItems(next.right);
            onchange(next);
        });
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
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

<!--
@component
Drag-and-drop two-column layout editor with a resizable column split and locked
header/footer strips — shared by the Dashboard and Frame settings.
-->
{#snippet strip(title: string, items: readonly { label: string }[])}
    <div
        class="flex flex-wrap items-center gap-2 rounded border border-dashed border-(--border) bg-(--surface) px-3 py-2"
    >
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
        {#if maxPerColumn !== undefined}<span class="text-(--text-2)"
                >· {items.length > maxPerColumn ? 'swap' : `${items.length}/${maxPerColumn}`}</span
            >{/if}
    </p>
    <div
        use:dndzone={{ items, type: dragType, flipDurationMs: 150 }}
        onconsider={(e) => consider(side, e)}
        onfinalize={(e) => finalize(side, e)}
        class="min-h-16 space-y-1"
    >
        {#each items as item (item.id)}
            <div class="flex cursor-grab items-center gap-2 py-0.5 select-none active:cursor-grabbing">
                <Grip class="icon-sm shrink-0 text-(--text-2)" />
                <span class="type-body min-w-0 flex-1 truncate text-(--text-1)">{item.label}</span>
                <IconButton label="Hide {item.label}" size="sm" onclick={() => ontoggle(item.id)}>
                    <X class="icon-sm" />
                </IconButton>
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
            class="flex cursor-col-resize items-center justify-center bg-(--border) text-(--text-2) transition-colors hover:bg-(--accent) hover:text-(--accent-fg)"
            onpointerdown={startResize}
        >
            <GripVertical class="icon-sm" />
        </div>

        <div class="flex-1 p-3">
            {@render column('Right column', 'right', rightItems)}
        </div>
    </div>

    {@render strip('Footer', footer)}

    <SettingsGroup title="Hidden" class="mt-5">
        {#if hidden.length === 0}
            <p class="type-label text-(--text-2)">Every widget is showing.</p>
        {:else}
            <div class="flex flex-wrap gap-2" role="group" aria-label="Hidden widgets">
                {#each hidden as w (w.id)}
                    <Chip
                        pressed={false}
                        disabled={full}
                        title={w.description}
                        aria-label="Show {w.label}"
                        onclick={() => ontoggle(w.id)}
                    >
                        <Plus class="icon-xs" aria-hidden="true" />
                        {w.label}
                    </Chip>
                {/each}
            </div>
            <p class="type-caption mt-2 text-(--text-2)">
                {full ? 'Both columns are full — hide a widget to add another.' : 'Tap one to add it.'}
            </p>
        {/if}
    </SettingsGroup>
</div>
