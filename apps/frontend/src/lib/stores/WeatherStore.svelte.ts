import { api, type CurrentWeather, type ForecastDay } from '#lib/api.ts';

export const weatherStore = $state({
    current: null as CurrentWeather | null,
    forecast: [] as ForecastDay[],
    /** The first load has finished, successfully or not — widgets show a skeleton until then. */
    loaded: false,
});

// A failed refresh keeps the last good values on screen rather than blanking the widgets.
export async function loadWeather() {
    const [current, forecast] = await Promise.all([api.weather.current(), api.weather.forecast()]);
    if (current) weatherStore.current = current;
    if (forecast) weatherStore.forecast = forecast;
    weatherStore.loaded = true;
}
