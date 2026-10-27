import { describe, expect, it } from 'vitest';
import { playedLabel } from './music';

// Local times: Saturday 24 October 2026
const at = (day: number, hour: number, minute = 0) => new Date(2026, 9, day, hour, minute).toISOString();
const now = new Date(2026, 9, 24, 19, 0);

describe('playedLabel', () => {
    it('names the part of today', () => {
        expect(playedLabel(at(24, 8), now)).toBe('this morning');
        expect(playedLabel(at(24, 13), now)).toBe('this afternoon');
        expect(playedLabel(at(24, 18), now)).toBe('this evening');
    });

    it('counts the small hours as the night before', () => {
        expect(playedLabel(at(24, 1), now)).toBe('last night');
        expect(playedLabel(at(23, 22), now)).toBe('last night');
        expect(playedLabel(at(24, 1), new Date(2026, 9, 24, 2))).toBe('tonight');
    });

    it('says yesterday, then the weekday, then the date', () => {
        expect(playedLabel(at(23, 15), now)).toBe('yesterday afternoon');
        expect(playedLabel(at(20, 18), now)).toBe('Tuesday evening');
        expect(playedLabel(at(12, 9), now)).toBe('Oct 12');
    });
});
