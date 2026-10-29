/** Two-column widget layout helpers shared by the layout editors. */

export type Columns = { left: string[]; right: string[] };
export type Side = keyof Columns;

/**
 * Settles a drop into a capped column. If the drop pushed a column past `max`, the
 * widget it landed on (the one now just below it, or above it at the end) moves to
 * where the dragged widget came from in the other column — so dragging between two
 * full columns swaps the pair instead of being refused.
 */
export function settleDrop(
    columns: Columns,
    max: number,
    droppedId: string,
    origin: { side: Side; index: number }
): Columns {
    const target: Side = columns.left.includes(droppedId) ? 'left' : 'right';
    const other: Side = target === 'left' ? 'right' : 'left';
    if (columns[target].length <= max || origin.side === target) return columns;

    const items = [...columns[target]];
    const at = items.indexOf(droppedId);
    const displacedAt = at + 1 < items.length ? at + 1 : at - 1;
    const [displaced] = items.splice(displacedAt, 1);

    const rest = [...columns[other]];
    rest.splice(Math.min(origin.index, rest.length), 0, displaced);
    return { [target]: items, [other]: rest } as Columns;
}

/**
 * Brings a saved layout up to date with widgets released since it was saved: a default
 * widget missing from `known` (the ids that existed when it was saved) joins its default
 * column, and the enabled list when it's on by default. Widgets in `known` stay as saved,
 * so the ones the user turned off stay off.
 */
export function mergeNewWidgets<C extends string, E extends C>(
    saved: { columns: { left: C[]; right: C[] }; enabled: E[] },
    known: readonly string[],
    defaults: { columns: { left: C[]; right: C[] }; enabled: readonly E[] }
): { columns: { left: C[]; right: C[] }; enabled: E[] } {
    const placed = new Set([...saved.columns.left, ...saved.columns.right]);
    const added = (ids: readonly C[]) => ids.filter((id) => !known.includes(id) && !placed.has(id));
    return {
        columns: {
            left: [...saved.columns.left, ...added(defaults.columns.left)],
            right: [...saved.columns.right, ...added(defaults.columns.right)],
        },
        enabled: [
            ...saved.enabled,
            ...defaults.enabled.filter((id) => !known.includes(id) && !saved.enabled.includes(id)),
        ],
    };
}
