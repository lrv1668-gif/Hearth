import { describe, expect, it } from 'vitest';
import { frameTemplates, moveToTemplate, normalizeSlots, parseFrameTemplate, widgetsForSize } from './frameTemplates';

describe('frameTemplates', () => {
    it('gives every slot an area in both orientations, and no stray areas', () => {
        for (const t of frameTemplates) {
            for (const rows of [t.areas.landscape, t.areas.portrait]) {
                const names = new Set(rows.flatMap((r) => r.split(' ')));
                expect([...names].sort()).toEqual(t.slots.map((_, i) => `s${i}`).sort());
                // Every row has the same number of columns, as grid-template-areas requires.
                expect(new Set(rows.map((r) => r.split(' ').length)).size).toBeLessThanOrEqual(1);
            }
        }
    });
});

describe('normalizeSlots', () => {
    it('keeps positions, emptying unknown, repeated and wrong-size widgets in place', () => {
        expect(normalizeSlots('feature', ['coming-up', '', 'quote'])).toEqual(['coming-up', '', 'quote']);
        expect(normalizeSlots('feature', ['nope', 'quote', 'quote'])).toEqual(['', 'quote', '']);
        // Week Ahead is M/L only, so it can't take a quarter.
        expect(normalizeSlots('quad', ['week', 'moon'])).toEqual(['', 'moon', '', '']);
    });

    it('pads and trims to the template', () => {
        expect(normalizeSlots('full', ['week', 'moon'])).toEqual(['week']);
        expect(normalizeSlots('halves', [])).toEqual(['', '']);
        expect(normalizeSlots('columns', ['week'])).toEqual([]);
    });
});

describe('parseFrameTemplate', () => {
    const saved = { template: 'halves' as const, slots: ['week', 'quote'] };

    it('falls back per param to the saved settings', () => {
        expect(parseFrameTemplate(new URLSearchParams(), saved)).toEqual(saved);
        expect(parseFrameTemplate(new URLSearchParams('template=full'), saved)).toEqual({
            template: 'full',
            slots: ['week'],
        });
    });

    it('keeps an empty slot from the URL in its place', () => {
        expect(parseFrameTemplate(new URLSearchParams('template=feature&slots=,moon,quote'), saved)).toEqual({
            template: 'feature',
            slots: ['', 'moon', 'quote'],
        });
    });

    it('ignores an unknown template', () => {
        expect(parseFrameTemplate(new URLSearchParams('template=nope'), saved).template).toBe('halves');
    });
});

describe('moveToTemplate', () => {
    it('carries widgets into the slots they fit, in order', () => {
        expect(moveToTemplate('quad', ['week', 'moon', '', 'quote'])).toEqual(['moon', 'quote', '', '']);
        expect(moveToTemplate('feature', ['moon', 'week'])).toEqual(['week', 'moon', '']);
    });
});

describe('widgetsForSize', () => {
    it('leaves out widgets not designed for the size', () => {
        expect(widgetsForSize('s')).not.toContain('week');
        expect(widgetsForSize('l')).toContain('week');
    });
});
