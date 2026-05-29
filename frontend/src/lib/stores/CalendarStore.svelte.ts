import { api, type CalendarEvent } from '$lib/api';

interface CalendarStoreState {
    events: CalendarEvent[];
    googleConnected: boolean;
}

export const calendarStore = $state<CalendarStoreState>({
    events: [],
    googleConnected: false,
});

export async function loadCalendarStatus(): Promise<void> {
    const status = await api.calendar.googleStatus();
    calendarStore.googleConnected = status?.authenticated ?? false;
}

export async function loadCalendarEvents(): Promise<void> {
    calendarStore.events = await api.calendar.events();
}
