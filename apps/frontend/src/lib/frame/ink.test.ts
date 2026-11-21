import { describe, expect, it } from 'vitest';
import { onBlockInk, pickBlockInk, plateRect, type Pixels } from './ink.ts';

describe('plateRect', () => {
    const plate = { w: 400, h: 200 };
    const frame = { w: 1600, h: 1200 };

    it.each([
        ['top-left', 64, 64],
        ['top-middle', 600, 64],
        ['top-right', 1136, 64],
        ['bottom-left', 64, 936],
        ['bottom-middle', 600, 936],
        ['bottom-right', 1136, 936],
    ] as const)('%s', (position, x, y) => {
        expect(plateRect(position, plate, frame)).toEqual({ x, y, w: 400, h: 200 });
    });

    it('scales the inset with the frame', () => {
        expect(plateRect('top-left', plate, { w: 800, h: 600 }, 0.5)).toEqual({ x: 32, y: 32, w: 400, h: 200 });
    });
});

describe('onBlockInk', () => {
    it('sets white on dark inks and black on light ones', () => {
        expect(onBlockInk('#000000')).toBe('#ffffff');
        expect(onBlockInk('#5e5b91')).toBe('#ffffff');
        expect(onBlockInk('#ffff6e')).toBe('#000000');
        expect(onBlockInk('#f16f74')).toBe('#000000');
    });
});

describe('pickBlockInk', () => {
    const frame = { w: 160, h: 120 };
    const plate = { x: 64, y: 40, w: 32, h: 40 };

    /** A 160×120 photo filled with `outside`, with `under` beneath the plate. */
    function photo(outside: string, under = outside): Pixels {
        const data = new Uint8ClampedArray(160 * 120 * 4);
        const rgb = (hex: string) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));
        for (let y = 0; y < 120; y++) {
            for (let x = 0; x < 160; x++) {
                const inPlate = x >= 64 && x < 96 && y >= 40 && y < 80;
                data.set([...rgb(inPlate ? under : outside), 255], (y * 160 + x) * 4);
            }
        }
        return { width: 160, height: 120, data };
    }

    const inks = ['#5e5b91', '#f16f74', '#000000'];

    it('moves off the theme block when the photo around it is that colour', () => {
        expect(pickBlockInk(photo('#5e5b91'), frame, plate, inks, '#5e5b91')).not.toBe('#5e5b91');
    });

    it('keeps the theme block when it already stands out', () => {
        expect(pickBlockInk(photo('#ffffff'), frame, plate, inks, '#5e5b91')).toBe('#5e5b91');
    });

    it('ignores the photo hidden under the plate', () => {
        expect(pickBlockInk(photo('#ffffff', '#5e5b91'), frame, plate, inks, '#5e5b91')).toBe('#5e5b91');
    });

    it('falls back to the preferred ink without candidates', () => {
        expect(pickBlockInk(photo('#5e5b91'), frame, plate, [], '#5e5b91')).toBe('#5e5b91');
    });
});
