import { describe, expect, it } from 'vitest';
import { isPhotoInterval, photoIntervals } from '#lib/constants/frameModes.ts';
import {
    frameUrl,
    frameCanvas,
    isFrameSize,
    isFrameDithering,
    isPortraitRotation,
    normalizeFrameSize,
    toQuietHours,
    DEFAULT_FRAME_SIZE,
    isAmbientPosition,
    isPhotoAdjust,
    parsePhotoAdjust,
    photoAdjustApplies,
    photoFilter,
} from './settings.ts';

describe('isAmbientPosition', () => {
    it('accepts the six positions and rejects anything else', () => {
        expect(
            ['top-left', 'top-middle', 'top-right', 'bottom-left', 'bottom-middle', 'bottom-right'].every(
                isAmbientPosition
            )
        ).toBe(true);
        expect(isAmbientPosition('middle')).toBe(false);
        expect(isAmbientPosition(null)).toBe(false);
    });
});

describe('frameUrl', () => {
    it('spells out every option', () => {
        expect(
            frameUrl({
                mode: 'daily',
                palette: 'bw',
                theme: 'night',
                font: 'lora',
                textSize: 120,
                orientation: 'portrait',
                width: 800,
                height: 480,
                layout: { left: ['today', 'moon'], right: [] },
                split: 55,
                template: 'feature',
                slots: ['week', '', 'quote'],
                position: 'top-middle',
            })
        ).toBe(
            '/frame?mode=daily&palette=bw&theme=night&font=lora&text=120&orientation=portrait&width=800&height=480&left=today%2Cmoon&right=&split=55&template=feature&slots=week%2C%2Cquote&position=top-middle'
        );
    });

    it('adds only the photo adjustments that differ from 100', () => {
        const url = frameUrl({
            mode: 'ambient',
            palette: 'color',
            theme: 'almanac',
            font: 'inter',
            textSize: 100,
            orientation: 'landscape',
            width: 1600,
            height: 1200,
            layout: { left: [], right: [] },
            split: 50,
            template: 'columns',
            slots: [],
            position: 'bottom-left',
            adjust: { brightness: 125, contrast: 100, saturation: 90 },
        });
        expect(url).toContain('&brightness=125&saturation=90');
        expect(url).not.toContain('contrast=');
    });
});

describe('frameCanvas', () => {
    it('keeps the native size in landscape and swaps it in portrait', () => {
        expect(frameCanvas({ width: 800, height: 480, orientation: 'landscape' })).toEqual({
            w: 800,
            h: 480,
            scale: 0.4,
        });
        expect(frameCanvas({ width: 800, height: 480, orientation: 'portrait' })).toEqual({
            w: 480,
            h: 800,
            scale: 0.4,
        });
    });

    it('scales to fit both edges', () => {
        expect(frameCanvas({ width: 800, height: 600, orientation: 'landscape' }).scale).toBe(0.5);
        expect(frameCanvas({ width: 2000, height: 1200, orientation: 'landscape' }).scale).toBe(1);
    });

    it('is 1:1 at the reference panel', () => {
        expect(frameCanvas({ ...DEFAULT_FRAME_SIZE, orientation: 'landscape' }).scale).toBe(1);
    });
});

describe('frame size', () => {
    it('accepts in-range integer sizes with width ≥ height', () => {
        expect(isFrameSize(800, 480)).toBe(true);
        expect(isFrameSize(480, 800)).toBe(false);
        expect(isFrameSize(100, 100)).toBe(false);
        expect(isFrameSize(800.5, 480)).toBe(false);
        expect(isFrameSize('800', 480)).toBe(false);
    });

    it('normalizes typed sizes into range, long edge first', () => {
        expect(normalizeFrameSize(480, 800)).toEqual({ width: 800, height: 480 });
        expect(normalizeFrameSize(8, 99999)).toEqual({ width: 4000, height: 200 });
        expect(normalizeFrameSize(NaN, 600.4)).toEqual({ width: 1600, height: 600 });
    });
});

describe('panel render settings', () => {
    it('accepts only the two dithering choices', () => {
        expect(isFrameDithering('hearth')).toBe(true);
        expect(isFrameDithering('frame')).toBe(true);
        expect(isFrameDithering('inky')).toBe(false);
        expect(isFrameDithering(undefined)).toBe(false);
    });

    it('accepts only quarter turns for portrait', () => {
        expect(isPortraitRotation(90)).toBe(true);
        expect(isPortraitRotation(270)).toBe(true);
        expect(isPortraitRotation(180)).toBe(false);
        expect(isPortraitRotation('90')).toBe(false);
    });

    it("keeps photo intervals within Display's 15 min – 1 day range", () => {
        expect(photoIntervals.every((i) => isPhotoInterval(i.minutes))).toBe(true);
        expect(isPhotoInterval(14)).toBe(false);
        expect(isPhotoInterval(1441)).toBe(false);
        expect(isPhotoInterval(30.5)).toBe(false);
    });
});

describe('toQuietHours', () => {
    it('reads a saved window', () => {
        expect(toQuietHours('23:00', '06:00')).toEqual({ start: '23:00', end: '06:00' });
    });

    it('is off when either end is missing, malformed or equal', () => {
        expect(toQuietHours(null, null)).toBeNull();
        expect(toQuietHours('23:00', null)).toBeNull();
        expect(toQuietHours('25:00', '06:00')).toBeNull();
        expect(toQuietHours('06:00', '06:00')).toBeNull();
    });
});

describe('photo adjustments', () => {
    it("accepts whole percents within Display's 50–150 range", () => {
        expect(isPhotoAdjust(50)).toBe(true);
        expect(isPhotoAdjust(150)).toBe(true);
        expect(isPhotoAdjust(49)).toBe(false);
        expect(isPhotoAdjust(151)).toBe(false);
        expect(isPhotoAdjust(102.5)).toBe(false);
        expect(isPhotoAdjust('120')).toBe(false);
    });

    it('reads them from the URL, leaving missing or bad ones at 100', () => {
        const params = new URLSearchParams('brightness=130&contrast=abc&saturation=200');
        expect(parsePhotoAdjust(params)).toEqual({ brightness: 130, contrast: 100, saturation: 100 });
        expect(parsePhotoAdjust(new URLSearchParams(''))).toEqual({ brightness: 100, contrast: 100, saturation: 100 });
    });

    it('builds a CSS filter from the changed ones only', () => {
        expect(photoFilter({ brightness: 100, contrast: 100, saturation: 100 })).toBe('');
        expect(photoFilter({ brightness: 130, contrast: 100, saturation: 80 })).toBe('brightness(1.3) saturate(0.8)');
    });

    it('applies where Hearth dithers: its own dithering, or black and white', () => {
        expect(photoAdjustApplies('hearth', 'color')).toBe(true);
        expect(photoAdjustApplies('frame', 'bw')).toBe(true);
        expect(photoAdjustApplies('frame', 'color')).toBe(false);
    });
});
