import { api, type AlmanacResponse } from '#lib/api.ts';

class AlmanacStore {
    almanac = $state<AlmanacResponse | null>(null);
    error = $state(false);
}

export const almanacStore = new AlmanacStore();

export async function loadAlmanac() {
    const almanac = await api.almanac.today();
    if (almanac === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last almanac.
        almanacStore.error = almanacStore.almanac === null;
        return;
    }
    almanacStore.error = false;
    almanacStore.almanac = almanac;
}
