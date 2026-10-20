import type { CalendarItem, Task } from '$lib/api';
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

export interface FrameOptions {
    mode: 'ambient' | 'daily';
    palette: 'color' | 'bw';
    theme: string;
    font: string;
    orientation: 'landscape' | 'portrait';
    layout: FrameLayout;
    split: number;
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
    });
    return `/frame?${params}`;
}

/* ── Column space ─────────────────────────────────────────────────────────────
   Frame widgets size their lists in "lines": one line of frame body text (48px).
   FrameDaily measures the real column height and body line height at render
   time (so font presets are accounted for); COLUMN_LINES is only the fallback —
   ~15 lines landscape, ~23 portrait at the default font. Every widget pays for its heading, gaps sit between
   widgets, and fixed-height widgets (the arc, the moon) take what they need — the
   rest is split evenly between the widgets whose lists can grow or shrink.     */
export const COLUMN_LINES = { landscape: 15, portrait: 23 } as const;
const HEADING_LINES = 1.25;
const GAP_LINES = 0.85;
const FIXED_LINES: Partial<Record<FrameWidgetId, number>> = { arc: 1.7, moon: 2.7 };

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
