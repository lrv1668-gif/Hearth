/* Widgets that can fill the e-paper frame's Daily-mode columns. The frame redraws
   on a schedule, not live, so only widgets that stay correct for an hour or more
   belong here — no trains, no now-playing. The masthead (date + weather) and the
   forecast band are fixed and not listed. */
export const frameWidgets = [
    { id: 'arc', label: "Today's Arc", description: 'Daylight, golden hours and today’s events as a ribbon' },
    { id: 'today', label: 'Today', description: 'Tasks due today' },
    { id: 'coming-up', label: 'Coming Up', description: 'Next calendar events' },
    { id: 'week', label: 'Week Ahead', description: 'Events and tasks for the next 7 days' },
    { id: 'countdowns', label: 'Countdowns', description: 'Days until upcoming dates' },
    { id: 'almanac', label: 'Almanac', description: 'Season, daylight trend and frost' },
    { id: 'moon', label: 'Moon Phase', description: 'Tonight’s moon and the next phase' },
    { id: 'quote', label: 'Daily Quote', description: 'One quote a day' },
    { id: 'birds', label: 'Birds Nearby', description: 'Recent sightings from eBird' },
] as const;

export type FrameWidgetId = (typeof frameWidgets)[number]['id'];

export interface FrameLayout {
    left: FrameWidgetId[];
    right: FrameWidgetId[];
}

// Always shown on the frame, outside the columns.
export const fixedFrameHeader = [{ label: 'Date & Weather' }] as const;
export const fixedFrameFooter = [{ label: '5-Day Forecast' }] as const;

/** Widgets per column. Each widget's item count shrinks to fit what shares its
 *  column (see allocateLines in lib/frame.ts); three is the most that stays legible. */
export const MAX_COLUMN_WIDGETS = 3;

export const DEFAULT_FRAME_LAYOUT: FrameLayout = {
    left: ['arc', 'today'],
    right: ['coming-up', 'countdowns'],
};

/** Left column's share of the width, in percent — same 30–70 range as the dashboard. */
export const DEFAULT_FRAME_SPLIT = 50;
export const MIN_SPLIT = 30;
export const MAX_SPLIT = 70;

export function isValidFrameWidgetId(value: string): value is FrameWidgetId {
    return frameWidgets.some((w) => w.id === value);
}

/** Drops unknown ids, de-duplicates across both columns (left wins) and caps each
 *  column — so a hand-edited URL or stale saved setting still renders sanely. */
export function normalizeFrameLayout(layout: { left: readonly string[]; right: readonly string[] }): FrameLayout {
    const seen = new Set<string>();
    const clean = (ids: readonly string[]) => {
        const out: FrameWidgetId[] = [];
        for (const id of ids) {
            if (out.length === MAX_COLUMN_WIDGETS) break;
            if (!isValidFrameWidgetId(id) || seen.has(id)) continue;
            seen.add(id);
            out.push(id);
        }
        return out;
    };
    const left = clean(layout.left);
    return { left, right: clean(layout.right) };
}

export function clampSplit(value: number): number {
    return Number.isFinite(value) ? Math.min(MAX_SPLIT, Math.max(MIN_SPLIT, Math.round(value))) : DEFAULT_FRAME_SPLIT;
}

/** Reads `?left=a,b&right=c&split=55` from the frame URL. A missing column param
 *  falls back to the default for that column; an empty one (`left=`) means an
 *  intentionally empty column. */
export function parseFrameLayout(params: URLSearchParams): FrameLayout & { split: number } {
    const column = (key: 'left' | 'right') => {
        const raw = params.get(key);
        return raw === null ? DEFAULT_FRAME_LAYOUT[key] : raw.split(',').filter(Boolean);
    };
    const split = params.get('split');
    return {
        ...normalizeFrameLayout({ left: column('left'), right: column('right') }),
        split: split === null ? DEFAULT_FRAME_SPLIT : clampSplit(Number(split)),
    };
}
