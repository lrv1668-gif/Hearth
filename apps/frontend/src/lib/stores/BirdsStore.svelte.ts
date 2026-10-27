import { api, type BirdSighting } from '$lib/api';

class BirdsStore {
    sightings = $state<BirdSighting[]>([]);
    error = $state(false);
}

export const birdsStore = new BirdsStore();

export async function loadBirds() {
    const sightings = await api.birds.recent();
    if (sightings === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last sightings.
        birdsStore.error = birdsStore.sightings.length === 0;
        return;
    }
    birdsStore.error = false;
    birdsStore.sightings = sightings;
}
