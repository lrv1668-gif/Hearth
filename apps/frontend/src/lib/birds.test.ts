import { describe, expect, it } from 'vitest';
import { rareFirst } from './birds';

describe('rareFirst', () => {
    it('moves notable sightings to the front and keeps the rest in order', () => {
        const sightings = [
            { id: 'a', is_notable: false },
            { id: 'b', is_notable: true },
            { id: 'c', is_notable: false },
            { id: 'd', is_notable: true },
        ];
        expect(rareFirst(sightings).map((s) => s.id)).toEqual(['b', 'd', 'a', 'c']);
    });

    it('does not reorder the original array', () => {
        const sightings = [{ is_notable: false }, { is_notable: true }];
        rareFirst(sightings);
        expect(sightings[0].is_notable).toBe(false);
    });
});
