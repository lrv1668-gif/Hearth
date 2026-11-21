import { get } from './http.ts';

export interface CurrentWeather {
    temperature_f: number;
    weather_code: number;
    description: string;
    wind_mph: number;
    fetched_at: string;
    uv_index: number | null;
    us_aqi: number | null;
    /** Plain-language bands from the Weather service (WHO UV, EPA AQI). */
    uv_category: string | null;
    aqi_category: string | null;
}

export interface ForecastDay {
    date: string;
    weather_code: number;
    description: string;
    temp_max_f: number;
    temp_min_f: number;
    sunrise: string;
    sunset: string;
    /** The day's peak UV — what a scheduled frame shows, since the current UV is 0 at dawn. */
    uv_index_max: number | null;
    uv_max_category: string | null;
}

export const weather = {
    current: (): Promise<CurrentWeather | null> => get<CurrentWeather>('/weather/current'),

    forecast: (): Promise<ForecastDay[] | null> => get<ForecastDay[]>('/weather/forecast'),
};
