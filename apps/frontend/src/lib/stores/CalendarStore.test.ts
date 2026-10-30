import { beforeEach, describe, expect, it } from 'vitest';
import { mockFetch } from '#lib/test/fetch.ts';
import { calendarStore, loadCalendarItems, loadCalendarStatus } from './CalendarStore.svelte.ts';

const { respond, unreachable } = mockFetch();

beforeEach(() => {
    calendarStore.items = [];
    calendarStore.googleConnected = true;
    calendarStore.error = false;
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

    it('keeps the connection status when the check fails', async () => {
        unreachable();
        await loadCalendarStatus();
        expect(calendarStore.googleConnected).toBe(true);
    });
});
