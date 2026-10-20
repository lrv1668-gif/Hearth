import { describe, expect, it } from 'vitest';
import type { CalendarItem, Task } from './api';
import { frameUrl, nextEvents, relativeDayLabel, todayTasks } from './frame';

const now = new Date('2026-10-19T12:00');

function task(overrides: Partial<Task>): Task {
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

function event(overrides: Partial<CalendarItem>): CalendarItem {
    return { kind: 'event', id: 'e', title: 'Event', is_all_day: false, provider: 'google', ...overrides };
}

describe('todayTasks', () => {
    it('keeps only pending tasks due today', () => {
        const tasks = [
            task({ id: 1, due_date: '2026-10-19' }),
            task({ id: 2, due_date: '2026-10-17' }),
            task({ id: 3, due_date: '2026-10-20' }),
            task({ id: 4, due_date: null }),
        ];
        expect(todayTasks(tasks, now).map((t) => t.id)).toEqual([1]);
    });

    it('accepts the API date-time form of due_date', () => {
        const tasks = [
            task({ id: 1, due_date: '2026-10-19T00:00:00' }),
            task({ id: 2, due_date: '2026-10-20T00:00:00' }),
        ];
        expect(todayTasks(tasks, now).map((t) => t.id)).toEqual([1]);
    });

    it('drops done tasks and countdowns', () => {
        const tasks = [task({ id: 1, done: true }), task({ id: 2, is_countdown: true })];
        expect(todayTasks(tasks, now)).toEqual([]);
    });

    it('orders timed tasks before untimed ones on the same day', () => {
        const tasks = [
            task({ id: 1, due_time: null }),
            task({ id: 2, due_time: '15:00' }),
            task({ id: 3, due_time: '09:30' }),
        ];
        expect(todayTasks(tasks, now).map((t) => t.id)).toEqual([3, 2, 1]);
    });
});

describe('nextEvents', () => {
    it('keeps in-progress and upcoming events, soonest first', () => {
        const items = [
            event({ id: 'later', start: '2026-10-19T18:00', end: '2026-10-19T19:00' }),
            event({ id: 'ongoing', start: '2026-10-19T11:00', end: '2026-10-19T13:00' }),
            event({ id: 'past', start: '2026-10-19T08:00', end: '2026-10-19T09:00' }),
        ];
        expect(nextEvents(items, now, 5).map((e) => e.id)).toEqual(['ongoing', 'later']);
    });

    it("keeps today's all-day events and sorts them at midnight", () => {
        const items = [
            event({ id: 'timed', start: '2026-10-19T15:00', end: '2026-10-19T16:00' }),
            event({ id: 'allday', start: '2026-10-19', is_all_day: true }),
            event({ id: 'yesterday', start: '2026-10-18', is_all_day: true }),
        ];
        expect(nextEvents(items, now, 5).map((e) => e.id)).toEqual(['allday', 'timed']);
    });

    it('excludes Google Tasks and limits the count', () => {
        const items = [
            event({ id: 'task', kind: 'task', start: '2026-10-19' }),
            event({ id: 'a', start: '2026-10-20T09:00' }),
            event({ id: 'b', start: '2026-10-21T09:00' }),
        ];
        expect(nextEvents(items, now, 1).map((e) => e.id)).toEqual(['a']);
    });
});

describe('relativeDayLabel', () => {
    it('labels today and tomorrow', () => {
        expect(relativeDayLabel(event({ start: '2026-10-19T15:00' }), now)).toBe('Today');
        expect(relativeDayLabel(event({ start: '2026-10-20', is_all_day: true }), now)).toBe('Tomorrow');
    });

    it('falls back to a short date', () => {
        expect(relativeDayLabel(event({ start: '2026-10-23T09:00' }), now)).toBe('Fri, Oct 23');
    });
});

describe('frameUrl', () => {
    it('spells out every option', () => {
        expect(frameUrl({ mode: 'daily', palette: 'bw', theme: 'night', font: 'lora', orientation: 'portrait' })).toBe(
            '/frame?mode=daily&palette=bw&theme=night&font=lora&orientation=portrait'
        );
    });
});
