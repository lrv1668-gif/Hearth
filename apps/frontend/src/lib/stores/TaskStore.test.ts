import { beforeEach, describe, expect, it } from 'vitest';
import { mockFetch } from '$lib/test/fetch';
import { task } from '$lib/test/fixtures';
import { loadTasks, removeTask, taskStore } from './TaskStore.svelte.ts';

const { respond, unreachable } = mockFetch();

beforeEach(() => {
    taskStore.tasks = [task({ id: 1 }), task({ id: 2 })];
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

    it('keeps the task when the delete fails', async () => {
        respond(500);
        await removeTask(1);
        expect(taskStore.tasks.map((t) => t.id)).toEqual([1, 2]);
    });
});
