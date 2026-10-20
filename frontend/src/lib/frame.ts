import type { CalendarItem, Task } from '$lib/api';
import { eventDateKey, localDateKey } from '$lib/utils';

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
        .filter((e) => (e.is_all_day ? eventDateKey(e) >= todayKey : new Date(e.end ?? e.start!) > now))
        .sort((a, b) => eventStart(a).getTime() - eventStart(b).getTime())
        .slice(0, count);
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

export interface FrameOptions {
    mode: 'ambient' | 'daily';
    palette: 'color' | 'bw';
    theme: string;
    font: string;
    orientation: 'landscape' | 'portrait';
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
    });
    return `/frame?${params}`;
}
