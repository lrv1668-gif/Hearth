import { describe, expect, it } from 'vitest';
import { mergeNewWidgets, settleDrop } from './widgetLayout';

describe('mergeNewWidgets', () => {
    const defaults = { columns: { left: ['a', 'tasks'], right: ['b', 'c'] }, enabled: ['a', 'b', 'c'] };

    it('keeps a widget the user turned off off', () => {
        // "b" was known when saved, and the user removed it
        const saved = { columns: { left: ['a', 'tasks'], right: ['c'] }, enabled: ['a', 'c'] };
        expect(mergeNewWidgets(saved, ['a', 'tasks', 'b', 'c'], defaults)).toEqual(saved);
    });

    it('adds a widget released after the snapshot to its default column, enabled', () => {
        const saved = { columns: { left: ['tasks', 'a'], right: ['b'] }, enabled: ['a', 'b'] };
        expect(mergeNewWidgets(saved, ['a', 'tasks', 'b'], defaults)).toEqual({
            columns: { left: ['tasks', 'a'], right: ['b', 'c'] },
            enabled: ['a', 'b', 'c'],
        });
    });

    it("doesn't duplicate a new widget the snapshot already placed", () => {
        const saved = { columns: { left: ['c', 'a', 'tasks'], right: ['b'] }, enabled: ['a', 'b', 'c'] };
        expect(mergeNewWidgets(saved, ['a', 'tasks', 'b'], defaults)).toEqual(saved);
    });
});

describe('settleDrop', () => {
    it('swaps with the widget it landed on when both columns are full', () => {
        // "b" dragged from left (index 1) onto "y" at the top of the right column.
        const columns = { left: ['a', 'c'], right: ['x', 'b', 'y', 'z'] };
        expect(settleDrop(columns, 3, 'b', { side: 'left', index: 1 })).toEqual({
            left: ['a', 'y', 'c'],
            right: ['x', 'b', 'z'],
        });
    });

    it('swaps with the widget above when dropped at the end of the column', () => {
        const columns = { left: ['x', 'y', 'z', 'b'], right: ['a', 'c'] };
        expect(settleDrop(columns, 3, 'b', { side: 'right', index: 0 })).toEqual({
            left: ['x', 'y', 'b'],
            right: ['z', 'a', 'c'],
        });
    });

    it('leaves a drop alone when the column still has room', () => {
        const columns = { left: ['a'], right: ['x', 'b', 'y'] };
        expect(settleDrop(columns, 3, 'b', { side: 'left', index: 1 })).toBe(columns);
    });

    it('leaves a reorder within one column alone', () => {
        const columns = { left: ['a', 'b', 'c'], right: ['x', 'y', 'z'] };
        expect(settleDrop(columns, 3, 'b', { side: 'left', index: 0 })).toBe(columns);
    });
});
