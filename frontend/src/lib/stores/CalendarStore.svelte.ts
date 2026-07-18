import { api, type CalendarItem } from '$lib/api';

interface CalendarStoreState {
    items: CalendarItem[];
    googleConnected: boolean;
}

export const calendarStore = $state<CalendarStoreState>({
    items: [],
    googleConnected: false,
});

export async function loadCalendarStatus(): Promise<void> {
    const status = await api.calendar.googleStatus();
    calendarStore.googleConnected = status?.authenticated ?? false;
}

export async function loadCalendarItems(): Promise<void> {
    calendarStore.items = await api.calendar.items();
}

export async function refreshCalendarItems(): Promise<void> {
    await api.calendar.refreshCache();
    await loadCalendarItems();
}

export async function toggleCalendarTask(
    taskListId: string,
    taskId: string,
    completed: boolean
): Promise<void> {
    const prev = calendarStore.items;
    // Optimistic update
    calendarStore.items = calendarStore.items.map((i) =>
        i.id === taskId ? { ...i, is_completed: completed } : i
    );
    const ok = await api.calendar.toggleTask(taskListId, taskId, completed);
    if (!ok) {
        calendarStore.items = prev;
    }
}
