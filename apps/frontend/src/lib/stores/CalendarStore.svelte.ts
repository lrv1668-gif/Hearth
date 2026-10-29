import { api, type CalendarItem } from '$lib/api';

interface CalendarStoreState {
    items: CalendarItem[];
    googleConnected: boolean;
}

export const calendarStore = $state<CalendarStoreState>({
    items: [],
    googleConnected: false,
});

// A failed check or refresh keeps what's already known
export async function loadCalendarStatus(): Promise<void> {
    const status = await api.calendar.googleStatus();
    if (status) calendarStore.googleConnected = status.authenticated;
}

export async function loadCalendarItems(): Promise<void> {
    const items = await api.calendar.items();
    if (items) calendarStore.items = items;
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
