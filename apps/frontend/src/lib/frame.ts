import type { AmbientPosition, CalendarItem, CurrentWeather, ForecastDay, FrameStatus, Photo, Task } from '$lib/api';
import { eventDateKey, formatTime, localDateKey } from '$lib/utils';
import type { FrameLayout, FrameWidgetId } from '$lib/constants/frameWidgets';
import type { FrameTemplateId } from '$lib/constants/frameTemplates';
import type { FrameModeId } from '$lib/constants/frameModes';
import { getContext, setContext } from 'svelte';

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

/* ── Text size ────────────────────────────────────────────────────────────────
   A percent on the frame's reading type (titles down to captions) and icons, for a
   panel read from closer or further than the tuned 3–4 m. Never below 100: the
   tuned sizes are the e-paper legibility floor. Larger fits fewer list items. */
export const DEFAULT_FRAME_TEXT_SIZE = 100;
export const MIN_FRAME_TEXT_SIZE = 100;
export const MAX_FRAME_TEXT_SIZE = 150;

export function isFrameTextSize(value: unknown): value is number {
    return (
        Number.isInteger(value) && (value as number) >= MIN_FRAME_TEXT_SIZE && (value as number) <= MAX_FRAME_TEXT_SIZE
    );
}

/* ── Panel size ───────────────────────────────────────────────────────────────
   The panel's native size is landscape (width ≥ height); portrait swaps it. The
   frame's type, icons and spacing are tuned for the 1600×1200 Inky Impression
   13.3" and scale so that reference fits the panel in both directions — a
   wider-aspect panel (800×480) scales by its short edge, so portrait columns keep
   room. Line weights stay 3px so they land on whole pixels. */
export const FRAME_REFERENCE_SIZE = { width: 1600, height: 1200 } as const;
export const DEFAULT_FRAME_SIZE = { width: 1600, height: 1200 } as const;
export const MIN_FRAME_SIZE = 200;
export const MAX_FRAME_SIZE = 4000;

/** Which way a portrait frame is turned onto the panel's native landscape for
 *  GET /display/frame, in clockwise degrees — depends on which side the frame hangs. */
export type PortraitRotation = 90 | 270;
export const DEFAULT_PORTRAIT_ROTATION: PortraitRotation = 90;

export function isPortraitRotation(value: unknown): value is PortraitRotation {
    return value === 90 || value === 270;
}

/** Who dithers GET /display/frame: Hearth's renderer (per mode), or the frame client —
 *  sent undithered, so inkypull lets inky Floyd–Steinberg it on the Pi. */
export type FrameDithering = 'hearth' | 'frame';
export const DEFAULT_FRAME_DITHERING: FrameDithering = 'hearth';

export function isFrameDithering(value: unknown): value is FrameDithering {
    return value === 'hearth' || value === 'frame';
}

/* ── Quiet hours ──────────────────────────────────────────────────────────────
   A nightly window (home time) in which GET /display/frame keeps its picture instead
   of redrawing on the interval; a mode change still draws. An end before the start
   runs past midnight; Display refuses equal times. */
export interface QuietHours {
    start: string;
    end: string;
}
export const DEFAULT_QUIET_HOURS: QuietHours = { start: '23:00', end: '06:00' };

const HH_MM = /^([01]\d|2[0-3]):[0-5]\d$/;

/** Saved quiet_start/quiet_end as a window, or null when off or malformed. */
export function toQuietHours(start: unknown, end: unknown): QuietHours | null {
    return typeof start === 'string' && typeof end === 'string' && HH_MM.test(start) && HH_MM.test(end) && start !== end
        ? { start, end }
        : null;
}

/* ── Frame check-in ───────────────────────────────────────────────────────────
   GET /display/frame/status's last_poll: when the frame client (inkypull) last asked
   for its picture. A frame that's gone quiet for longer than this gets a warning. */
export const FRAME_CHECK_IN_STALE_MS = 60 * 60 * 1000;

export interface CheckIn {
    text: string;
    /** True when the frame hasn't checked in for a while (or since Hearth started). */
    stale: boolean;
}

/** "3 min ago", "2 hours ago", "4 days ago". */
export function timeAgo(ms: number): string {
    const minutes = Math.floor(ms / 60_000);
    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
    const days = Math.floor(hours / 24);
    return days === 1 ? '1 day ago' : `${days} days ago`;
}

/** One line on whether the frame is checking in and has the latest picture. */
export function checkIn(status: FrameStatus, now: Date): CheckIn {
    const poll = status.last_poll;
    if (!poll) return { text: 'The frame hasn’t checked in since Hearth last started.', stale: true };
    const age = now.getTime() - new Date(poll.at).getTime();
    const when = `The frame checked in ${timeAgo(age)}`;
    if (age > FRAME_CHECK_IN_STALE_MS) return { text: `${when}. Check its power and Wi-Fi.`, stale: true };
    if (!poll.etag) return { text: `${when}, before anything had rendered.`, stale: false };
    return poll.etag === status.etag
        ? { text: `${when} and has the latest picture.`, stale: false }
        : { text: `${when}; the latest picture reaches it on its next check.`, stale: false };
}

/** Common Inky Impression panels, by native resolution. */
export const FRAME_SIZE_PRESETS = [
    { label: 'Inky 13.3″', width: 1600, height: 1200 },
    { label: 'Inky 7.3″', width: 800, height: 480 },
    { label: 'Inky 5.7″', width: 600, height: 448 },
    { label: 'Inky 4″', width: 640, height: 400 },
] as const;

/** Whether `width`×`height` is a valid native panel size. */
export function isFrameSize(width: unknown, height: unknown): boolean {
    const ok = (n: unknown) =>
        Number.isInteger(n) && (n as number) >= MIN_FRAME_SIZE && (n as number) <= MAX_FRAME_SIZE;
    return ok(width) && ok(height) && (width as number) >= (height as number);
}

/** Clamps typed dimensions into a valid native size (rounded, in range, long edge first). */
export function normalizeFrameSize(width: number, height: number): { width: number; height: number } {
    const clamp = (n: number, fallback: number) =>
        Number.isFinite(n) ? Math.min(MAX_FRAME_SIZE, Math.max(MIN_FRAME_SIZE, Math.round(n))) : fallback;
    const a = clamp(width, DEFAULT_FRAME_SIZE.width);
    const b = clamp(height, DEFAULT_FRAME_SIZE.height);
    return { width: Math.max(a, b), height: Math.min(a, b) };
}

/** The frame canvas for a native panel size: `w`×`h` as rendered (portrait swaps
 *  them) and `scale`, the multiplier on the 1600×1200-reference type and spacing. */
export function frameCanvas(o: { width: number; height: number; orientation: 'landscape' | 'portrait' }) {
    const portrait = o.orientation === 'portrait';
    return {
        w: portrait ? o.height : o.width,
        h: portrait ? o.width : o.height,
        scale: Math.min(o.width / FRAME_REFERENCE_SIZE.width, o.height / FRAME_REFERENCE_SIZE.height),
    };
}

const FRAME_SCALE = Symbol('frame-scale');

/** Shares the frame's scale with the components below (call during init). */
export function setFrameScale(scale: () => number) {
    setContext(FRAME_SCALE, scale);
}

/** The frame's scale for px thresholds measured in JS — 1 outside the frame. */
export function getFrameScale(): () => number {
    return getContext<(() => number) | undefined>(FRAME_SCALE) ?? (() => 1);
}

export interface FrameOptions {
    mode: FrameModeId;
    palette: 'color' | 'bw';
    theme: string;
    font: string;
    /** Percent on the reading type and icons (see isFrameTextSize). */
    textSize: number;
    orientation: 'landscape' | 'portrait';
    /** The panel's native (landscape) size. */
    width: number;
    height: number;
    layout: FrameLayout;
    split: number;
    template: FrameTemplateId;
    /** The template's widget per slot, '' for an empty slot. */
    slots: readonly string[];
    position: AmbientPosition;
    /** Ambient/Photo's photo, fixed instead of a random pick (Settings' preview). */
    photo?: Photo | null;
}

/** The /frame URL for a set of options — what the live preview and the Display
 *  service's renderer load. Every option is spelled out so the URL is self-describing. */
export function frameUrl(o: FrameOptions): string {
    return `/frame?${frameParams(o)}`;
}

/** /frame's query params — also taken by the Display service's dithered preview. */
export function frameParams(o: FrameOptions): URLSearchParams {
    const params = new URLSearchParams({
        mode: o.mode,
        palette: o.palette,
        theme: o.theme,
        font: o.font,
        text: String(o.textSize),
        orientation: o.orientation,
        width: String(o.width),
        height: String(o.height),
        left: o.layout.left.join(','),
        right: o.layout.right.join(','),
        split: String(o.split),
        template: o.template,
        slots: o.slots.join(','),
        position: o.position,
    });
    if (o.photo) params.set('photo', JSON.stringify(o.photo));
    return params;
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
// Last Played is a 5-line album square plus its 3px border, whatever the column has.
const FIXED_LINES: Partial<Record<FrameWidgetId, number>> = {
    arc: 1.7,
    moon: 2.7,
    countdowns: 4.5,
    'last-played': 5.3,
};

/** Content lines (excluding its heading) each widget in a column gets. */
export function allocateLines(column: readonly FrameWidgetId[], totalLines: number): Map<FrameWidgetId, number> {
    const flexible = column.filter((id) => FIXED_LINES[id] === undefined);
    const overhead = column.length * HEADING_LINES + Math.max(0, column.length - 1) * GAP_LINES;
    const fixed = column.reduce((sum, id) => sum + (FIXED_LINES[id] ?? 0), 0);
    const share = flexible.length ? Math.max(0, totalLines - overhead - fixed) / flexible.length : 0;
    return new Map(column.map((id) => [id, FIXED_LINES[id] ?? share]));
}

/** Content lines a template slot of `totalLines` gives its one widget — all of it but
 *  the kicker, fixed-height widgets included (Last Played's L sleeve grows into it). */
export function slotLines(totalLines: number): number {
    return Math.max(0, totalLines - HEADING_LINES);
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
/**
 * `d` rounded to the nearest whole hour (local time). The frame draws the sun's position
 * at this, not the exact minute: Display re-renders Daily every 15 min, and a sun that
 * moved a few pixels each time would redraw the whole panel (~30 s of flashing) four
 * times an hour. Only the position snaps — dates still come from the real time.
 */
export function nearestHour(d: Date): Date {
    const snapped = new Date(d);
    snapped.setMinutes(d.getMinutes() >= 30 ? 60 : 0, 0, 0);
    return snapped;
}

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

/** The largest whole size in [min, max] for which `fits(size)` holds — `min` if none does.
 *  `fits` must be monotonic (true up to some size, false beyond), like "the text fits its box". */
export function fitFontSize(fits: (size: number) => boolean, min: number, max: number): number {
    let lo = Math.ceil(min);
    let hi = Math.floor(max);
    if (!fits(lo)) return lo;
    while (lo < hi) {
        const mid = Math.ceil((lo + hi) / 2);
        if (fits(mid)) lo = mid;
        else hi = mid - 1;
    }
    return lo;
}

/** The next sunrise after `now` in the forecast (Open-Meteo's local ISO times), or null. */
export function nextSunrise(forecast: ForecastDay[], now: Date): Date | null {
    return forecast.map((f) => new Date(f.sunrise)).find((sunrise) => sunrise > now) ?? null;
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

/* ── Ambient plate ink ────────────────────────────────────────────────────────
   The Ambient plate's date block is a solid pure-ink square on top of the photo.
   It stands out best in an ink the photo around it doesn't already use, so the
   frame samples the photo just outside the plate and picks, from the theme's own
   inks, the one farthest from those colours (in OKLab, so "far" means far to the eye). */

/** Where the plate sits inside an Ambient frame: mirrors FrameAmbient's .plate CSS. */
export const PLATE_INSET = 64;

export interface Rect {
    x: number;
    y: number;
    w: number;
    h: number;
}

/** The plate's box in frame pixels for an Ambient position, on a frame at `scale`. */
export function plateRect(
    position: AmbientPosition,
    plate: { w: number; h: number },
    frame: { w: number; h: number },
    scale = 1
): Rect {
    const [row, col] = position.split('-');
    const inset = PLATE_INSET * scale;
    const x = col === 'left' ? inset : col === 'right' ? frame.w - inset - plate.w : (frame.w - plate.w) / 2;
    const y = row === 'top' ? inset : frame.h - inset - plate.h;
    return { x, y, w: plate.w, h: plate.h };
}

type Oklab = [number, number, number];

/** sRGB (0–255) → OKLab (Björn Ottosson's matrices). */
export function oklab(r: number, g: number, b: number): Oklab {
    const lin = (c: number) => {
        c /= 255;
        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    };
    const [lr, lg, lb] = [lin(r), lin(g), lin(b)];
    const l = Math.cbrt(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb);
    const m = Math.cbrt(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb);
    const s = Math.cbrt(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb);
    return [
        0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s,
        1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s,
        0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s,
    ];
}

function hexRgb(hex: string): [number, number, number] {
    return [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16)) as [number, number, number];
}

function relativeLuminance(hex: string): number {
    const [r, g, b] = hexRgb(hex).map((c) => {
        c /= 255;
        return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
    });
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

/** Black or white, whichever reads better set on `ink` (WCAG contrast). */
export function onBlockInk(ink: string): '#000000' | '#ffffff' {
    const lum = relativeLuminance(ink);
    return (lum + 0.05) / 0.05 >= 1.05 / (lum + 0.05) ? '#000000' : '#ffffff';
}

/** Pixels as RGBA bytes, row-major — the shape of a canvas ImageData. */
export interface Pixels {
    width: number;
    height: number;
    data: ArrayLike<number>;
}

/** How far around the plate (frame px at scale 1) counts as its surroundings. */
const RING = 48;
/** OKLab distance at which an ink reads as clearly apart from the photo around it. */
const STANDS_OUT = 0.25;
/** Below that, how much better another ink must score before the theme's own --block gives way. */
const KEEP_PREFERRED = 0.85;

/**
 * The block ink that stands out most against the photo around `plate`. `pixels` is
 * the photo as drawn on the frame (cover-cropped) at any scale; `frame` is the frame's
 * size, so `plate` maps onto it. Each candidate scores the distance to its 10th-
 * percentile nearest sample — low when a fair patch of the surroundings matches it.
 * The theme's `preferred` ink stays while it stands out, or unless another clearly beats it.
 * `scale` is the frame's (see frameCanvas), which widens or narrows the sampled ring.
 */
export function pickBlockInk(
    pixels: Pixels,
    frame: { w: number; h: number },
    plate: Rect,
    candidates: readonly string[],
    preferred: string,
    scale = 1
): string {
    if (candidates.length === 0) return preferred;
    const margin = RING * scale;
    const sx = pixels.width / frame.w;
    const sy = pixels.height / frame.h;
    const inside = (x: number, y: number, r: Rect) => x >= r.x && x < r.x + r.w && y >= r.y && y < r.y + r.h;
    const ring = { x: plate.x - margin, y: plate.y - margin, w: plate.w + 2 * margin, h: plate.h + 2 * margin };

    const samples: Oklab[] = [];
    const x0 = Math.max(0, Math.floor(ring.x * sx));
    const x1 = Math.min(pixels.width, Math.ceil((ring.x + ring.w) * sx));
    const y0 = Math.max(0, Math.floor(ring.y * sy));
    const y1 = Math.min(pixels.height, Math.ceil((ring.y + ring.h) * sy));
    for (let py = y0; py < y1; py++) {
        for (let px = x0; px < x1; px++) {
            if (inside((px + 0.5) / sx, (py + 0.5) / sy, plate)) continue;
            const i = (py * pixels.width + px) * 4;
            samples.push(oklab(pixels.data[i], pixels.data[i + 1], pixels.data[i + 2]));
        }
    }
    if (samples.length === 0) return preferred;

    const score = (ink: string): number => {
        const [L, a, b] = oklab(...hexRgb(ink));
        const d = samples.map(([l2, a2, b2]) => Math.hypot(L - l2, a - a2, b - b2)).sort((p, q) => p - q);
        return d[Math.floor(d.length * 0.1)];
    };
    const scored = candidates.map((ink) => ({ ink, score: score(ink) }));
    const best = scored.reduce((p, q) => (q.score > p.score ? q : p));
    const kept = scored.find((s) => s.ink === preferred);
    return kept && (kept.score >= STANDS_OUT || kept.score >= best.score * KEEP_PREFERRED) ? preferred : best.ink;
}
