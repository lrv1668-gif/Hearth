import { api, type StopDepartures } from '#lib/api.ts';
import { ensureHousehold, household } from './HouseholdStore.svelte.ts';

export const trainsStore = $state<{ groups: StopDepartures[] }>({
    groups: [],
});

export async function loadTrainDepartures() {
    await ensureHousehold();
    const stopKeys = household.trainStops.map((s) => s.stopKey);
    if (stopKeys.length === 0) {
        trainsStore.groups = [];
        return;
    }
    // A failed refresh keeps the last good board on screen.
    const groups = await api.trains.departures(stopKeys);
    if (groups) trainsStore.groups = groups;
}
