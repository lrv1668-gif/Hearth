import {
    Cloud,
    CloudLightning,
    CloudRain,
    CloudSnow,
    Leaf,
    Snowflake,
    Sun,
    SunMedium,
    ThermometerSnowflake,
    ThermometerSun,
    Wind,
    Zap,
} from '@lucide/svelte';
import type { WeatherAlert } from '#lib/api.ts';

/** Lucide icon for a WMO weather code (as returned by the Weather service). */
export function weatherIcon(code: number) {
    if (code === 0 || code === 1) return Sun;
    if (code <= 3) return Cloud;
    if (code <= 57) return CloudRain;
    if (code <= 77) return CloudSnow;
    if (code <= 82) return CloudRain;
    if (code <= 86) return CloudSnow;
    return Zap;
}

const alertIcons = {
    storm: CloudLightning,
    snow: Snowflake,
    frost: ThermometerSnowflake,
    heat: ThermometerSun,
    wind: Wind,
    air: Leaf,
    uv: SunMedium,
} as const;

/** Lucide icon for a weather alert's kind. */
export function alertIcon(kind: WeatherAlert['kind']) {
    return alertIcons[kind] ?? CloudLightning;
}
