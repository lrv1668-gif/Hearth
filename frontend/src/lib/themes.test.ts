import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

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

const themes = [...css.matchAll(/\[data-theme='([\w-]+)'\]\s*\{([\s\S]*?)\n\}/g)]
    .filter(([, id]) => !id.startsWith('epaper-'))
    .map(([, id, body]) => ({
        id,
        tokens: Object.fromEntries([...body.matchAll(/--([\w-]+):\s*(#[0-9a-f]{6})/gi)].map(([, k, v]) => [k, v])),
    }));

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
