import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Task } from '#lib/api.ts';
import { loadTasks, removeTask, taskStore } from './TaskStore.svelte.ts';
import { calendarStore, loadCalendarItems, loadCalendarStatus } from './CalendarStore.svelte.ts';

const task = (id: number): Task => ({
    id,
    title: `Task ${id}`,
    done: false,
    due_date: null,
    due_time: null,
    created_at: '2026-10-01T00:00:00',
    description: null,
    assignee: null,
    recurrence_unit: null,
    recurrence_interval: null,
    recurrence_days: null,
    recurrence_end_date: null,
    series_id: null,
    is_countdown: false,
});

const fetchMock = vi.fn<typeof fetch>();
const respond = (status: number, body?: unknown) =>
    fetchMock.mockResolvedValueOnce(new Response(body === undefined ? null : JSON.stringify(body), { status }));

beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock);
    taskStore.tasks = [task(1), task(2)];
    calendarStore.items = [];
    calendarStore.googleConnected = true;
});

afterEach(() => {
    fetchMock.mockReset();
    vi.unstubAllGlobals();
});

describe('loadTasks', () => {
    it('replaces the list on success', async () => {
        respond(200, [task(3)]);
        await loadTasks();
        expect(taskStore.tasks.map((t) => t.id)).toEqual([3]);
    });

    it('keeps the last good list when the service errors', async () => {
        respond(502);
        await loadTasks();
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
    });

    it('keeps the last good list when the network is down', async () => {
        fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
        await expect(loadTasks()).resolves.toBeUndefined();
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
    });
});

describe('removeTask', () => {
    it('drops the task once the server deletes it', async () => {
        respond(204);
        await removeTask(1);
        expect(taskStore.tasks.map((t) => t.id)).toEqual([2]);
    });

    it('keeps the task when the delete fails', async () => {
        respond(500);
        await removeTask(1);
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
    });
});

describe('calendar store', () => {
    it('keeps the last good items when a refresh fails', async () => {
        calendarStore.items = [{ id: 'e1' } as (typeof calendarStore.items)[number]];
        respond(503);
        await loadCalendarItems();
        expect(calendarStore.items.map((i) => i.id)).toEqual(['e1']);
    });

    it('keeps the connection status when the check fails', async () => {
        fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
        await loadCalendarStatus();
        expect(calendarStore.googleConnected).toBe(true);
    });
});
