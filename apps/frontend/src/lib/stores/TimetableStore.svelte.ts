import { api, type StopTimetable } from '#lib/api.ts';
import { ensureHousehold, household } from './HouseholdStore.svelte.ts';

class TimetableStore {
    timetables = $state<StopTimetable[]>([]);
    error = $state(false);
}

export const timetableStore = new TimetableStore();

export async function loadTimetable() {
    await ensureHousehold();
    const stopKeys = household.trainStops.map((s) => s.stopKey);
    if (stopKeys.length === 0) {
        timetableStore.timetables = [];
        timetableStore.error = false;
        return;
    }
    const timetables = await api.trains.timetable(stopKeys);
    if (timetables === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last block.
        timetableStore.error = timetableStore.timetables.length === 0;
        return;
    }
    timetableStore.error = false;
    timetableStore.timetables = timetables;
}
