import { browser } from '$app/env';
import { api, type HouseholdSettings } from '#lib/api.ts';
import type { RssFeed } from '#lib/constants/rss.ts';
import type { TrainStop } from '#lib/constants/trains.ts';

/** Where SettingsStore parks the feeds and stops it finds in an old localStorage
 *  snapshot, so they can be moved to the Display service once (see loadHousehold). */
export const LEGACY_HOUSEHOLD_KEY = 'hearth-household-legacy';

const SAVE_DELAY_MS = 400;

export interface Household {
    rssFeeds: RssFeed[];
    trainStops: TrainStop[];
}

export function fromSaved(saved: HouseholdSettings): Household {
    return {
        rssFeeds: (saved.rss_feeds ?? []).map(({ title, url }) => ({ title, url })),
        trainStops: (saved.train_stops ?? []).map(({ label, stop_key, line_filter }) => ({
            label,
            stopKey: stop_key,
            // null shows every line; [] (Settings' "Deselect all") shows none
            ...(line_filter ? { lineFilter: line_filter } : {}),
        })),
    };
}

export function toSaved({ rssFeeds, trainStops }: Household): HouseholdSettings {
    return {
        rss_feeds: rssFeeds.map(({ title, url }) => ({ title, url })),
        train_stops: trainStops.map(({ label, stopKey, lineFilter }) => ({
            label,
            stop_key: stopKey,
            line_filter: lineFilter ?? null,
        })),
    };
}

/** The lists parked by SettingsStore, or null when there's nothing worth moving. */
export function parseLegacy(raw: string | null): Household | null {
    if (!raw) return null;
    try {
        const { rssFeeds, trainStops } = JSON.parse(raw);
        const feeds: RssFeed[] = Array.isArray(rssFeeds) ? rssFeeds : [];
        const stops: TrainStop[] = Array.isArray(trainStops) ? trainStops : [];
        return feeds.length || stops.length ? { rssFeeds: feeds, trainStops: stops } : null;
    } catch {
        return null;
    }
}

/**
 * The RSS feeds and transit stops the household follows. One shared copy lives in the
 * Display service, so every browser and the frame's headless renderer see the same
 * lists — the dashboard's News Feed and Trains, and the frame's Headlines and
 * Scheduled Departures.
 */
class HouseholdStore {
    rssFeeds = $state<RssFeed[]>([]);
    trainStops = $state<TrainStop[]>([]);
    /** True once a load finished, so empty lists mean "none" rather than "not yet". */
    loaded = $state(false);
    /** Display couldn't be reached: saving is held off so empty lists can't overwrite the real ones. */
    error = $state(false);

    snapshot(): Household {
        return { rssFeeds: this.rssFeeds, trainStops: this.trainStops };
    }

    apply({ rssFeeds, trainStops }: Household) {
        this.rssFeeds = rssFeeds;
        this.trainStops = trainStops;
    }
}

export const household = new HouseholdStore();

let pending: Promise<void> | null = null;

/** Loads the shared lists once per page; a failed load is retried on the next call. */
export function ensureHousehold(): Promise<void> {
    pending ??= loadHousehold().then(() => {
        if (household.error) pending = null;
    });
    return pending;
}

/** Loads the shared lists. If none are saved yet but this browser has lists from before
 *  they moved to the server, those are uploaded once. */
export async function loadHousehold() {
    const saved = await api.display.household();
    if (saved === undefined) {
        household.error = true;
    } else {
        household.error = false;
        if (saved) household.apply(fromSaved(saved));
        else await migrateLegacy();
    }
    household.loaded = true;
}

async function migrateLegacy() {
    if (!browser) return;
    const legacy = parseLegacy(localStorage.getItem(LEGACY_HOUSEHOLD_KEY));
    if (!legacy) return;
    household.apply(legacy);
    try {
        await api.display.saveHousehold(toSaved(legacy));
        localStorage.removeItem(LEGACY_HOUSEHOLD_KEY);
    } catch {
        // keep the legacy copy for the next attempt
    }
}

/**
 * Saves every change back to the Display service, debounced. Call once the lists have
 * loaded (it skips the initial values); returns the cleanup for onMount.
 */
export function syncHousehold(): () => void {
    let last = JSON.stringify(household.snapshot());
    let timer: ReturnType<typeof setTimeout> | undefined;
    const stop = $effect.root(() => {
        $effect(() => {
            const next = household.snapshot();
            const json = JSON.stringify(next);
            if (json === last || household.error || !household.loaded) return;
            clearTimeout(timer);
            timer = setTimeout(async () => {
                try {
                    await api.display.saveHousehold(toSaved(next));
                    last = json;
                } catch {
                    household.error = true;
                }
            }, SAVE_DELAY_MS);
        });
    });
    return () => {
        clearTimeout(timer);
        stop();
    };
}

export function subscribeToRssFeed(title: string, url: string) {
    household.rssFeeds = [...household.rssFeeds, { title, url }];
}

export function unsubscribeFromRssFeed(title: string) {
    household.rssFeeds = household.rssFeeds.filter((rssFeed) => rssFeed.title != title);
}

export function subscribeToTrainStop(label: string, stopKey: string) {
    household.trainStops = [...household.trainStops, { label, stopKey }];
}

export function unsubscribeFromTrainStop(stopKey: string) {
    household.trainStops = household.trainStops.filter((stop) => stop.stopKey != stopKey);
}

export function updateTrainStopLineFilter(stopKey: string, lineFilter: string[] | undefined) {
    household.trainStops = household.trainStops.map((stop) =>
        stop.stopKey === stopKey ? { ...stop, lineFilter } : stop
    );
}
