import { describe, expect, it } from 'vitest';
import { mockFetch } from '#lib/test/fetch.ts';
import { fromSaved, household, loadHousehold, parseLegacy, toSaved } from './HouseholdStore.svelte.ts';

const { respond, unreachable } = mockFetch();

describe('parseLegacy', () => {
    it('keeps a snapshot with feeds or stops', () => {
        const raw = JSON.stringify({ rssFeeds: [{ title: 'BBC', url: 'https://bbc.example/rss' }], trainStops: [] });
        expect(parseLegacy(raw)).toEqual({
            rssFeeds: [{ title: 'BBC', url: 'https://bbc.example/rss' }],
            trainStops: [],
        });
    });

    it.each([null, '', '{}', '{"rssFeeds":[],"trainStops":[]}', 'not json'])('has nothing to move for %s', (raw) => {
        expect(parseLegacy(raw)).toBeNull();
    });
});

describe('toSaved / fromSaved', () => {
    it('round-trips stops and their line filters through Display’s snake_case', () => {
        const lists = {
            rssFeeds: [{ title: 'BBC', url: 'https://bbc.example/rss' }],
            trainStops: [
                { label: 'Home', stopKey: 's-1', lineFilter: ['Red|Richmond|rail'] },
                { label: 'Work', stopKey: 's-2' },
                { label: 'Hidden', stopKey: 's-3', lineFilter: [] },
            ],
        };
        const saved = toSaved(lists);
        expect(saved.train_stops[0]).toEqual({ label: 'Home', stop_key: 's-1', line_filter: ['Red|Richmond|rail'] });
        expect(saved.train_stops[1].line_filter).toBeNull();
        expect(fromSaved(saved)).toEqual(lists);
    });
});

describe('loadHousehold', () => {
    it('applies the saved lists', async () => {
        respond(200, { rss_feeds: [{ title: 'BBC', url: 'https://bbc.example/rss' }], train_stops: [] });
        await loadHousehold();
        expect(household.rssFeeds).toEqual([{ title: 'BBC', url: 'https://bbc.example/rss' }]);
        expect(household.error).toBe(false);
        expect(household.loaded).toBe(true);
    });

    it('flags an unreachable Display so nothing is saved over the real lists', async () => {
        unreachable();
        await loadHousehold();
        expect(household.error).toBe(true);
    });
});
