import { describe, expect, it } from 'vitest';
import type { CalendarItem, Task } from './api';
import { allocateLines, frameUrl, itemsThatFit, nextEvents, relativeDayLabel, todayTasks, weekAhead } from './frame';

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

describe('frameUrl', () => {
    it('spells out every option', () => {
        expect(
            frameUrl({
                mode: 'daily',
                palette: 'bw',
                theme: 'night',
                font: 'lora',
                orientation: 'portrait',
                layout: { left: ['today', 'moon'], right: [] },
                split: 55,
            })
        ).toBe(
            '/frame?mode=daily&palette=bw&theme=night&font=lora&orientation=portrait&left=today%2Cmoon&right=&split=55'
        );
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

describe('allocateLines', () => {
    it('gives a lone widget the whole column minus its heading', () => {
        expect(allocateLines(['today'], 12).get('today')).toBeCloseTo(10.75);
    });

    it('gives fixed-height widgets their size and splits the rest between the others', () => {
        const lines = allocateLines(['arc', 'today', 'week'], 12);
        expect(lines.get('arc')).toBe(1.7);
        expect(lines.get('today')).toBeCloseTo(lines.get('week')!);
        expect(lines.get('today')!).toBeLessThan(5);
    });

    it('never hands out negative space', () => {
        expect(allocateLines(['today', 'week', 'quote'], 2).get('today')).toBe(0);
    });
});

describe('itemsThatFit', () => {
    it('floors to whole items but always shows one', () => {
        expect(itemsThatFit(5, 2)).toBe(2);
        expect(itemsThatFit(0, 2)).toBe(1);
    });
});
