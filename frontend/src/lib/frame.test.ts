import { describe, expect, it } from 'vitest';
import type { CalendarItem, CurrentWeather, ForecastDay, Task } from './api';
import type { Pixels } from './frame';
import {
    allocateLines,
    dayOfYear,
    durationLabel,
    forecastBars,
    frameUrl,
    isAmbientPosition,
    itemsThatFit,
    nextEvents,
    onBlockInk,
    pickBlockInk,
    plateRect,
    railDayLabel,
    relativeDayLabel,
    sunProgress,
    todayTasks,
    upcomingCountdowns,
    weatherSentence,
    weekAhead,
} from './frame';

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

describe('isAmbientPosition', () => {
    it('accepts the six positions and rejects anything else', () => {
        expect(
            ['top-left', 'top-middle', 'top-right', 'bottom-left', 'bottom-middle', 'bottom-right'].every(
                isAmbientPosition
            )
        ).toBe(true);
        expect(isAmbientPosition('middle')).toBe(false);
        expect(isAmbientPosition(null)).toBe(false);
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
                position: 'top-middle',
            })
        ).toBe(
            '/frame?mode=daily&palette=bw&theme=night&font=lora&orientation=portrait&left=today%2Cmoon&right=&split=55&position=top-middle'
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
        expect(allocateLines(['today'], 12).get('today')).toBeCloseTo(10.95);
    });

    it('gives fixed-height widgets their size and splits the rest between the others', () => {
        const lines = allocateLines(['arc', 'today', 'week'], 12);
        expect(lines.get('arc')).toBe(1.7);
        expect(lines.get('today')).toBeCloseTo(lines.get('week')!);
        expect(lines.get('today')!).toBeLessThan(5);
    });

    it('treats countdown stamps as fixed height', () => {
        expect(allocateLines(['countdowns', 'today'], 12).get('countdowns')).toBe(4.5);
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

function forecast(overrides: Partial<ForecastDay>): ForecastDay {
    return {
        date: '2026-10-19',
        weather_code: 2,
        description: 'Partly cloudy',
        temp_max_f: 63,
        temp_min_f: 47,
        sunrise: '2026-10-19T07:21',
        sunset: '2026-10-19T18:24',
        ...overrides,
    };
}

const current: CurrentWeather = {
    temperature_f: 58.4,
    weather_code: 2,
    description: 'Partly cloudy',
    wind_mph: 4,
    fetched_at: '2026-10-19T12:00:00Z',
    uv_index: null,
    us_aqi: null,
};

describe('railDayLabel', () => {
    it('names today, tomorrow, the weekday within a week, then the date', () => {
        expect(railDayLabel(event({ start: '2026-10-19T16:15' }), now)).toBe('Today');
        expect(railDayLabel(event({ start: '2026-10-20T09:00' }), now)).toBe('Tomorrow');
        expect(railDayLabel(event({ start: '2026-10-24', is_all_day: true }), now)).toBe('Sat');
        expect(railDayLabel(event({ start: '2026-10-27T09:30' }), now)).toBe('Oct 27');
    });
});

describe('dayOfYear', () => {
    it('counts from January 1st as day 1', () => {
        expect(dayOfYear(new Date('2026-01-01T08:00'))).toBe(1);
        expect(dayOfYear(new Date('2026-10-20T23:00'))).toBe(293);
    });
});

describe('sunProgress', () => {
    const rise = new Date('2026-10-19T07:00');
    const set = new Date('2026-10-19T19:00');

    it('is the fraction of daylight elapsed', () => {
        expect(sunProgress(rise, set, new Date('2026-10-19T13:00'))).toBeCloseTo(0.5);
    });

    it('is null while the sun is down', () => {
        expect(sunProgress(rise, set, new Date('2026-10-19T05:00'))).toBeNull();
        expect(sunProgress(rise, set, new Date('2026-10-19T21:00'))).toBeNull();
    });
});

describe('durationLabel', () => {
    it('formats hours and zero-padded minutes', () => {
        expect(durationLabel(new Date('2026-10-19T07:21'), new Date('2026-10-19T18:24'))).toBe('11 h 03 m');
    });
});

describe('weatherSentence', () => {
    it('looks ahead to the high while it is still warming', () => {
        expect(weatherSentence(current, forecast({}))).toBe('Partly cloudy, 58° now and rising to 63°.');
    });

    it('looks ahead to the low once the high is reached', () => {
        expect(weatherSentence({ ...current, temperature_f: 63 }, forecast({}))).toBe(
            'Partly cloudy, 63° now, falling to 47° tonight.'
        );
    });

    it('stops after the current reading without a forecast', () => {
        expect(weatherSentence(current, null)).toBe('Partly cloudy, 58° now.');
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

describe('forecastBars', () => {
    it("places every day on the week's shared scale", () => {
        const bars = forecastBars([
            forecast({ temp_min_f: 40, temp_max_f: 60 }),
            forecast({ temp_min_f: 50, temp_max_f: 80 }),
        ]);
        expect(bars[0]).toEqual({ left: 0, width: 50 });
        expect(bars[1]).toEqual({ left: 25, width: 75 });
    });
});

describe('plateRect', () => {
    const plate = { w: 400, h: 200 };
    const frame = { w: 1600, h: 1200 };

    it.each([
        ['top-left', 64, 64],
        ['top-middle', 600, 64],
        ['top-right', 1136, 64],
        ['bottom-left', 64, 936],
        ['bottom-middle', 600, 936],
        ['bottom-right', 1136, 936],
    ] as const)('%s', (position, x, y) => {
        expect(plateRect(position, plate, frame)).toEqual({ x, y, w: 400, h: 200 });
    });
});

describe('onBlockInk', () => {
    it('sets white on dark inks and black on light ones', () => {
        expect(onBlockInk('#000000')).toBe('#ffffff');
        expect(onBlockInk('#5e5b91')).toBe('#ffffff');
        expect(onBlockInk('#ffff6e')).toBe('#000000');
        expect(onBlockInk('#f16f74')).toBe('#000000');
    });
});

describe('pickBlockInk', () => {
    const frame = { w: 160, h: 120 };
    const plate = { x: 64, y: 40, w: 32, h: 40 };

    /** A 160×120 photo filled with `outside`, with `under` beneath the plate. */
    function photo(outside: string, under = outside): Pixels {
        const data = new Uint8ClampedArray(160 * 120 * 4);
        const rgb = (hex: string) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16));
        for (let y = 0; y < 120; y++) {
            for (let x = 0; x < 160; x++) {
                const inPlate = x >= 64 && x < 96 && y >= 40 && y < 80;
                data.set([...rgb(inPlate ? under : outside), 255], (y * 160 + x) * 4);
            }
        }
        return { width: 160, height: 120, data };
    }

    const inks = ['#5e5b91', '#f16f74', '#000000'];

    it('moves off the theme block when the photo around it is that colour', () => {
        expect(pickBlockInk(photo('#5e5b91'), frame, plate, inks, '#5e5b91')).not.toBe('#5e5b91');
    });

    it('keeps the theme block when it already stands out', () => {
        expect(pickBlockInk(photo('#ffffff'), frame, plate, inks, '#5e5b91')).toBe('#5e5b91');
    });

    it('ignores the photo hidden under the plate', () => {
        expect(pickBlockInk(photo('#ffffff', '#5e5b91'), frame, plate, inks, '#5e5b91')).toBe('#5e5b91');
    });

    it('falls back to the preferred ink without candidates', () => {
        expect(pickBlockInk(photo('#5e5b91'), frame, plate, [], '#5e5b91')).toBe('#5e5b91');
    });
});
