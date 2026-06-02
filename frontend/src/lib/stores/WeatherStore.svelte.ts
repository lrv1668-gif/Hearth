import { api, type CurrentWeather, type ForecastDay } from '$lib/api';

export const weatherStore = $state({
    current: null as CurrentWeather | null,
    forecast: [] as ForecastDay[],
});

export async function loadWeather() {
    const [current, forecast] = await Promise.all([api.weather.current(), api.weather.forecast()]);
    weatherStore.current = current;
    weatherStore.forecast = forecast;
}
