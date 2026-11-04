import { describe, expect, it } from 'vitest';
import type { StopTimetable } from '#lib/api.ts';
import { clockTime, lineKey, timetableStops, windowLabel } from './timetable.ts';

const timetable: StopTimetable = {
    stop_key: 's-1',
    stop_name: 'Fruitvale',
    window_start: '2026-11-04T19:00:00Z',
    window_end: '2026-11-04T21:00:00Z',
    lines: [
        { route_short_name: 'Red', headsign: 'Richmond', mode: 'rail', departures: ['2026-11-04T19:16:00Z'] },
        { route_short_name: '1', headsign: null, mode: 'bus', departures: ['2026-11-04T19:30:00Z'] },
    ],
};

describe('timetableStops', () => {
    it('keeps every line when the stop has no filter, labelled from Settings', () => {
        const [stop] = timetableStops([{ label: 'Home', stopKey: 's-1' }], [timetable]);
        expect(stop.label).toBe('Home');
        expect(stop.lines).toHaveLength(2);
    });

    it('applies the line filter, keyed like the dashboard', () => {
        const [stop] = timetableStops([{ label: 'Home', stopKey: 's-1', lineFilter: ['1||bus'] }], [timetable]);
        expect(stop.lines.map(lineKey)).toEqual(['1||bus']);
    });

    it('drops a stop whose filter hides every line, or that has no timetable', () => {
        expect(
            timetableStops(
                [
                    { label: 'Home', stopKey: 's-1', lineFilter: [] },
                    { label: 'Work', stopKey: 's-2' },
                ],
                [timetable]
            )
        ).toEqual([]);
    });
});

describe('labels', () => {
    it('prints times without am/pm and the window with it', () => {
        const local = (h: number, m = 0) => new Date(2026, 10, 4, h, m).toISOString();
        expect(clockTime(local(19, 4))).toBe('7:04');
        expect(windowLabel(local(19), local(21))).toBe('7–9 pm');
        expect(windowLabel(local(11), local(13))).toBe('11 am–1 pm');
    });
});
