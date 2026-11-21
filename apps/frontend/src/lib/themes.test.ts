import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { FRAME_INKS, frameDataTheme, frameThemes } from './constants/frameThemes';
import { nearestFill } from './frame/inks';
import { customThemeFromTokens, customThemeTokens, DEFAULT_CUSTOM_THEME, rateTheme } from './frame/customTheme';

// WCAG 2.x contrast. --text-2 carries readable secondary text (AA body, 4.5:1);
// --text-3 is limited to icons and non-text marks (AA graphics, 3:1).
// --text-4 is decorative only and deliberately unchecked.
const css = readFileSync(new URL('../themes.css', import.meta.url), 'utf8');

function luminance(hex: string): number {
    const [r, g, b] = [1, 3, 5].map((i) => {
        const c = parseInt(hex.slice(i, i + 2), 16) / 255;
        return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrast(a: string, b: string): number {
    const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
    return (hi + 0.05) / (lo + 0.05);
}

const allThemes = [...css.matchAll(/\[data-theme='([\w-]+)'\]\s*\{([\s\S]*?)\n\}/g)].map(([, id, body]) => ({
    id,
    tokens: Object.fromEntries(
        [...body.matchAll(/--([\w-]+):\s*(#[0-9a-f]{6})/gi)].map(([, k, v]) => [k, v.toLowerCase()])
    ) as Record<string, string>,
}));
const themes = allThemes.filter((t) => !t.id.startsWith('epaper-'));
const frameCss = allThemes.filter((t) => t.id.startsWith('epaper-'));

// The Spectra 6 inks (measured, white-balanced); fills may also be any two-ink mix
// from lib/frame/inks.ts that doesn't speckle — see "Frame palettes" in themes.css.
const INKS: readonly string[] = FRAME_INKS;
// Tokens drawn as text, icons or thin lines — these must be a pure ink, since a
// dithered mix breaks thin strokes apart.
const LINE_TOKENS = [
    'border',
    'text-1',
    'text-2',
    'text-3',
    'text-4',
    'accent',
    'done',
    'arc-outline',
    'block',
    'on-block',
];

describe('theme contrast', () => {
    it('parses every theme', () => {
        expect(themes.length).toBeGreaterThanOrEqual(18);
    });

    it.each(themes)('$id: --text-2 meets 4.5:1 on --bg', ({ tokens }) => {
        expect(contrast(tokens['text-2'], tokens['bg'])).toBeGreaterThanOrEqual(4.5);
    });

    it.each(themes)('$id: --text-3 meets 3:1 on --bg', ({ tokens }) => {
        expect(contrast(tokens['text-3'], tokens['bg'])).toBeGreaterThanOrEqual(3);
    });

    // --accent and --danger are drawn as text (links, widget titles, errors) on the page
    // and on the raised tiles, so they need AA body contrast on every background.
    it.each(themes)('$id: --accent and --danger meet 4.5:1 on every background', ({ tokens }) => {
        for (const bg of ['bg', 'surface', 'surface-hi']) {
            expect(contrast(tokens['accent'], tokens[bg])).toBeGreaterThanOrEqual(4.5);
            expect(contrast(tokens['danger'], tokens[bg])).toBeGreaterThanOrEqual(4.5);
        }
    });

    it.each(themes)('$id: --accent-fg meets 4.5:1 on --accent and --accent-hi', ({ tokens }) => {
        expect(contrast(tokens['accent-fg'], tokens['accent'])).toBeGreaterThanOrEqual(4.5);
        expect(contrast(tokens['accent-fg'], tokens['accent-hi'])).toBeGreaterThanOrEqual(4.5);
    });

    // A chromatic accent, not a second copy of the body text colour.
    it.each(themes)('$id: --accent differs from --text-1', ({ tokens }) => {
        expect(tokens['accent']).not.toBe(tokens['text-1']);
    });
});

describe('frame palettes', () => {
    it('has a block for every frame theme plus both B&W variants', () => {
        const ids = frameCss.map((t) => t.id);
        for (const t of frameThemes) expect(ids).toContain(`epaper-${t.id}`);
        expect(ids).toEqual(expect.arrayContaining(['epaper-bw', 'epaper-bw-night', 'epaper-custom']));
    });

    // Within 2 per channel: the original cream and butter were hand-rounded (#fffedb, not #ffffdb)
    it.each(frameCss)('$id: uses only inks and two-ink mixes that dither cleanly', ({ tokens }) => {
        for (const value of Object.values(tokens)) {
            const { fill, distance } = nearestFill(value);
            expect(distance, value).toBeLessThanOrEqual(2);
            expect(fill.grain, value).not.toBe('speckled');
        }
    });

    it.each(frameCss)('$id: text, icons and lines are pure inks', ({ tokens }) => {
        for (const key of LINE_TOKENS) if (tokens[key]) expect(INKS).toContain(tokens[key]);
    });

    // Frame text is never smaller than 22px, so the large-text 3:1 floor applies to --text-2.
    it.each(frameCss)('$id: text is legible on --bg', ({ tokens }) => {
        expect(contrast(tokens['text-1'], tokens['bg'])).toBeGreaterThanOrEqual(7);
        expect(contrast(tokens['text-2'], tokens['bg'])).toBeGreaterThanOrEqual(3);
    });

    // The forecast band sits on --surface, so its type must read there too.
    it.each(frameCss)('$id: --on-block is legible on --block', ({ tokens }) => {
        expect(contrast(tokens['on-block'], tokens['block'])).toBeGreaterThanOrEqual(4.5);
    });

    it.each(frameCss)('$id: text is legible on --surface', ({ tokens }) => {
        expect(contrast(tokens['text-1'], tokens['surface'])).toBeGreaterThanOrEqual(7);
        expect(contrast(tokens['text-2'], tokens['surface'])).toBeGreaterThanOrEqual(3);
    });
});

describe('frameDataTheme', () => {
    it('maps colour mode to the theme block', () => {
        expect(frameDataTheme('garden', 'color')).toBe('epaper-garden');
    });

    it('maps B&W mode by theme lightness', () => {
        expect(frameDataTheme('almanac', 'bw')).toBe('epaper-bw');
        expect(frameDataTheme('night', 'bw')).toBe('epaper-bw-night');
    });

    it('maps Custom by its paper', () => {
        expect(frameDataTheme('custom', 'color')).toBe('epaper-custom');
        expect(frameDataTheme('custom', 'bw', { ...DEFAULT_CUSTOM_THEME, paper: '#000000' })).toBe('epaper-bw-night');
        expect(frameDataTheme('custom', 'bw', DEFAULT_CUSTOM_THEME)).toBe('epaper-bw');
    });
});

describe('custom frame themes', () => {
    // Every frame token the built-in themes set must come out of customThemeTokens too
    it('sets every token a built-in frame theme does', () => {
        const almanac = frameCss.find((t) => t.id === 'epaper-almanac')!;
        expect(Object.keys(customThemeTokens(DEFAULT_CUSTOM_THEME))).toEqual(
            expect.arrayContaining(Object.keys(almanac.tokens))
        );
    });

    it('rates every built-in light theme, read back as roles, without an avoid', () => {
        for (const t of frameCss.filter((t) => !t.id.startsWith('epaper-bw'))) {
            const roles = customThemeFromTokens(t.tokens);
            const avoided = rateTheme(roles).filter((r) => r.rating.level === 'avoid');
            expect(avoided, t.id).toEqual([]);
        }
    });
});
