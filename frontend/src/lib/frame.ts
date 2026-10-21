import type { AmbientPosition, CalendarItem, CurrentWeather, ForecastDay, Task } from '$lib/api';
import { eventDateKey, formatTime, localDateKey } from '$lib/utils';
import type { FrameLayout, FrameWidgetId } from '$lib/constants/frameWidgets';

/**
 * Pending, non-countdown tasks due today — what the e-paper frame's Daily "Today"
 * column shows. Past-dated tasks are left out, matching the dashboard Agenda's window.
 * Timed tasks first by time, then untimed ones.
 */
export function todayTasks(tasks: Task[], now: Date): Task[] {
    const todayKey = localDateKey(now);
    return tasks
        .filter((t) => !t.is_countdown && !t.done && t.due_date?.slice(0, 10) === todayKey)
        .sort((a, b) => (a.due_time ?? '99:99').localeCompare(b.due_time ?? '99:99'));
}

/** Start of an event in local time — all-day events start at local midnight. */
function eventStart(e: CalendarItem): Date {
    return e.is_all_day ? new Date(`${e.start!.slice(0, 10)}T00:00`) : new Date(e.start!);
}

/**
 * The next `count` calendar events that haven't finished yet (in-progress events
 * count), soonest first. Google Tasks are excluded — they belong with tasks.
 */
export function nextEvents(items: CalendarItem[], now: Date, count: number): CalendarItem[] {
    const todayKey = localDateKey(now);
    return items
        .filter((e) => e.kind === 'event' && e.start)
        .filter((e) => (e.is_all_day ? allDayRunsThrough(e, todayKey) : new Date(e.end ?? e.start!) > now))
        .sort((a, b) => eventStart(a).getTime() - eventStart(b).getTime())
        .slice(0, count);
}

/** Whether an all-day event is still on as of `todayKey`. Google's all-day `end` is
 *  exclusive (a one-day event on the 19th ends "…-20"), so it runs while end > today. */
function allDayRunsThrough(e: CalendarItem, todayKey: string): boolean {
    return e.end ? e.end.slice(0, 10) > todayKey : eventDateKey(e) >= todayKey;
}

/** The Coming Up rail's day label: "Today", "Tomorrow", a short weekday within the
 *  week, then "Oct 27". */
export function railDayLabel(e: CalendarItem, now: Date): string {
    const start = eventStart(e);
    const today = new Date(now);
    today.setHours(0, 0, 0, 0);
    const days = Math.round((new Date(start).setHours(0, 0, 0, 0) - today.getTime()) / 86_400_000);
    if (days <= 0) return 'Today';
    if (days === 1) return 'Tomorrow';
    if (days < 7) return start.toLocaleDateString('en-US', { weekday: 'short' });
    return start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

/** "Today", "Tomorrow", or a short weekday for an event's day. */
export function relativeDayLabel(e: CalendarItem, now: Date): string {
    const key = eventDateKey(e);
    if (key === localDateKey(now)) return 'Today';
    const tomorrow = new Date(now);
    tomorrow.setDate(now.getDate() + 1);
    if (key === localDateKey(tomorrow)) return 'Tomorrow';
    return eventStart(e).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
}

export interface WeekEntry {
    title: string;
    /** Display time ("9:30AM"), or null for all-day events and untimed tasks. */
    time: string | null;
    /** Sort key within the day: "HH:MM", or "" so untimed entries come first. */
    sortKey: string;
}

export interface WeekDay {
    key: string; // "YYYY-MM-DD"
    label: string; // "Today", "Tomorrow", "Wed 21"
    entries: WeekEntry[];
}

/**
 * Calendar events, Google Tasks and pending Hearth tasks for the `days` days
 * starting today, grouped by day — only days that have something on them.
 * Untimed entries lead each day, then timed ones in order.
 */
export function weekAhead(tasks: Task[], items: CalendarItem[], now: Date, days = 7): WeekDay[] {
    const keys = Array.from({ length: days }, (_, i) => {
        const d = new Date(now);
        d.setDate(now.getDate() + i);
        return localDateKey(d);
    });
    const byDay = new Map<string, WeekEntry[]>(keys.map((k) => [k, []]));

    for (const e of items) {
        if (!e.start || e.is_completed) continue;
        const timed = e.kind === 'event' && !e.is_all_day;
        const start = new Date(e.start);
        const hhmm = timed
            ? `${String(start.getHours()).padStart(2, '0')}:${String(start.getMinutes()).padStart(2, '0')}`
            : '';
        byDay.get(eventDateKey(e))?.push({ title: e.title, time: timed ? formatTime(hhmm) : null, sortKey: hhmm });
    }
    for (const t of tasks) {
        if (t.is_countdown || t.done || !t.due_date) continue;
        byDay.get(t.due_date.slice(0, 10))?.push({
            title: t.title,
            time: t.due_time ? formatTime(t.due_time) : null,
            sortKey: t.due_time ?? '',
        });
    }

    return keys
        .map((key, i) => ({
            key,
            label: i === 0 ? 'Today' : i === 1 ? 'Tomorrow' : weekdayLabel(key),
            entries: byDay.get(key)!.sort((a, b) => a.sortKey.localeCompare(b.sortKey)),
        }))
        .filter((d) => d.entries.length > 0);
}

/** "Wed 21" for a "YYYY-MM-DD" key, in local time. */
function weekdayLabel(key: string): string {
    const d = new Date(`${key}T00:00`);
    return `${d.toLocaleDateString('en-US', { weekday: 'short' })} ${d.getDate()}`;
}

/** Where Ambient mode's info card can sit — top/bottom row, left/middle/right — in
 *  picker (reading) order. */
export const AMBIENT_POSITIONS = [
    { value: 'top-left', label: 'Top left' },
    { value: 'top-middle', label: 'Top middle' },
    { value: 'top-right', label: 'Top right' },
    { value: 'bottom-left', label: 'Bottom left' },
    { value: 'bottom-middle', label: 'Bottom middle' },
    { value: 'bottom-right', label: 'Bottom right' },
] as const satisfies readonly { value: AmbientPosition; label: string }[];
export const DEFAULT_AMBIENT_POSITION: AmbientPosition = 'bottom-left';

export function isAmbientPosition(value: string | null | undefined): value is AmbientPosition {
    return AMBIENT_POSITIONS.some((c) => c.value === value);
}

export interface FrameOptions {
    mode: 'ambient' | 'daily';
    palette: 'color' | 'bw';
    theme: string;
    font: string;
    orientation: 'landscape' | 'portrait';
    layout: FrameLayout;
    split: number;
    position: AmbientPosition;
}

/** The /frame URL for a set of options — what the preview (and later the Display
 *  service) loads. Every option is spelled out so the URL is self-describing. */
export function frameUrl(o: FrameOptions): string {
    const params = new URLSearchParams({
        mode: o.mode,
        palette: o.palette,
        theme: o.theme,
        font: o.font,
        orientation: o.orientation,
        left: o.layout.left.join(','),
        right: o.layout.right.join(','),
        split: String(o.split),
        position: o.position,
    });
    return `/frame?${params}`;
}

/* ── Column space ─────────────────────────────────────────────────────────────
   Frame widgets size their lists in "lines": one line of frame body text (48px).
   FrameDaily measures the real column height and body line height at render
   time (so font presets are accounted for); COLUMN_LINES is only the fallback —
   ~15 lines landscape, ~20 portrait at the default font, below the masthead. Every widget pays for its kicker, gaps sit between widgets, and
   fixed-height widgets (the arc, the moon, the countdown stamps) take what they
   need — the rest is split evenly between the widgets whose lists can grow.   */
export const COLUMN_LINES = { landscape: 15, portrait: 20 } as const;
const HEADING_LINES = 1.05;
const GAP_LINES = 0.85;
const FIXED_LINES: Partial<Record<FrameWidgetId, number>> = { arc: 1.7, moon: 2.7, countdowns: 4.5 };

/** Content lines (excluding its heading) each widget in a column gets. */
export function allocateLines(column: readonly FrameWidgetId[], totalLines: number): Map<FrameWidgetId, number> {
    const flexible = column.filter((id) => FIXED_LINES[id] === undefined);
    const overhead = column.length * HEADING_LINES + Math.max(0, column.length - 1) * GAP_LINES;
    const fixed = column.reduce((sum, id) => sum + (FIXED_LINES[id] ?? 0), 0);
    const share = flexible.length ? Math.max(0, totalLines - overhead - fixed) / flexible.length : 0;
    return new Map(column.map((id) => [id, FIXED_LINES[id] ?? share]));
}

/** How many list items of `itemLines` height fit in `lines` (at least 1). */
export function itemsThatFit(lines: number, itemLines: number): number {
    return Math.max(1, Math.floor(lines / itemLines));
}

/* ── Almanac edition ──────────────────────────────────────────────────────────
   Small derivations for the Daily masthead, sun path, stamps and forecast strip. */

/** Day of the year, 1-based — the edition number in the edition bar. */
export function dayOfYear(now: Date): number {
    const start = new Date(now.getFullYear(), 0, 1);
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    return Math.round((today.getTime() - start.getTime()) / 86_400_000) + 1;
}

/** How far `now` is through the daylight between sunrise and sunset (0–1), or null
 *  while the sun is down. */
export function sunProgress(sunrise: Date, sunset: Date, now: Date): number | null {
    const span = sunset.getTime() - sunrise.getTime();
    const elapsed = now.getTime() - sunrise.getTime();
    if (span <= 0 || elapsed < 0 || elapsed > span) return null;
    return elapsed / span;
}

/** "11 h 03 m" between two times. */
export function durationLabel(from: Date, to: Date): string {
    const minutes = Math.max(0, Math.round((to.getTime() - from.getTime()) / 60_000));
    return `${Math.floor(minutes / 60)} h ${String(minutes % 60).padStart(2, '0')} m`;
}

/** The masthead's one sentence of weather: "Partly cloudy, 58° now and rising to 63°."
 *  Once the day's high is (about) reached it looks ahead to the low instead. */
export function weatherSentence(current: CurrentWeather, today: ForecastDay | null): string {
    const now = Math.round(current.temperature_f);
    const lead = `${current.description}, ${now}° now`;
    if (!today) return `${lead}.`;
    const high = Math.round(today.temp_max_f);
    const low = Math.round(today.temp_min_f);
    return high > now ? `${lead} and rising to ${high}°.` : `${lead}, falling to ${low}° tonight.`;
}

export interface Countdown {
    task: Task;
    days: number;
}

/** Pending countdowns from today on, soonest first. */
export function upcomingCountdowns(tasks: Task[], now: Date, limit: number): Countdown[] {
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
    return tasks
        .filter((t) => t.is_countdown && t.due_date && !t.done)
        .map((task) => ({
            task,
            days: Math.round((new Date(`${task.due_date!.slice(0, 10)}T00:00`).getTime() - today) / 86_400_000),
        }))
        .filter((c) => c.days >= 0)
        .sort((a, b) => a.days - b.days)
        .slice(0, limit);
}

/** The shared temperature scale for the forecast strip's range bars: each day's
 *  bar as left offset and width, in percent of the week's lowest low to highest high. */
export function forecastBars(days: ForecastDay[]): { left: number; width: number }[] {
    const lo = Math.min(...days.map((d) => d.temp_min_f));
    const span = Math.max(...days.map((d) => d.temp_max_f)) - lo || 1;
    return days.map((d) => ({
        left: ((d.temp_min_f - lo) / span) * 100,
        width: ((d.temp_max_f - d.temp_min_f) / span) * 100,
    }));
}
