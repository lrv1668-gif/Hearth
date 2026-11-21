import { describe, expect, it } from 'vitest';
import type { CalendarItem } from '#lib/api.ts';
import { task } from '#lib/test/fixtures.ts';
import {
    heroCountdown,
    nextEvents,
    fitAgenda,
    railDayLabel,
    relativeDayLabel,
    todayTasks,
    upcomingCountdowns,
    weekAhead,
} from './agenda.ts';

const now = new Date('2026-10-19T12:00');

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

describe('fitAgenda', () => {
    const costs = { headingLines: 2, rowLines: 1 };
    const items = [
        event({ id: 'trip', start: '2026-10-17', end: '2026-10-22', is_all_day: true }),
        event({ id: 'lunch', start: '2026-10-19T12:30', end: '2026-10-19T13:30' }),
        event({ id: 'dentist', start: '2026-10-20T09:00' }),
        event({ id: 'dinner', start: '2026-10-23T19:00' }),
    ];

    it('groups by day, with a running event under today', () => {
        const days = fitAgenda(items, now, 20, costs);
        expect(days.map((d) => [d.label, d.events.map((e) => e.id)])).toEqual([
            ['Today', ['trip', 'lunch']],
            ['Tomorrow', ['dentist']],
            ['Friday, Oct 23', ['dinner']],
        ]);
    });

    it('cuts to the budget, starting a day only if its heading and one event fit', () => {
        // Today: 2 + 1 + 1 = 4 lines; Tomorrow needs 3 more.
        expect(fitAgenda(items, now, 6, costs).map((d) => d.label)).toEqual(['Today']);
        expect(fitAgenda(items, now, 3.5, costs).map((d) => d.events.length)).toEqual([1]);
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

    it('keeps a multi-day all-day event that started before today and is still running', () => {
        const items = [
            event({ id: 'trip', start: '2026-10-17', end: '2026-10-22', is_all_day: true }),
            event({ id: 'over', start: '2026-10-17', end: '2026-10-19', is_all_day: true }),
        ];
        expect(nextEvents(items, now, 5).map((e) => e.id)).toEqual(['trip']);
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

describe('weekAhead', () => {
    it('groups events and tasks by day, skipping empty days', () => {
        const days = weekAhead(
            [task({ id: 1, title: 'Bins', due_date: '2026-10-21T00:00:00' })],
            [event({ id: 'a', title: 'Dentist', start: '2026-10-19T15:00' })],
            now
        );
        expect(days.map((d) => [d.label, d.entries.map((e) => e.title)])).toEqual([
            ['Today', ['Dentist']],
            ['Wed 21', ['Bins']],
        ]);
    });

    it('puts untimed entries first, then timed ones in order', () => {
        const days = weekAhead(
            [task({ id: 1, title: 'Late', due_date: '2026-10-20', due_time: '18:00' })],
            [
                event({ id: 'b', title: 'Birthday', start: '2026-10-20', is_all_day: true }),
                event({ id: 'c', title: 'Standup', start: '2026-10-20T09:30' }),
            ],
            now
        );
        expect(days[0].label).toBe('Tomorrow');
        expect(days[0].entries.map((e) => [e.title, e.time])).toEqual([
            ['Birthday', null],
            ['Standup', '9:30AM'],
            ['Late', '6PM'],
        ]);
    });

    it('ignores done tasks, countdowns, completed Google Tasks and days past the window', () => {
        const days = weekAhead(
            [task({ id: 1, done: true }), task({ id: 2, is_countdown: true }), task({ id: 3, due_date: '2026-10-26' })],
            [event({ id: 'g', kind: 'task', start: '2026-10-19', is_completed: true })],
            now
        );
        expect(days).toEqual([]);
    });
});

describe('railDayLabel', () => {
    it('names today, tomorrow, the weekday within a week, then the date', () => {
        expect(railDayLabel(event({ start: '2026-10-19T16:15' }), now)).toBe('Today');
        expect(railDayLabel(event({ start: '2026-10-20T09:00' }), now)).toBe('Tomorrow');
        expect(railDayLabel(event({ start: '2026-10-24', is_all_day: true }), now)).toBe('Sat');
        expect(railDayLabel(event({ start: '2026-10-27T09:30' }), now)).toBe('Oct 27');
    });
});

describe('upcomingCountdowns', () => {
    it('keeps pending countdowns from today on, soonest first', () => {
        const tasks = [
            task({ id: 1, is_countdown: true, due_date: '2026-10-31' }),
            task({ id: 2, is_countdown: true, due_date: '2026-10-19' }),
            task({ id: 3, is_countdown: true, due_date: '2026-10-10' }),
            task({ id: 4, is_countdown: true, due_date: '2026-10-22', done: true }),
            task({ id: 5, due_date: '2026-10-20' }),
        ];
        expect(upcomingCountdowns(tasks, now, 5).map((c) => [c.task.id, c.days])).toEqual([
            [2, 0],
            [1, 12],
        ]);
    });
});

describe('heroCountdown', () => {
    const tasks = [
        task({ id: 1, is_countdown: true, due_date: '2026-10-31' }),
        task({ id: 2, is_countdown: true, due_date: '2026-10-22' }),
        task({ id: 3, is_countdown: true, due_date: '2026-10-10' }),
    ];

    it('shows the chosen countdown while it is still ahead', () => {
        expect(heroCountdown(tasks, now, 1)).toMatchObject({ task: { id: 1 }, days: 12 });
    });

    it('falls back to the next one coming up', () => {
        expect(heroCountdown(tasks, now, null)?.task.id).toBe(2);
        expect(heroCountdown(tasks, now, 3)?.task.id).toBe(2); // already past
        expect(heroCountdown(tasks, now, 99)?.task.id).toBe(2); // deleted
    });

    it('is null with no countdowns ahead', () => {
        expect(heroCountdown([], now, null)).toBeNull();
    });
});
