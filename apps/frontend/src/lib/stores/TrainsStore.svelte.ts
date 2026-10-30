import { api, type StopDepartures } from '#lib/api.ts';
import { settings } from './SettingsStore.svelte';

export const trainsStore = $state<{ groups: StopDepartures[] }>({
    groups: [],
});

export async function loadTrainDepartures() {
    const stopKeys = settings.trainStops.map((s) => s.stopKey);
    if (stopKeys.length === 0) {
        trainsStore.groups = [];
        return;
    }
    // A failed refresh keeps the last good board on screen.
    const groups = await api.trains.departures(stopKeys);
    if (groups) trainsStore.groups = groups;
}
