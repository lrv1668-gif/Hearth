import { api, type ModeOverride, type ModeSchedule, type ResolvedMode } from '$lib/api';
import { DEFAULT_FRAME_MODE } from '$lib/constants/frameModes';

/**
 * Which mode the e-paper frame shows: the Display service resolves a manual override
 * (with an optional expiry, or a photo pinned with "Show on frame"), else the
 * time-of-day schedule, else its default mode. Shared by every client.
 */
class ModeStore {
    /** What the frame shows now; null until loaded or while Display is unreachable. */
    current = $state<ResolvedMode | null>(null);
    schedule = $state<ModeSchedule>({ default_mode: DEFAULT_FRAME_MODE, rules: [] });
    loaded = $state(false);
    error = $state(false);

    /** The upload "Show on frame" has pinned, if any. */
    get pinnedPhotoId(): string | null {
        return this.current?.reason === 'override' ? (this.current.photo_id ?? null) : null;
    }
}

export const modeStore = new ModeStore();

export async function loadModes() {
    const [current, schedule] = await Promise.all([api.display.mode(), api.display.schedule()]);
    modeStore.error = current === null || schedule === null;
    if (current) modeStore.current = current;
    if (schedule) modeStore.schedule = schedule;
    modeStore.loaded = true;
}

async function refreshCurrent() {
    const current = await api.display.mode();
    modeStore.error = current === null;
    if (current) modeStore.current = current;
}

export async function saveSchedule(schedule: ModeSchedule) {
    modeStore.schedule = schedule;
    try {
        modeStore.schedule = await api.display.saveSchedule(schedule);
        await refreshCurrent();
    } catch {
        modeStore.error = true;
    }
}

export async function setOverride(manual: ModeOverride) {
    try {
        await api.display.setOverride(manual);
        await refreshCurrent();
    } catch {
        modeStore.error = true;
    }
}

/** Back to the schedule. With `photoId`, only if that photo is the one pinned. */
export async function clearOverride(photoId?: string) {
    if (!(await api.display.clearOverride(photoId))) modeStore.error = true;
    await refreshCurrent();
}

/** "Show on frame": pin one upload until it's switched off — no expiry. */
export function pinPhoto(photoId: string) {
    return setOverride({ mode: 'photo', until: null, photo_id: photoId });
}
