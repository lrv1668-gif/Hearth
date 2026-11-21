import { del, get, patch, sendJson, tryFetch } from './http.ts';
import type { Task } from './tasks.ts';

export interface CalendarItem {
    kind: 'event' | 'task'; // "event" = Google Calendar event; "task" = Google Task
    id: string;
    title: string;
    description?: string | null;
    location?: string | null;
    start?: string | null; // ISO 8601 with offset, "YYYY-MM-DD" for all-day/tasks, or null (undated task)
    end?: string | null; // null for tasks
    is_all_day: boolean;
    calendar_name?: string | null;
    provider: string; // "google" | "ics" (a calendar subscribed to by link)
    is_completed?: boolean | null; // null for events; true/false for tasks
    task_list_id?: string | null; // null for events; required for toggle endpoint
    html_link?: string | null; // direct URL to view in provider
}

/** A calendar subscribed to by link (Settings → Connections). The link itself stays in the
 *  Calendar service — only its host comes back. */
export interface IcsFeed {
    id: string;
    name: string;
    host: string;
}

// Discriminated union shared by Calendar.svelte, DayOverflowModal.svelte, and UpcomingTasksWidget.svelte.
// kind: 'task' = internal Hearth task; kind: 'event' = external calendar item (CalendarItem.kind determines event vs task).
export type Item = { kind: 'task'; data: Task } | { kind: 'event'; data: CalendarItem };

export const calendar = {
    googleStatus: (): Promise<{ authenticated: boolean } | null> => get('/calendar/google/status'),

    googleDisconnect: (): Promise<boolean> => del('/calendar/google/auth'),

    items: (): Promise<CalendarItem[] | null> => get<CalendarItem[]>('/calendar/items'),

    toggleTask: (taskListId: string, taskId: string, completed: boolean): Promise<boolean> =>
        patch(`/calendar/google/tasks/${encodeURIComponent(taskListId)}/${encodeURIComponent(taskId)}`, {
            completed,
        }),

    /** Makes the next `items()` ask every provider again instead of serving cached copies. */
    refreshCache: async (): Promise<boolean> => {
        const res = await tryFetch('/calendar/items/refresh', { method: 'POST' });
        return res?.ok ?? false;
    },

    icsFeeds: (): Promise<IcsFeed[] | null> => get<IcsFeed[]>('/calendar/ics/feeds'),

    /** Subscribes to a calendar link (webcal:// or https://). The service reads the link
     *  first, so `error` is its reason when it refuses — a wrong or private link. */
    addIcsFeed: async (name: string, url: string): Promise<{ feed: IcsFeed } | { error: string }> => {
        const res = await sendJson('POST', '/calendar/ics/feeds', { name, url }).catch(() => null);
        const body = (await res?.json().catch(() => null)) as (IcsFeed & { error?: string }) | null;
        if (res?.ok && body) return { feed: body };
        return { error: body?.error ?? 'Couldn’t reach the calendar service.' };
    },

    removeIcsFeed: (id: string): Promise<boolean> => del(`/calendar/ics/feeds/${encodeURIComponent(id)}`),
};
