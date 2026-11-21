// What the frame's lists show: today's tasks, the next events, the week ahead and
// countdowns, picked from the task and calendar stores in local time.
import type { CalendarItem, Task } from '#lib/api.ts';
import { eventDateKey, formatTime, localDateKey } from '#lib/utils.ts';

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

/** A day of the agenda: its heading and the events shown under it. */
export interface AgendaDay {
    key: string;
    label: string;
    events: CalendarItem[];
}

/** The next events grouped by day, cut to fit `lines` — each day's heading costs
 *  `headingLines` and each event `rowLines`. A day only starts if its heading and at
 *  least one event fit. The heading is "Today", "Tomorrow", else "Thursday, Oct 30". */
export function fitAgenda(
    items: CalendarItem[],
    now: Date,
    lines: number,
    { headingLines, rowLines }: { headingLines: number; rowLines: number }
): AgendaDay[] {
    const days: AgendaDay[] = [];
    let left = lines;
    for (const e of nextEvents(items, now, 30)) {
        const key = eventDateKey(e) < localDateKey(now) ? localDateKey(now) : eventDateKey(e);
        const day = days.at(-1);
        if (day?.key === key) {
            if (left < rowLines) break;
            day.events.push(e);
            left -= rowLines;
        } else {
            if (left < headingLines + rowLines) break;
            const label = railDayLabel(e, now);
            days.push({
                key,
                label:
                    label === 'Today' || label === 'Tomorrow'
                        ? label
                        : new Date(`${key}T00:00`).toLocaleDateString('en-US', {
                              weekday: 'long',
                              month: 'short',
                              day: 'numeric',
                          }),
                events: [e],
            });
            left -= headingLines + rowLines;
        }
    }
    return days;
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

export interface Countdown {
    task: Task;
    days: number;
}

/** Countdown mode's countdown: the chosen one while it's still ahead, else the next one coming up. */
export function heroCountdown(tasks: Task[], now: Date, id: number | null): Countdown | null {
    const upcoming = upcomingCountdowns(tasks, now, Infinity);
    return upcoming.find((c) => c.task.id === id) ?? upcoming[0] ?? null;
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
