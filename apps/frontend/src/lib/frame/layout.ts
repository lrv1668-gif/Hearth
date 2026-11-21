// Fitting content to the frame: the canvas scale, line budgets for columns and
// slots, and type that grows to fill its box.
import type { FrameWidgetId } from '#lib/constants/frameWidgets.ts';
import { getContext, setContext } from 'svelte';

const FRAME_SCALE = Symbol('frame-scale');

/** Shares the frame's scale with the components below (call during init). */
export function setFrameScale(scale: () => number) {
    setContext(FRAME_SCALE, scale);
}

/** The frame's scale for px thresholds measured in JS — 1 outside the frame. */
export function getFrameScale(): () => number {
    return getContext<(() => number) | undefined>(FRAME_SCALE) ?? (() => 1);
}

/* ── Column space ─────────────────────────────────────────────────────────────
   Frame widgets size their lists in "lines": one line of frame body text (48px).
   FrameDaily measures the real column height and body line height at render
   time (so font presets are accounted for); COLUMN_LINES is only the fallback —
   ~15 lines landscape, ~24 portrait at the default font, below the masthead. Every widget pays for its kicker, gaps sit between widgets, and
   fixed-height widgets (the arc, the moon, the countdown stamps) take what they
   need — the rest is split evenly between the widgets whose lists can grow.   */
export const COLUMN_LINES = { landscape: 15, portrait: 24 } as const;
const HEADING_LINES = 1.05;
const GAP_LINES = 0.85;
// Last Played is a 5-line album square plus its 3px border, whatever the column has.
const FIXED_LINES: Partial<Record<FrameWidgetId, number>> = {
    arc: 1.7,
    moon: 2.7,
    countdowns: 4.5,
    'last-played': 5.3,
    photo: 7.4,
    // One event (year, three lines of text) and the Wikipedia credit
    'on-this-day': 6.2,
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
