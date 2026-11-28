import { get } from './http.ts';

/** °F and mph, or °C and km/h — the household's choice in Settings → Home. */
export type TemperatureUnit = 'F' | 'C';

export interface CurrentWeather {
    /** In `temperature_unit`; the forecast days' temperatures use the same unit. */
    temperature: number;
    weather_code: number;
    description: string;
    /** In `wind_unit`. */
    wind_speed: number;
    fetched_at: string;
    uv_index: number | null;
    us_aqi: number | null;
    /** Plain-language bands from the Weather service (WHO UV, EPA AQI). */
    uv_category: string | null;
    aqi_category: string | null;
    temperature_unit: TemperatureUnit;
    wind_unit: 'mph' | 'km/h';
    /** Feels-like, in `temperature_unit`. */
    apparent_temperature: number | null;
    /** Relative humidity, 0–100. */
    humidity: number | null;
    /** Where the wind blows from, in degrees, and as an eight-point name ("NW"). */
    wind_direction: number | null;
    wind_compass: string | null;
}

/** One hour of the next 24, from the current hour. */
export interface HourlyWeather {
    /** Open-Meteo's local ISO hour, "2026-06-28T14:00". */
    time: string;
    /** In the current conditions' `temperature_unit`. */
    temperature: number;
    weather_code: number;
    /** 0–100; null when Open-Meteo has none. */
    precipitation_chance: number | null;
}

export interface ForecastDay {
    date: string;
    weather_code: number;
    description: string;
    temp_max: number;
    temp_min: number;
    sunrise: string;
    sunset: string;
    /** The day's peak UV — what a scheduled frame shows, since the current UV is 0 at dawn. */
    uv_index_max: number | null;
    uv_max_category: string | null;
    /** The day's highest hourly chance of rain or snow, 0–100; null when Open-Meteo has none. */
    precipitation_chance: number | null;
    /** The day's strongest sustained wind, in the current conditions' `wind_unit`. */
    wind_speed_max: number | null;
}

/** A line worth showing on an otherwise quiet screen, picked by the Weather service from the
 *  snapshot — most pressing first. */
export interface WeatherAlert {
    kind: 'storm' | 'snow' | 'frost' | 'heat' | 'wind' | 'air' | 'uv';
    text: string;
}

export const weather = {
    current: (): Promise<CurrentWeather | null> => get<CurrentWeather>('/weather/current'),

    forecast: (): Promise<ForecastDay[] | null> => get<ForecastDay[]>('/weather/forecast'),

    alerts: (): Promise<WeatherAlert[] | null> => get<WeatherAlert[]>('/weather/alerts'),

    hourly: (): Promise<HourlyWeather[] | null> => get<HourlyWeather[]>('/weather/hourly'),
};
