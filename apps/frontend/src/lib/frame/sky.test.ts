import { describe, expect, it } from 'vitest';
import type { CurrentWeather, ForecastDay } from '#lib/api.ts';
import {
    dayOfYear,
    durationLabel,
    editionLabel,
    forecastBars,
    nearestHour,
    nextSunrise,
    sunProgress,
    weatherSentence,
} from './sky.ts';

const now = new Date('2026-10-19T12:00');

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

describe('editionLabel', () => {
    const rise = new Date('2026-10-19T07:00');
    const set = new Date('2026-10-19T18:00');

    it('names the hour, snapped like the sun, while the sun is up', () => {
        expect(editionLabel(rise, set, new Date('2026-10-19T10:40'))).toBe('11 AM');
    });

    it('holds one label after sunset and another before sunrise, so it adds no redraws', () => {
        expect(editionLabel(rise, set, new Date('2026-10-19T19:40'))).toBe('Evening');
        expect(editionLabel(rise, set, new Date('2026-10-19T23:10'))).toBe('Evening');
        expect(editionLabel(rise, set, new Date('2026-10-19T05:20'))).toBe('Early');
    });

    it('is null without sun times', () => {
        expect(editionLabel(null, null, new Date('2026-10-19T10:00'))).toBeNull();
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
