import { describe, expect, it } from 'vitest';
import { DEFAULT_FRAME_LAYOUT, normalizeFrameLayout, parseFrameLayout } from './frameWidgets';

describe('parseFrameLayout', () => {
    it('falls back to the default layout when no params are given', () => {
        expect(parseFrameLayout(new URLSearchParams())).toEqual({ ...DEFAULT_FRAME_LAYOUT, split: 50 });
    });

    it('reads comma-separated columns and keeps an explicitly empty column', () => {
        expect(parseFrameLayout(new URLSearchParams('left=week,moon&right=&split=60'))).toEqual({
            left: ['week', 'moon'],
            right: [],
            split: 60,
        });
    });

    it('falls back per param to the given layout, e.g. saved settings', () => {
        const saved = { left: ['moon' as const], right: ['quote' as const], split: 40 };
        expect(parseFrameLayout(new URLSearchParams(), saved)).toEqual(saved);
        expect(parseFrameLayout(new URLSearchParams('left=week'), saved)).toEqual({
            left: ['week'],
            right: ['quote'],
            split: 40,
        });
    });

    it('clamps the split to the 30–70 range and ignores junk', () => {
        expect(parseFrameLayout(new URLSearchParams('split=95')).split).toBe(70);
        expect(parseFrameLayout(new URLSearchParams('split=abc')).split).toBe(50);
    });
});

describe('normalizeFrameLayout', () => {
    it('drops unknown ids and duplicates, with the left column winning', () => {
        expect(normalizeFrameLayout({ left: ['trains', 'moon'], right: ['moon', 'quote'] })).toEqual({
            left: ['moon'],
            right: ['quote'],
        });
    });

    it('caps each column at three widgets', () => {
        expect(normalizeFrameLayout({ left: ['arc', 'today', 'week', 'moon'], right: [] }).left).toEqual([
            'arc',
            'today',
            'week',
        ]);
    });

    it('lets a widget cut from a full left column still appear on the right', () => {
        expect(normalizeFrameLayout({ left: ['arc', 'today', 'week', 'moon'], right: ['moon'] }).right).toEqual([
            'moon',
        ]);
    });
});
