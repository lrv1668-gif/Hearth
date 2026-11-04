import { api, type OnThisDay } from '#lib/api.ts';

class OnThisDayStore {
    day = $state<OnThisDay | null>(null);
    error = $state(false);
}

export const onThisDayStore = new OnThisDayStore();

export async function loadOnThisDay() {
    const day = await api.history.onThisDay();
    if (day === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last day.
        onThisDayStore.error = onThisDayStore.day === null;
        return;
    }
    onThisDayStore.error = false;
    onThisDayStore.day = day;
}

/** "1605", or "44 BC" for a negative year. */
export function yearLabel(year: number): string {
    return year < 0 ? `${-year} BC` : String(year);
}
