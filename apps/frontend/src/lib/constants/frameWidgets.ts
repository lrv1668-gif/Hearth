/* Widgets that can fill the e-paper frame's Daily-mode columns. The frame redraws
   on a schedule, not live, so only widgets that stay correct for an hour or more
   belong here — no trains, no now-playing (Last Played shows the listening history
   instead, cached for an hour so it doesn't redraw the panel every track), no live
   scores (Last Result & Next Game shows a game in progress as "Playing now"); Photo holds one
   photo for the whole day. The edition bar, masthead (date block,
   weather and the sun path) and the forecast band are fixed and not listed. */
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
    { id: 'last-played', label: 'Last Played', description: 'The latest track from Spotify, with its album art' },
    { id: 'sports', label: 'Last Result & Next Game', description: 'Last result and next game for teams you follow' },
    { id: 'photo', label: 'Photo', description: 'A photo of the day from your Ambient photos' },
] as const;

export type FrameWidgetId = (typeof frameWidgets)[number]['id'];

export interface FrameLayout {
    left: FrameWidgetId[];
    right: FrameWidgetId[];
}

// Always shown on the frame, outside the columns.
export const fixedFrameHeader = [{ label: 'Edition, Date & Sun Path' }] as const;
export const fixedFrameFooter = [{ label: 'Conditions & 5-Day Forecast' }] as const;

/** Widgets per column. Each widget's item count shrinks to fit what shares its
 *  column (see allocateLines in lib/frame.ts); three is the most that stays legible. */
export const MAX_COLUMN_WIDGETS = 3;

// The masthead's sun path already shows the day's light, so the Day Arc is off by default.
export const DEFAULT_FRAME_LAYOUT: FrameLayout = {
    left: ['countdowns', 'today'],
    right: ['coming-up'],
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

/** Reads `?left=a,b&right=c&split=55` from the frame URL. A missing param falls
 *  back to `fallback` (the saved Settings → Frame layout in a browser, else the
 *  default); an empty one (`left=`) means an intentionally empty column. */
export function parseFrameLayout(
    params: Pick<URLSearchParams, 'get'>,
    fallback: FrameLayout & { split: number } = { ...DEFAULT_FRAME_LAYOUT, split: DEFAULT_FRAME_SPLIT }
): FrameLayout & { split: number } {
    const column = (key: 'left' | 'right') => {
        const raw = params.get(key);
        return raw === null ? fallback[key] : raw.split(',').filter(Boolean);
    };
    const split = params.get('split');
    return {
        ...normalizeFrameLayout({ left: column('left'), right: column('right') }),
        split: clampSplit(split === null ? fallback.split : Number(split)),
    };
}
