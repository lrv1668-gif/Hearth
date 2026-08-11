import { fetchTrainDepartures, type StopDepartures } from '$lib/api';
import { settings } from './SettingsStore.svelte';

export const trainsStore = $state<{ groups: StopDepartures[]; loading: boolean }>({
    groups: [],
    loading: false,
});

export async function loadTrainDepartures() {
    const stopKeys = settings.trainStops.map((s) => s.stopKey);
    if (stopKeys.length === 0) {
        trainsStore.groups = [];
        return;
    }
    trainsStore.loading = true;
    trainsStore.groups = await fetchTrainDepartures(stopKeys);
    trainsStore.loading = false;
}
