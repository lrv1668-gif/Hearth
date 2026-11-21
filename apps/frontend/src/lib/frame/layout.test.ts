import { describe, expect, it } from 'vitest';
import { allocateLines, fitFontSize, itemsThatFit } from './layout.ts';

describe('allocateLines', () => {
    it('gives a lone widget the whole column minus its heading', () => {
        expect(allocateLines(['today'], 12).get('today')).toBeCloseTo(10.95);
    });

    it('gives fixed-height widgets their size and splits the rest between the others', () => {
        const lines = allocateLines(['arc', 'today', 'week'], 12);
        expect(lines.get('arc')).toBe(1.7);
        expect(lines.get('today')).toBeCloseTo(lines.get('week')!);
        expect(lines.get('today')!).toBeLessThan(5);
    });

    it('treats countdown stamps as fixed height', () => {
        expect(allocateLines(['countdowns', 'today'], 12).get('countdowns')).toBe(4.5);
    });

    it('gives Last Played its album square and the rest to its neighbour', () => {
        const lines = allocateLines(['last-played', 'today'], 20);
        expect(lines.get('last-played')).toBe(5.3);
        expect(lines.get('today')).toBeCloseTo(20 - 2 * 1.05 - 0.85 - 5.3);
    });

    it('never hands out negative space', () => {
        expect(allocateLines(['today', 'week', 'quote'], 2).get('today')).toBe(0);
    });
});

describe('itemsThatFit', () => {
    it('floors to whole items but always shows one', () => {
        expect(itemsThatFit(5, 2)).toBe(2);
        expect(itemsThatFit(0, 2)).toBe(1);
    });
});

describe('fitFontSize', () => {
    it('finds the largest size that still fits', () => {
        expect(fitFontSize((px) => px <= 87, 22, 200)).toBe(87);
    });

    it('stops at the bounds', () => {
        expect(fitFontSize(() => true, 22, 200)).toBe(200);
        expect(fitFontSize(() => false, 22, 200)).toBe(22);
    });

    it('works on fractional bounds from a scaled frame', () => {
        expect(fitFontSize((px) => px <= 50, 10.5, 99.9)).toBe(50);
    });
});
