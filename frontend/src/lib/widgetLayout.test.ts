import { describe, expect, it } from 'vitest';
import { settleDrop } from './widgetLayout';

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
