import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { frameDataTheme, frameThemes } from './constants/frameThemes';

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

// The Spectra 6 inks (measured, white-balanced) and the two-ink mixes that dither
// evenly — see the "Frame palettes" comment in themes.css.
const INKS = ['#000000', '#ffffff', '#ffff6e', '#f16f74', '#5e5b91', '#5a8d6c'];
const MIXES = ['#fffedb', '#fffeb6', '#f8b771', '#2f2e49'];
// Tokens drawn as text, icons or thin lines — these must be a pure ink, since a
// dithered mix breaks thin strokes apart.
const LINE_TOKENS = ['border', 'text-1', 'text-2', 'text-3', 'text-4', 'accent', 'done', 'arc-outline'];

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
});

describe('frame palettes', () => {
    it('has a block for every frame theme plus both B&W variants', () => {
        const ids = frameCss.map((t) => t.id);
        for (const t of frameThemes) expect(ids).toContain(`epaper-${t.id}`);
        expect(ids).toEqual(expect.arrayContaining(['epaper-bw', 'epaper-bw-night']));
    });

    it.each(frameCss)('$id: uses only inks and approved two-ink mixes', ({ tokens }) => {
        for (const value of Object.values(tokens)) expect([...INKS, ...MIXES]).toContain(value);
    });

    it.each(frameCss)('$id: text, icons and lines are pure inks', ({ tokens }) => {
        for (const key of LINE_TOKENS) if (tokens[key]) expect(INKS).toContain(tokens[key]);
    });

    // Frame text is never smaller than 22px, so the large-text 3:1 floor applies to --text-2.
    it.each(frameCss)('$id: text is legible on --bg', ({ tokens }) => {
        expect(contrast(tokens['text-1'], tokens['bg'])).toBeGreaterThanOrEqual(7);
        expect(contrast(tokens['text-2'], tokens['bg'])).toBeGreaterThanOrEqual(3);
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
});
