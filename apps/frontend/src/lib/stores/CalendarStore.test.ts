import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mockFetch } from '#lib/test/fetch.ts';
import {
    addIcsFeed,
    calendarStore,
    loadCalendarItems,
    loadCalendarStatus,
    loadIcsFeeds,
    removeIcsFeed,
} from './CalendarStore.svelte.ts';

const { respond, unreachable, call } = mockFetch();

beforeEach(() => {
    calendarStore.items = [];
    calendarStore.googleConnected = true;
    calendarStore.error = false;
    calendarStore.icsFeeds = [];
});

describe('calendar store', () => {
    it('keeps the last good items when a refresh fails', async () => {
        calendarStore.items = [{ id: 'e1' } as (typeof calendarStore.items)[number]];
        respond(503);
        await loadCalendarItems();
        expect(calendarStore.items.map((i) => i.id)).toEqual(['e1']);
    });

    it('flags an error when a failed load leaves no items, not for an empty calendar', async () => {
        respond(200, []);
        await loadCalendarItems();
        expect(calendarStore.error).toBe(false);

        unreachable();
        await loadCalendarItems();
        expect(calendarStore.error).toBe(true);
    });

    it('marks the items loaded after the first attempt, even a failed one', async () => {
        calendarStore.loaded = false;
        unreachable();
        await loadCalendarItems();
        expect(calendarStore.loaded).toBe(true);

        calendarStore.loaded = false;
        respond(200, []);
        await loadCalendarItems();
        expect(calendarStore.loaded).toBe(true);
    });

    it('keeps the connection status when the check fails', async () => {
        unreachable();
        await loadCalendarStatus();
        expect(calendarStore.googleConnected).toBe(true);
    });

    it('keeps the subscribed calendars when the list can’t be read', async () => {
        calendarStore.icsFeeds = [{ id: 'f1', name: 'Family', host: 'example.com' }];
        unreachable();
        await loadIcsFeeds();
        expect(calendarStore.icsFeeds).toHaveLength(1);
    });

    it('adds a subscribed calendar and reloads the items', async () => {
        respond(201, { id: 'f1', name: 'Family', host: 'p01-caldav.icloud.com' });
        respond(200, [{ id: 'e1' }]);

        expect(await addIcsFeed('Family', 'webcal://p01-caldav.icloud.com/published/2/abc')).toBeNull();

        expect(call(0).url).toBe('/calendar/ics/feeds');
        expect(JSON.parse(String(call(0).init?.body))).toEqual({
            name: 'Family',
            url: 'webcal://p01-caldav.icloud.com/published/2/abc',
        });
        expect(calendarStore.icsFeeds.map((f) => f.name)).toEqual(['Family']);
        await vi.waitFor(() => expect(calendarStore.items.map((i) => i.id)).toEqual(['e1']));
    });

    it('returns the service’s reason when a link is refused', async () => {
        respond(400, { error: 'That link didn’t return a calendar (an .ics file).' });
        expect(await addIcsFeed('', 'https://example.com/')).toBe('That link didn’t return a calendar (an .ics file).');
        expect(calendarStore.icsFeeds).toEqual([]);
    });

    it('says so when the service can’t be reached while adding', async () => {
        unreachable();
        expect(await addIcsFeed('', 'https://example.com/a.ics')).toBe('Couldn’t reach the calendar service.');
    });

    it('removes a subscribed calendar only once the service has', async () => {
        calendarStore.icsFeeds = [{ id: 'f1', name: 'Family', host: 'example.com' }];
        respond(500);
        await removeIcsFeed('f1');
        expect(calendarStore.icsFeeds).toHaveLength(1);

        respond(204);
        respond(200, []);
        await removeIcsFeed('f1');
        expect(calendarStore.icsFeeds).toEqual([]);
    });
});
