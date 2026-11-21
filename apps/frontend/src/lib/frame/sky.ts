// Daily's masthead and forecast band: the edition line, the sun's path, the
// weather sentence and the forecast's range bars.
import type { CurrentWeather, ForecastDay } from '#lib/api.ts';

/* ── Almanac edition ──────────────────────────────────────────────────────────
   Small derivations for the Daily masthead, sun path, stamps and forecast strip. */

/** How far `now` is through the daylight between sunrise and sunset (0–1), or null
 *  while the sun is down. */
/**
 * `d` rounded to the nearest whole hour (local time). The frame draws the sun's position
 * at this, not the exact minute: Display re-renders Daily every 15 min, and a sun that
 * moved a few pixels each time would redraw the whole panel (~30 s of flashing) four
 * times an hour. Only the position snaps — dates still come from the real time.
 */
export function nearestHour(d: Date): Date {
    const snapped = new Date(d);
    snapped.setMinutes(d.getMinutes() >= 30 ? 60 : 0, 0, 0);
    return snapped;
}

export function sunProgress(sunrise: Date, sunset: Date, now: Date): number | null {
    const span = sunset.getTime() - sunrise.getTime();
    const elapsed = now.getTime() - sunrise.getTime();
    if (span <= 0 || elapsed < 0 || elapsed > span) return null;
    return elapsed / span;
}

/**
 * The Daily edition line's edition: the hour ("11 AM") while the sun is up, snapped like
 * the sun path's disc so it changes only on renders that move the disc anyway; "Evening"
 * after sunset and "Early" before sunrise, when the disc stands still and an hourly label
 * would make every render a full panel refresh; null without sun times (no sun path).
 */
export function editionLabel(sunrise: Date | null, sunset: Date | null, now: Date): string | null {
    if (!sunrise || !sunset) return null;
    const at = nearestHour(now);
    if (sunProgress(sunrise, sunset, at) !== null) return at.toLocaleTimeString('en-US', { hour: 'numeric' });
    return at > sunset ? 'Evening' : 'Early';
}

/** "11 h 03 m" between two times. */
export function durationLabel(from: Date, to: Date): string {
    const minutes = Math.max(0, Math.round((to.getTime() - from.getTime()) / 60_000));
    return `${Math.floor(minutes / 60)} h ${String(minutes % 60).padStart(2, '0')} m`;
}

/** When a photo frame was drawn, "7:05 AM" — never a clock, which would be wrong in between.
 *  Only Ambient and Photo show it: they redraw on the photo interval anyway, while in the
 *  15-minute modes a changing time would make every render a full ~30 s panel refresh. */
export function updatedTime(now: Date): string {
    return now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

/** The masthead's one sentence of weather: "Partly cloudy, 58° now and rising to 63°."
 *  Once the day's high is (about) reached it looks ahead to the low instead. */
export function weatherSentence(current: CurrentWeather, today: ForecastDay | null): string {
    const now = Math.round(current.temperature);
    const lead = `${current.description}, ${now}° now`;
    if (!today) return `${lead}.`;
    const high = Math.round(today.temp_max);
    const low = Math.round(today.temp_min);
    return high > now ? `${lead} and rising to ${high}°.` : `${lead}, falling to ${low}° tonight.`;
}

/** The next sunrise after `now` in the forecast (Open-Meteo's local ISO times), or null. */
export function nextSunrise(forecast: ForecastDay[], now: Date): Date | null {
    return forecast.map((f) => new Date(f.sunrise)).find((sunrise) => sunrise > now) ?? null;
}

/** The shared temperature scale for the forecast strip's range bars: each day's
 *  bar as left offset and width, in percent of the week's lowest low to highest high. */
export function forecastBars(days: ForecastDay[]): { left: number; width: number }[] {
    const lo = Math.min(...days.map((d) => d.temp_min));
    const span = Math.max(...days.map((d) => d.temp_max)) - lo || 1;
    return days.map((d) => ({
        left: ((d.temp_min - lo) / span) * 100,
        width: ((d.temp_max - d.temp_min) / span) * 100,
    }));
}

/** Below this chance a day's rain isn't worth a line on the forecast. */
export const RAIN_CHANCE_SHOWN = 30;

/** A forecast day's chance of rain or snow in percent when it's worth showing, else null. */
export function rainChance(day: ForecastDay): number | null {
    const chance = day.precipitation_chance;
    return chance != null && chance >= RAIN_CHANCE_SHOWN ? chance : null;
}
