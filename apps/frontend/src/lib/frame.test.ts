import { describe, expect, it } from 'vitest';
import type { CalendarItem, CurrentWeather, ForecastDay, FrameStatus } from './api';
import { task } from './test/fixtures';
import type { Pixels } from './frame';
import { isPhotoInterval, photoIntervals } from './constants/frameModes';
import {
    allocateLines,
    checkIn,
    dayOfYear,
    durationLabel,
    forecastBars,
    frameUrl,
    fitFontSize,
    frameCanvas,
    heroCountdown,
    isFrameSize,
    isFrameDithering,
    isPortraitRotation,
    nearestHour,
    nextSunrise,
    normalizeFrameSize,
    timeAgo,
    toQuietHours,
    DEFAULT_FRAME_SIZE,
    isAmbientPosition,
    itemsThatFit,
    nextEvents,
    fitAgenda,
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
                textSize: 120,
                orientation: 'portrait',
                width: 800,
                height: 480,
                layout: { left: ['today', 'moon'], right: [] },
                split: 55,
                template: 'feature',
                slots: ['week', '', 'quote'],
                position: 'top-middle',
            })
        ).toBe(
            '/frame?mode=daily&palette=bw&theme=night&font=lora&text=120&orientation=portrait&width=800&height=480&left=today%2Cmoon&right=&split=55&template=feature&slots=week%2C%2Cquote&position=top-middle'
        );
    });
});

describe('frameCanvas', () => {
    it('keeps the native size in landscape and swaps it in portrait', () => {
        expect(frameCanvas({ width: 800, height: 480, orientation: 'landscape' })).toEqual({
            w: 800,
            h: 480,
            scale: 0.4,
        });
        expect(frameCanvas({ width: 800, height: 480, orientation: 'portrait' })).toEqual({
            w: 480,
            h: 800,
            scale: 0.4,
        });
    });

    it('scales to fit both edges', () => {
        expect(frameCanvas({ width: 800, height: 600, orientation: 'landscape' }).scale).toBe(0.5);
        expect(frameCanvas({ width: 2000, height: 1200, orientation: 'landscape' }).scale).toBe(1);
    });

    it('is 1:1 at the reference panel', () => {
        expect(frameCanvas({ ...DEFAULT_FRAME_SIZE, orientation: 'landscape' }).scale).toBe(1);
    });
});

describe('frame size', () => {
    it('accepts in-range integer sizes with width ≥ height', () => {
        expect(isFrameSize(800, 480)).toBe(true);
        expect(isFrameSize(480, 800)).toBe(false);
        expect(isFrameSize(100, 100)).toBe(false);
        expect(isFrameSize(800.5, 480)).toBe(false);
        expect(isFrameSize('800', 480)).toBe(false);
    });

    it('normalizes typed sizes into range, long edge first', () => {
        expect(normalizeFrameSize(480, 800)).toEqual({ width: 800, height: 480 });
        expect(normalizeFrameSize(8, 99999)).toEqual({ width: 4000, height: 200 });
        expect(normalizeFrameSize(NaN, 600.4)).toEqual({ width: 1600, height: 600 });
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

    it('gives Last Played its album square and the rest to its neighbour', () => {
        const lines = allocateLines(['last-played', 'today'], 20);
        expect(lines.get('last-played')).toBe(5.3);
        expect(lines.get('today')).toBeCloseTo(20 - 2 * 1.05 - 0.85 - 5.3);
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
        uv_index_max: null,
        uv_max_category: null,
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
    uv_category: null,
    aqi_category: null,
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

describe('fitFontSize', () => {
    it('finds the largest size that still fits', () => {
        expect(fitFontSize((px) => px <= 87, 22, 200)).toBe(87);
    });

    it('stops at the bounds', () => {
        expect(fitFontSize(() => true, 22, 200)).toBe(200);
        expect(fitFontSize(() => false, 22, 200)).toBe(22);
    });

    it('works on fractional bounds from a scaled frame', () => {
        expect(fitFontSize((px) => px <= 50, 10.5, 99.9)).toBe(50);
    });
});

describe('nextSunrise', () => {
    it("takes today's sunrise before dawn and tomorrow's after it", () => {
        const days = [
            forecast({ date: '2026-10-19', sunrise: '2026-10-19T07:12' }),
            forecast({ date: '2026-10-20', sunrise: '2026-10-20T07:13' }),
        ];
        expect(nextSunrise(days, new Date('2026-10-19T03:00'))).toEqual(new Date('2026-10-19T07:12'));
        expect(nextSunrise(days, now)).toEqual(new Date('2026-10-20T07:13'));
        expect(nextSunrise(days, new Date('2026-10-21T03:00'))).toBeNull();
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

    it('scales the inset with the frame', () => {
        expect(plateRect('top-left', plate, { w: 800, h: 600 }, 0.5)).toEqual({ x: 32, y: 32, w: 400, h: 200 });
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

describe('panel render settings', () => {
    it('accepts only the two dithering choices', () => {
        expect(isFrameDithering('hearth')).toBe(true);
        expect(isFrameDithering('frame')).toBe(true);
        expect(isFrameDithering('inky')).toBe(false);
        expect(isFrameDithering(undefined)).toBe(false);
    });

    it('accepts only quarter turns for portrait', () => {
        expect(isPortraitRotation(90)).toBe(true);
        expect(isPortraitRotation(270)).toBe(true);
        expect(isPortraitRotation(180)).toBe(false);
        expect(isPortraitRotation('90')).toBe(false);
    });

    it("keeps photo intervals within Display's 15 min – 1 day range", () => {
        expect(photoIntervals.every((i) => isPhotoInterval(i.minutes))).toBe(true);
        expect(isPhotoInterval(14)).toBe(false);
        expect(isPhotoInterval(1441)).toBe(false);
        expect(isPhotoInterval(30.5)).toBe(false);
    });
});

describe('nearestHour', () => {
    it('rounds to the nearest whole hour', () => {
        expect(nearestHour(new Date(2026, 9, 23, 12, 29, 59))).toEqual(new Date(2026, 9, 23, 12, 0));
        expect(nearestHour(new Date(2026, 9, 23, 12, 30))).toEqual(new Date(2026, 9, 23, 13, 0));
    });

    it('rolls over into the next day', () => {
        expect(nearestHour(new Date(2026, 9, 23, 23, 45))).toEqual(new Date(2026, 9, 24, 0, 0));
    });

    it('keeps two renders within the same hour identical', () => {
        expect(nearestHour(new Date(2026, 9, 23, 14, 5))).toEqual(nearestHour(new Date(2026, 9, 23, 14, 20)));
    });
});

describe('toQuietHours', () => {
    it('reads a saved window', () => {
        expect(toQuietHours('23:00', '06:00')).toEqual({ start: '23:00', end: '06:00' });
    });

    it('is off when either end is missing, malformed or equal', () => {
        expect(toQuietHours(null, null)).toBeNull();
        expect(toQuietHours('23:00', null)).toBeNull();
        expect(toQuietHours('25:00', '06:00')).toBeNull();
        expect(toQuietHours('06:00', '06:00')).toBeNull();
    });
});

describe('checkIn', () => {
    const now = new Date('2026-10-24T12:00:00Z');
    const status = (poll: FrameStatus['last_poll'], etag = '"a"'): FrameStatus => ({
        mode: 'daily',
        rendered_at: null,
        checked_at: null,
        next_render_at: null,
        etag,
        last_error: null,
        last_poll: poll,
    });
    const polled = (minutesAgo: number, etag: string | null = '"a"') => ({
        at: new Date(now.getTime() - minutesAgo * 60_000).toISOString(),
        address: '192.168.1.42',
        user_agent: 'inkypull',
        etag,
    });

    it('says when the frame has the latest picture', () => {
        expect(checkIn(status(polled(3)), now)).toEqual({
            text: 'The frame checked in 3 min ago and has the latest picture.',
            stale: false,
        });
    });

    it('says the latest picture is on its way when the frame holds an older one', () => {
        expect(checkIn(status(polled(0, '"old"')), now).text).toBe(
            'The frame checked in just now; the latest picture reaches it on its next check.'
        );
    });

    it('warns once the frame has been quiet for over an hour', () => {
        expect(checkIn(status(polled(180)), now)).toEqual({
            text: 'The frame checked in 3 hours ago. Check its power and Wi-Fi.',
            stale: true,
        });
    });

    it('warns when nothing has polled since Hearth started', () => {
        expect(checkIn(status(null), now).stale).toBe(true);
    });
});

describe('timeAgo', () => {
    it('counts minutes, hours and days', () => {
        expect(timeAgo(30_000)).toBe('just now');
        expect(timeAgo(59 * 60_000)).toBe('59 min ago');
        expect(timeAgo(60 * 60_000)).toBe('1 hour ago');
        expect(timeAgo(49 * 60 * 60_000)).toBe('2 days ago');
    });
});
