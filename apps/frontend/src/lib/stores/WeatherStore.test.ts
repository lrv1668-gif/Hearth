import { beforeEach, describe, expect, it } from 'vitest';
import type { WeatherAlert } from '#lib/api.ts';
import { mockFetch } from '#lib/test/fetch.ts';
import { loadWeather, weatherStore } from './WeatherStore.svelte.ts';

const frost: WeatherAlert = { kind: 'frost', text: 'Frost tonight · low of 28°' };

const { fetchMock } = mockFetch();

/** Answers each Weather endpoint by path; a missing one is unreachable. */
function weather(routes: Record<string, unknown>) {
    fetchMock.mockImplementation(async (input) => {
        const path = new URL(String(input), 'http://hearth').pathname;
        if (!(path in routes)) throw new TypeError(`unreachable: ${path}`);
        return new Response(JSON.stringify(routes[path]), { status: 200 });
    });
}

beforeEach(() => {
    weatherStore.current = null;
    weatherStore.forecast = [];
    weatherStore.alerts = [];
    weatherStore.hourly = [];
    weatherStore.loaded = false;
});

describe('loadWeather', () => {
    it('loads the alerts with the conditions', async () => {
        weather({ '/weather/current': null, '/weather/forecast': [], '/weather/alerts': [frost] });

        await loadWeather();

        expect(weatherStore.alerts).toEqual([frost]);
        expect(weatherStore.loaded).toBe(true);
    });

    it('keeps the last alerts when a refresh fails', async () => {
        weatherStore.alerts = [frost];
        weather({});

        await loadWeather();

        expect(weatherStore.alerts).toEqual([frost]);
        expect(weatherStore.loaded).toBe(true);
    });

    it('clears the alerts once the weather calms', async () => {
        weatherStore.alerts = [frost];
        weather({ '/weather/current': null, '/weather/forecast': [], '/weather/alerts': [] });

        await loadWeather();

        expect(weatherStore.alerts).toEqual([]);
    });

    it('loads the next hours with the conditions, and keeps them when a refresh fails', async () => {
        const hour = { time: '2026-06-28T14:00', temperature: 72, weather_code: 1, precipitation_chance: 20 };
        weather({
            '/weather/current': null,
            '/weather/forecast': [],
            '/weather/alerts': [],
            '/weather/hourly': [hour],
        });

        await loadWeather();
        expect(weatherStore.hourly).toEqual([hour]);

        weather({});
        await loadWeather();
        expect(weatherStore.hourly).toEqual([hour]);
    });
});
