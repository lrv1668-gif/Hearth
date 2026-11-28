import { describe, expect, it } from 'vitest';
import type { CurrentWeather, ForecastDay, HourlyWeather } from '#lib/api.ts';
import {
    feelsLike,
    hourLabel,
    stripLabels,
    stripPoints,
    upcomingHours,
    weatherDensity,
    weatherTiles,
} from './weather.ts';

const current: CurrentWeather = {
    temperature: 58.4,
    weather_code: 2,
    description: 'Partly cloudy',
    wind_speed: 6.6,
    fetched_at: '2026-10-19T12:00:00Z',
    uv_index: 3,
    us_aqi: 42,
    uv_category: 'Moderate',
    aqi_category: 'Good',
    temperature_unit: 'F',
    wind_unit: 'mph',
    apparent_temperature: 55,
    humidity: 62,
    wind_direction: 310,
    wind_compass: 'NW',
};

const today: ForecastDay = {
    date: '2026-10-19',
    weather_code: 2,
    description: 'Partly cloudy',
    temp_max: 63,
    temp_min: 47,
    sunrise: '2026-10-19T07:21',
    sunset: '2026-10-19T18:24',
    uv_index_max: 5.6,
    uv_max_category: 'High',
    precipitation_chance: null,
    wind_speed_max: null,
};

/** `count` hours from 2026-10-19 at `from` o'clock, warming a degree an hour. */
function hours(count: number, from = 12, chance: (i: number) => number | null = () => null): HourlyWeather[] {
    return Array.from({ length: count }, (_, i) => ({
        time: `2026-10-19T${String(from + i).padStart(2, '0')}:00`,
        temperature: 50 + i,
        weather_code: 2,
        precipitation_chance: chance(i),
    }));
}

describe('weatherDensity', () => {
    it('keeps the full set at ordinary text sizes', () => {
        expect(weatherDensity(100, false)).toEqual({ hours: 12, step: 3, days: 7, alerts: 2, sun: true });
        expect(weatherDensity(125, true)).toEqual({ hours: 12, step: 3, days: 7, alerts: 2, sun: true });
    });

    it('drops to the compact set at large text', () => {
        expect(weatherDensity(130, true)).toEqual({ hours: 8, step: 2, days: 5, alerts: 1, sun: true });
        expect(weatherDensity(150, false)).toEqual({ hours: 8, step: 2, days: 5, alerts: 1, sun: false });
    });

    it('labels four points of the strip either way', () => {
        for (const size of [100, 150]) {
            const { hours: count, step } = weatherDensity(size, false);
            expect(count / step).toBe(4);
        }
    });
});

describe('upcomingHours', () => {
    it('starts at the hour nearest now', () => {
        const list = hours(24, 10);
        expect(upcomingHours(list, new Date('2026-10-19T12:20'), 12)[0].time).toBe('2026-10-19T12:00');
        expect(upcomingHours(list, new Date('2026-10-19T12:40'), 12)[0].time).toBe('2026-10-19T13:00');
    });

    it('takes at most `count` hours', () => {
        expect(upcomingHours(hours(24), new Date('2026-10-19T12:00'), 8)).toHaveLength(8);
    });

    it('leaves the strip out when too few hours remain', () => {
        expect(upcomingHours(hours(7), new Date('2026-10-19T14:00'), 12)).toEqual([]);
        expect(upcomingHours(hours(8), new Date('2026-10-19T14:00'), 12)).toHaveLength(6);
    });
});

describe('stripPoints', () => {
    it('spans the strip on its own low–high scale, at column centres', () => {
        const points = stripPoints(hours(4));
        expect(points.map((p) => p.x)).toEqual([12.5, 37.5, 62.5, 87.5]);
        expect(points[0].y).toBe(100);
        expect(points[3].y).toBe(0);
    });

    it('draws a flat day through the middle', () => {
        const flat = hours(3).map((h) => ({ ...h, temperature: 60 }));
        expect(stripPoints(flat).map((p) => p.y)).toEqual([50, 50, 50]);
    });
});

describe('stripLabels', () => {
    it('labels every step-th hour with the stretch’s highest rain chance worth showing', () => {
        const list = hours(12, 12, (i) => [10, 40, 20, 0, 0, 0, null, 70, 30, 5, 0, 0][i]);
        const labels = stripLabels(list, 3);
        expect(labels.map((l) => l.index)).toEqual([0, 3, 6, 9]);
        expect(labels.map((l) => l.rain)).toEqual([40, null, 70, null]);
    });
});

describe('hourLabel', () => {
    it('says Now for the first hour, then the hour', () => {
        const [first, second] = hours(2, 14);
        expect(hourLabel(first, true)).toBe('Now');
        expect(hourLabel(second, false)).toBe('3 PM');
    });
});

describe('feelsLike', () => {
    it('shows a feels-like that differs by 2° or more', () => {
        expect(feelsLike(current)).toBe(55);
    });

    it('leaves out a feels-like close to the reading, or none', () => {
        expect(feelsLike({ ...current, apparent_temperature: 57 })).toBeNull();
        expect(feelsLike({ ...current, apparent_temperature: null })).toBeNull();
    });
});

describe('weatherTiles', () => {
    it('shows UV, air, wind and sun', () => {
        expect(weatherTiles(current, today)).toEqual([
            { id: 'uv', label: 'UV today', value: '6', detail: 'High' },
            { id: 'air', label: 'Air quality', value: '42', detail: 'Good' },
            { id: 'wind', label: 'Wind', value: '7 mph', detail: 'From the NW' },
            { id: 'sun', label: 'Sun', value: '7:21 – 6:24', detail: '11 h 03 m of daylight' },
        ]);
    });

    it('falls back to the current UV without a daily peak', () => {
        const uv = weatherTiles(current, { ...today, uv_index_max: null, uv_max_category: null })[0];
        expect(uv).toEqual({ id: 'uv', label: 'UV', value: '3', detail: 'Moderate' });
    });

    it('drops the tiles that have no reading', () => {
        const bare = { ...current, uv_index: null, us_aqi: null, aqi_category: null, wind_compass: null };
        expect(weatherTiles(bare, null)).toEqual([{ id: 'wind', label: 'Wind', value: '7 mph', detail: null }]);
    });
});
