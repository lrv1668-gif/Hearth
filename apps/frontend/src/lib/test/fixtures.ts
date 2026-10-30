import type { Task } from '#lib/api.ts';

/** A pending, untimed, one-off task due Monday 19 October 2026, with `overrides` on top. */
export function task(overrides: Partial<Task> = {}): Task {
    return {
        id: 1,
        title: 'Task',
        done: false,
        due_date: '2026-10-19',
        due_time: null,
        created_at: '2026-10-01T00:00:00Z',
        description: null,
        assignee: null,
        recurrence_unit: null,
        recurrence_interval: null,
        recurrence_days: null,
        recurrence_end_date: null,
        series_id: null,
        is_countdown: false,
        ...overrides,
    };
}
