import { api, type CurrentWeather, type ForecastDay, type HourlyWeather, type WeatherAlert } from '#lib/api.ts';

export const weatherStore = $state({
    current: null as CurrentWeather | null,
    forecast: [] as ForecastDay[],
    /** Storms, frost, heat… most pressing first; empty on a calm day. */
    alerts: [] as WeatherAlert[],
    /** The next 24 hours from the current one (as of the last fetch). */
    hourly: [] as HourlyWeather[],
    /** The first load has finished, successfully or not — widgets show a skeleton until then. */
    loaded: false,
});

// A failed refresh keeps the last good values on screen rather than blanking the widgets.
export async function loadWeather() {
    const [current, forecast, alerts, hourly] = await Promise.all([
        api.weather.current(),
        api.weather.forecast(),
        api.weather.alerts(),
        api.weather.hourly(),
    ]);
    if (current) weatherStore.current = current;
    if (forecast) weatherStore.forecast = forecast;
    if (alerts) weatherStore.alerts = alerts;
    if (hourly) weatherStore.hourly = hourly;
    weatherStore.loaded = true;
}
