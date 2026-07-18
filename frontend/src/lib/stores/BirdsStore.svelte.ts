import { api, type BirdSighting } from '$lib/api';

class BirdsStore {
    sightings = $state<BirdSighting[]>([]);
    error = $state(false);
}

export const birdsStore = new BirdsStore();

export async function loadBirds() {
    birdsStore.error = false;
    const sightings = await api.birds.recent();
    if (sightings === null) {
        birdsStore.error = true;
        return;
    }
    birdsStore.sightings = sightings;
}
