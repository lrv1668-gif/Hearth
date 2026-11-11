import { beforeEach, describe, expect, it } from 'vitest';
import { mockFetch } from '#lib/test/fetch.ts';
import { task } from '#lib/test/fixtures.ts';
import { addTask, editTask, loadTasks, removeTask, taskStore, toggleTask } from './TaskStore.svelte.ts';

const { respond, unreachable, call } = mockFetch();

beforeEach(() => {
    taskStore.tasks = [task({ id: 1 }), task({ id: 2 })];
    taskStore.error = false;
    taskStore.saveError = false;
});

describe('loadTasks', () => {
    it('replaces the list on success', async () => {
        respond(200, [task({ id: 3 })]);
        await loadTasks();
        expect(taskStore.tasks.map((t) => t.id)).toEqual([3]);
    });

    it('keeps the last good list when the service errors', async () => {
        respond(502);
        await loadTasks();
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
    });

    it('flags an error only when a failed load leaves nothing to show', async () => {
        respond(502);
        await loadTasks();
        expect(taskStore.error).toBe(false);

        taskStore.tasks = [];
        respond(502);
        await loadTasks();
        expect(taskStore.error).toBe(true);

        respond(200, []);
        await loadTasks();
        expect(taskStore.error).toBe(false);
    });

    it('marks the store loaded after the first attempt, even a failed one', async () => {
        taskStore.loaded = false;
        unreachable();
        await loadTasks();
        expect(taskStore.loaded).toBe(true);

        taskStore.loaded = false;
        respond(200, []);
        await loadTasks();
        expect(taskStore.loaded).toBe(true);
    });

    it('keeps the last good list when the network is down', async () => {
        unreachable();
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

    it('keeps the task and flags the failure when the delete fails', async () => {
        respond(500);
        await removeTask(1);
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
        expect(taskStore.saveError).toBe(true);
    });

    it('reloads the list after deleting a series', async () => {
        respond(204);
        respond(200, [task({ id: 2 })]);
        await removeTask(1, true);
        expect(taskStore.tasks.map((t) => t.id)).toEqual([2]);
        expect(taskStore.saveError).toBe(false);
    });
});

describe('toggleTask', () => {
    it('swaps in the saved task', async () => {
        respond(200, task({ id: 1, done: true }));
        await toggleTask(taskStore.tasks[0]);
        expect(taskStore.tasks[0].done).toBe(true);
        expect(taskStore.saveError).toBe(false);
    });

    it('leaves the task as it was and flags the failure when the service errors', async () => {
        respond(500);
        await expect(toggleTask(taskStore.tasks[0])).resolves.toBeUndefined();
        expect(taskStore.tasks[0].done).toBe(false);
        expect(taskStore.saveError).toBe(true);
    });

    it('flags the failure when the network is down, and clears it on the next save', async () => {
        unreachable();
        await toggleTask(taskStore.tasks[0]);
        expect(taskStore.saveError).toBe(true);

        respond(200, task({ id: 1, done: true }));
        await toggleTask(taskStore.tasks[0]);
        expect(taskStore.saveError).toBe(false);
    });
});

describe('editTask', () => {
    it('sends cleared fields as null and keeps the done state', async () => {
        respond(200, task({ id: 1, title: 'Renamed' }));
        await editTask(taskStore.tasks[0], 'Renamed');
        expect(JSON.parse(String(call().init?.body))).toEqual({
            done: false,
            title: 'Renamed',
            due_date: null,
            due_time: null,
            description: null,
            assignee: null,
        });
        expect(taskStore.tasks[0].title).toBe('Renamed');
    });

    it('keeps the old task and flags the failure when the service refuses it', async () => {
        respond(400);
        await editTask(taskStore.tasks[0], 'Renamed');
        expect(taskStore.tasks[0].title).not.toBe('Renamed');
        expect(taskStore.saveError).toBe(true);
    });
});

describe('addTask', () => {
    it('puts a one-off task at the top of the list', async () => {
        respond(201, task({ id: 3 }));
        await addTask({ title: 'New' });
        expect(taskStore.tasks.map((t) => t.id)).toEqual([3, 1, 2]);
    });

    it('reloads after adding a repeating task, to pick up its other instances', async () => {
        respond(201, task({ id: 3, series_id: 3 }));
        respond(200, [task({ id: 3, series_id: 3 }), task({ id: 4, series_id: 3 })]);
        await addTask({ title: 'Bins', due_date: '2030-01-01', recurrence_unit: 'week', recurrence_interval: 1 });
        expect(taskStore.tasks.map((t) => t.id)).toEqual([3, 4]);
    });

    it('adds nothing and flags the failure when the service refuses it', async () => {
        respond(400);
        await addTask({ title: 'New' });
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
        expect(taskStore.saveError).toBe(true);
    });
});
