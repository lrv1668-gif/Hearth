import { beforeEach, describe, expect, it } from 'vitest';
import type { ModeSchedule, ModeTimeline, ResolvedMode } from '#lib/api.ts';
import { DEFAULT_FRAME_MODE } from '#lib/constants/frameModes.ts';
import { mockFetch } from '#lib/test/fetch.ts';
import { clearOverride, loadModes, modeStore, setPalette } from './ModeStore.svelte.ts';

const scheduled: ResolvedMode = { mode: 'daily', reason: 'schedule', until: '2026-11-09T22:00:00Z', photo_id: null };
const pinned: ResolvedMode = { mode: 'photo', reason: 'override', until: null, photo_id: 'p1' };
const schedule: ModeSchedule = { default_mode: 'daily', rules: [] };
const day: ModeTimeline = { date: '2026-11-09', spans: [] };

const { fetchMock, call } = mockFetch();

/** Answers each Display endpoint by path, so the order of loadModes' parallel calls doesn't matter. */
function display(routes: Record<string, [number, unknown?]>) {
    fetchMock.mockImplementation(async (input, init) => {
        const url = new URL(String(input), 'http://hearth');
        const key = `${init?.method ?? 'GET'} ${url.pathname}`;
        const route = routes[key];
        if (!route) throw new TypeError(`unreachable: ${key}`);
        const [status, body] = route;
        return new Response(body === undefined ? null : JSON.stringify(body), { status });
    });
}

beforeEach(() => {
    modeStore.current = null;
    modeStore.schedule = { default_mode: DEFAULT_FRAME_MODE, rules: [] };
    modeStore.day = null;
    modeStore.palette = 'color';
    modeStore.loaded = false;
    modeStore.error = false;
});

describe('loadModes', () => {
    it('fills the store from Display', async () => {
        display({
            'GET /display/mode': [200, scheduled],
            'GET /display/mode/schedule': [200, schedule],
            'GET /display/palette': [200, { palette: 'bw' }],
            'GET /display/mode/day': [200, day],
        });
        await loadModes();
        expect(modeStore).toMatchObject({
            current: scheduled,
            schedule,
            palette: 'bw',
            day,
            loaded: true,
            error: false,
        });
    });

    it('keeps what it had and flags an error when Display is unreachable', async () => {
        modeStore.current = scheduled;
        modeStore.palette = 'bw';
        display({});
        await loadModes();
        expect(modeStore).toMatchObject({ current: scheduled, palette: 'bw', loaded: true, error: true });
    });
});

describe('setPalette', () => {
    it('takes the palette Display saved', async () => {
        display({ 'PUT /display/palette': [200, { palette: 'bw' }] });
        await setPalette('bw');
        expect(modeStore.palette).toBe('bw');
        expect(modeStore.error).toBe(false);
    });

    it('puts the old palette back when the save fails', async () => {
        display({ 'PUT /display/palette': [500] });
        await setPalette('bw');
        expect(modeStore.palette).toBe('color');
        expect(modeStore.error).toBe(true);
    });
});

describe('clearOverride', () => {
    it('sends the photo id and refreshes what the frame shows', async () => {
        modeStore.current = pinned;
        display({ 'DELETE /display/mode/override': [204], 'GET /display/mode': [200, scheduled] });
        await clearOverride('p1');
        expect(call(0).url).toBe('/display/mode/override?photo_id=p1');
        expect(modeStore.current).toEqual(scheduled);
        expect(modeStore.error).toBe(false);
    });

    it('flags an error when Display refuses, but still refreshes', async () => {
        modeStore.current = pinned;
        display({ 'DELETE /display/mode/override': [500], 'GET /display/mode': [200, pinned] });
        await clearOverride();
        expect(modeStore.error).toBe(true);
        expect(fetchMock).toHaveBeenCalledTimes(2);
    });
});

describe('pinnedPhotoId', () => {
    it('is the photo of a "Show on frame" override only', () => {
        modeStore.current = pinned;
        expect(modeStore.pinnedPhotoId).toBe('p1');
        modeStore.current = { ...pinned, reason: 'schedule' };
        expect(modeStore.pinnedPhotoId).toBeNull();
        modeStore.current = null;
        expect(modeStore.pinnedPhotoId).toBeNull();
    });
});
