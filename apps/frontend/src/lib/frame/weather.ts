// Weather mode: what the full-page view keeps at each text size, the next hours' strip
// and the reading tiles. The Weather service does the fetching and banding; these only
// pick and place.
import type { CurrentWeather, ForecastDay, HourlyWeather } from '#lib/api.ts';
import { durationLabel, nearestHour, RAIN_CHANCE_SHOWN } from './sky.ts';

/** From this text size (percent) the view drops to its compact set, so large type
 *  never crowds the panel. */
export const DENSE_TEXT_SIZE = 130;

/** The strip is left out below this many hours (a stale snapshot near its end). */
export const MIN_HOURS = 6;

/** How much Weather mode shows at a text size: hours in the strip, forecast days and
 *  alerts, and whether the sun tile fits (large text in landscape leaves its sunrise–
 *  sunset too little width). The strip always has four labelled points, every `step` hours. */
export function weatherDensity(
    textSize: number,
    portrait: boolean
): { hours: number; step: number; days: number; alerts: number; sun: boolean } {
    return textSize >= DENSE_TEXT_SIZE
        ? { hours: 8, step: 2, days: 5, alerts: 1, sun: portrait }
        : { hours: 12, step: 3, days: 7, alerts: 2, sun: true };
}

/** The next `count` hours from the one nearest `now` — snapped like the sun path, so the
 *  strip moves once an hour rather than on every render — or none when fewer than
 *  MIN_HOURS remain. */
export function upcomingHours(hourly: HourlyWeather[], now: Date, count: number): HourlyWeather[] {
    const from = nearestHour(now).getTime();
    const hours = hourly.filter((h) => new Date(h.time).getTime() >= from).slice(0, count);
    return hours.length >= MIN_HOURS ? hours : [];
}

/** One point of the strip's temperature line, in percent of its box: x at the hour's
 *  column centre, y from the top on the strip's own low–high scale. */
export interface StripPoint {
    x: number;
    y: number;
}

export function stripPoints(hours: HourlyWeather[]): StripPoint[] {
    const temps = hours.map((h) => h.temperature);
    const lo = Math.min(...temps);
    const span = Math.max(...temps) - lo;
    return hours.map((h, i) => ({
        x: ((i + 0.5) / hours.length) * 100,
        // A flat day draws a level line through the middle.
        y: span === 0 ? 50 : 100 - ((h.temperature - lo) / span) * 100,
    }));
}

/** A labelled point of the strip: every `step`-th hour, with the highest rain chance
 *  over its stretch when that's worth showing (RAIN_CHANCE_SHOWN). */
export interface StripLabel {
    index: number;
    hour: HourlyWeather;
    rain: number | null;
}

export function stripLabels(hours: HourlyWeather[], step: number): StripLabel[] {
    const labels: StripLabel[] = [];
    for (let i = 0; i < hours.length; i += step) {
        const chances = hours.slice(i, i + step).map((h) => h.precipitation_chance ?? 0);
        const rain = Math.max(...chances);
        labels.push({ index: i, hour: hours[i], rain: rain >= RAIN_CHANCE_SHOWN ? rain : null });
    }
    return labels;
}

/** "3 PM", or "Now" for the first hour. */
export function hourLabel(hour: HourlyWeather, first: boolean): string {
    return first ? 'Now' : new Date(hour.time).toLocaleTimeString('en-US', { hour: 'numeric' });
}

/** The feels-like temperature, rounded, when it differs enough from the reading to say. */
export function feelsLike(current: CurrentWeather): number | null {
    const feels = current.apparent_temperature;
    if (feels === null || feels === undefined) return null;
    return Math.abs(feels - current.temperature) >= 2 ? Math.round(feels) : null;
}

export type WeatherTileId = 'uv' | 'air' | 'wind' | 'sun';

/** One reading tile: a short label, the value, and its plain-language band or detail. */
export interface WeatherTile {
    id: WeatherTileId;
    label: string;
    value: string;
    detail: string | null;
}

const clock = (d: Date) => d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }).replace(/ [AP]M$/, '');

/**
 * The tiles beside the temperature, leaving out any without a reading. UV is the day's
 * peak, not the current index — a frame scheduled for the morning would otherwise say
 * "0 · Low" on a day that reaches 8.
 */
export function weatherTiles(current: CurrentWeather, today: ForecastDay | null): WeatherTile[] {
    const tiles: WeatherTile[] = [];

    const uvPeak = today?.uv_index_max ?? null;
    const uv = uvPeak ?? current.uv_index;
    if (uv !== null) {
        tiles.push({
            id: 'uv',
            label: uvPeak !== null ? 'UV today' : 'UV',
            value: String(Math.round(uv)),
            detail: (uvPeak !== null ? today?.uv_max_category : current.uv_category) ?? null,
        });
    }

    if (current.us_aqi !== null) {
        tiles.push({ id: 'air', label: 'Air quality', value: String(current.us_aqi), detail: current.aqi_category });
    }

    tiles.push({
        id: 'wind',
        label: 'Wind',
        value: `${Math.round(current.wind_speed)} ${current.wind_unit}`,
        detail: current.wind_compass ? `From the ${current.wind_compass}` : null,
    });

    if (today) {
        const rise = new Date(today.sunrise);
        const set = new Date(today.sunset);
        tiles.push({
            id: 'sun',
            label: 'Sun',
            value: `${clock(rise)} – ${clock(set)}`,
            detail: `${durationLabel(rise, set)} of daylight`,
        });
    }

    return tiles;
}
