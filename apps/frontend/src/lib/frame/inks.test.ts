import { describe, expect, it } from 'vitest';
import { FRAME_INKS } from '#lib/constants/frameThemes.ts';
import { contrast, FRAME_FILLS, grainOf, mixHex, nearestFill } from './inks.ts';

const [black, white, yellow, red, blue, green] = FRAME_INKS;

describe('mixHex', () => {
    it('mixes in plain sRGB, like the renderer', () => {
        expect(mixHex(white, yellow, 0.25)).toBe('#ffffdb');
        expect(mixHex(yellow, red, 0.5)).toBe('#f8b771');
        expect(mixHex(black, blue, 0.5)).toBe('#2f2e49');
    });
});

describe('FRAME_FILLS', () => {
    it('holds the six inks and every pair at three steps', () => {
        expect(FRAME_FILLS).toHaveLength(6 + 15 * 3);
        expect(new Set(FRAME_FILLS.map((f) => f.hex)).size).toBe(FRAME_FILLS.length);
    });

    it('labels a mix by its inks', () => {
        expect(FRAME_FILLS.find((f) => f.hex === '#f8b771')?.label).toBe('Yellow + 50% red');
    });
});

describe('grainOf', () => {
    it('grades a mix by how far apart its inks are in lightness', () => {
        expect(grainOf(white, yellow)).toBe('even');
        expect(grainOf(red, green)).toBe('even');
        expect(grainOf(yellow, red)).toBe('textured');
        expect(grainOf(black, blue)).toBe('textured');
        expect(grainOf(white, blue)).toBe('speckled');
        expect(grainOf(black, white)).toBe('speckled');
    });
});

describe('nearestFill', () => {
    it('finds an exact fill at no distance', () => {
        expect(nearestFill('#f8b771')).toMatchObject({ fill: { hex: '#f8b771' }, distance: 0 });
    });

    it('snaps an off-panel colour to the closest fill', () => {
        expect(nearestFill('#fffedb').fill.hex).toBe('#ffffdb');
    });
});

describe('contrast', () => {
    it('matches WCAG', () => {
        expect(contrast(black, white)).toBeCloseTo(21, 0);
        expect(contrast(red, white)).toBeCloseTo(2.9, 1);
    });
});
