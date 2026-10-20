import { Cloud, CloudRain, CloudSnow, Sun, Zap } from '@lucide/svelte';

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
