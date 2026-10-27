import { api, type HomeSettings } from '$lib/api';

/** This browser's IANA zone — the default for the household's until one is saved. */
export function browserTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

/** True for a zone id Intl accepts — the same check the frame's renderer makes. */
export function isTimeZone(zone: string): boolean {
    try {
        new Intl.DateTimeFormat('en', { timeZone: zone });
        return true;
    } catch {
        return false;
    }
}

/** The zone id as Intl spells it — "america/denver" becomes "America/Denver", which the
 *  host's case-sensitive tz database needs. */
export function canonicalTimeZone(zone: string): string {
    return new Intl.DateTimeFormat('en', { timeZone: zone }).resolvedOptions().timeZone;
}

/** Every zone this browser knows, for the time zone picker (empty on older browsers). */
export function knownTimeZones(): string[] {
    return typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : [];
}

/**
 * The household's location and time zone. One shared copy lives in the Display service:
 * Weather, Birds and Almanac read it (falling back to their LATITUDE/LONGITUDE/TZ env
 * vars), and the frame's renderer draws the clock in its zone.
 */
class HomeLocationStore {
    latitude = $state<number | null>(null);
    longitude = $state<number | null>(null);
    name = $state<string | null>(null);
    timeZone = $state<string | null>(null);
    /** True once something has been saved — otherwise the services use their env vars. */
    saved = $state(false);
    /** True once the Display service has answered (or failed to). */
    loaded = $state(false);
    /** The Display service couldn't be reached. */
    error = $state(false);

    apply(home: HomeSettings) {
        this.latitude = home.latitude;
        this.longitude = home.longitude;
        this.name = home.name;
        this.timeZone = home.time_zone;
        this.saved = true;
    }
}

export const homeLocation = new HomeLocationStore();

export async function loadHomeLocation() {
    const home = await api.display.home();
    homeLocation.error = home === undefined;
    if (home) homeLocation.apply(home);
    homeLocation.loaded = true;
}

/** Saves the location; throws when the Display service refuses it or can't be reached. */
export async function saveHomeLocation(home: HomeSettings) {
    homeLocation.apply(await api.display.saveHome(home));
    homeLocation.error = false;
}
