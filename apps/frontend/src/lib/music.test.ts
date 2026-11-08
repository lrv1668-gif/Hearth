import { describe, expect, it } from 'vitest';
import { formatTrackTime, playedLabel, trackPosition } from './music';

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

describe('formatTrackTime', () => {
    it('writes minutes and zero-padded seconds', () => {
        expect(formatTrackTime(0)).toBe('0:00');
        expect(formatTrackTime(61_900)).toBe('1:01');
        expect(formatTrackTime(238_000)).toBe('3:58');
    });

    it('adds hours for long tracks', () => {
        expect(formatTrackTime(3_725_000)).toBe('1:02:05');
    });
});

describe('trackPosition', () => {
    const track = { progress_ms: 60_000, duration_ms: 180_000, is_playing: true };

    it('moves on from the last report while playing', () => {
        expect(trackPosition(track, 1_000, 4_000)).toBe(63_000);
    });

    it('holds still while paused', () => {
        expect(trackPosition({ ...track, is_playing: false }, 1_000, 9_000)).toBe(60_000);
    });

    it('stops at the end of the track', () => {
        expect(trackPosition(track, 0, 999_000)).toBe(180_000);
    });
});
