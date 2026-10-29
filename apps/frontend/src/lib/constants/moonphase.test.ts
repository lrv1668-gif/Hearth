import { describe, expect, it } from 'vitest';
import { SYNODIC_PERIOD, upcomingPhases } from './moonphase';

describe('upcomingPhases', () => {
    it('lists the four major phases ahead, soonest first, within one lunar month', () => {
        const now = new Date('2026-10-19T12:00:00Z');
        const phases = upcomingPhases(now);
        expect(new Set(phases.map((p) => p.name)).size).toBe(4);
        const times = phases.map((p) => p.date.getTime());
        expect(times).toEqual([...times].sort((a, b) => a - b));
        expect(times[0]).toBeGreaterThan(now.getTime());
        expect(times[3] - now.getTime()).toBeLessThanOrEqual(SYNODIC_PERIOD * 86_400_000);
    });
});
