import { api, type CalendarItem, type IcsFeed } from '#lib/api.ts';

interface CalendarStoreState {
    items: CalendarItem[];
    googleConnected: boolean;
    /** Calendars subscribed to by link, oldest first. */
    icsFeeds: IcsFeed[];
    /** The Calendar service couldn't be reached and there are no items to show. */
    error: boolean;
    /** The first items load has finished, successfully or not. */
    loaded: boolean;
}

export const calendarStore = $state<CalendarStoreState>({
    items: [],
    googleConnected: false,
    icsFeeds: [],
    error: false,
    loaded: false,
});

// A failed check or refresh keeps what's already known
export async function loadCalendarStatus(): Promise<void> {
    const status = await api.calendar.googleStatus();
    if (status) calendarStore.googleConnected = status.authenticated;
}

export async function loadIcsFeeds(): Promise<void> {
    const feeds = await api.calendar.icsFeeds();
    if (feeds) calendarStore.icsFeeds = feeds;
}

/** Subscribes to a calendar link; its events load in the background. Returns the service's
 *  reason when it refuses. */
export async function addIcsFeed(name: string, url: string): Promise<string | null> {
    const result = await api.calendar.addIcsFeed(name, url);
    if ('error' in result) return result.error;
    calendarStore.icsFeeds = [...calendarStore.icsFeeds, result.feed];
    void loadCalendarItems();
    return null;
}

export async function removeIcsFeed(id: string): Promise<void> {
    if (!(await api.calendar.removeIcsFeed(id))) return;
    calendarStore.icsFeeds = calendarStore.icsFeeds.filter((f) => f.id !== id);
    await loadCalendarItems();
}

export async function loadCalendarItems(): Promise<void> {
    const items = await api.calendar.items();
    calendarStore.loaded = true;
    if (!items) {
        calendarStore.error = calendarStore.items.length === 0;
        return;
    }
    calendarStore.error = false;
    calendarStore.items = items;
}

export async function refreshCalendarItems(): Promise<void> {
    await api.calendar.refreshCache();
    await loadCalendarItems();
}

export async function toggleCalendarTask(taskListId: string, taskId: string, completed: boolean): Promise<void> {
    const prev = calendarStore.items;
    // Optimistic update
    calendarStore.items = calendarStore.items.map((i) => (i.id === taskId ? { ...i, is_completed: completed } : i));
    const ok = await api.calendar.toggleTask(taskListId, taskId, completed);
    if (!ok) {
        calendarStore.items = prev;
    }
}
