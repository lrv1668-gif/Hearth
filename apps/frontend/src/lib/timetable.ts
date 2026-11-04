import type { StopTimetable, TimetableLine } from '#lib/api.ts';
import type { TrainStop } from '#lib/constants/trains.ts';

/** The Settings line-filter key — the same one TrainsWidget builds from live departures. */
export function lineKey(line: Pick<TimetableLine, 'route_short_name' | 'headsign' | 'mode'>): string {
    return `${line.route_short_name}|${line.headsign ?? ''}|${line.mode}`;
}

export interface TimetableStop {
    stopKey: string;
    label: string;
    lines: TimetableLine[];
}

/**
 * The household's stops in Settings order, each with its timetable and only the lines its
 * filter allows (unset shows every line; `[]` shows none). Stops with nothing left are dropped.
 */
export function timetableStops(stops: TrainStop[], timetables: StopTimetable[]): TimetableStop[] {
    return stops.flatMap((stop) => {
        const t = timetables.find((x) => x.stop_key === stop.stopKey);
        if (!t) return [];
        const allowed = stop.lineFilter ? new Set(stop.lineFilter) : null;
        const lines = t.lines.filter((line) => !allowed || allowed.has(lineKey(line)));
        return lines.length ? [{ stopKey: stop.stopKey, label: stop.label || t.stop_name || stop.stopKey, lines }] : [];
    });
}

/** A departure as the timetable prints it: "7:04", no am/pm — the caption carries that. */
export function clockTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }).replace(/\s?[AP]M$/i, '');
}

/** The block's window, "7–9 pm" (or "11 am–1 pm" across noon). */
export function windowLabel(startIso: string, endIso: string): string {
    const part = (iso: string) => {
        const [hour, period] = new Date(iso)
            .toLocaleTimeString('en-US', { hour: 'numeric' })
            .toLowerCase()
            .split(/\s+/);
        return { hour, period };
    };
    const start = part(startIso);
    const end = part(endIso);
    return start.period === end.period
        ? `${start.hour}–${end.hour} ${end.period}`
        : `${start.hour} ${start.period}–${end.hour} ${end.period}`;
}
