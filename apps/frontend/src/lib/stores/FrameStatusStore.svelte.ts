import { api, type FrameStatus } from '$lib/api';

/** The panel render and the frame client's last check-in (GET /display/frame/status). */
class FrameStatusStore {
    /** null until loaded, or while the Display service can't be reached. */
    status = $state<FrameStatus | null>(null);
}

export const frameStatusStore = new FrameStatusStore();

/** Refreshes the status; a failed refresh keeps the last one. */
export async function loadFrameStatus() {
    const status = await api.display.frameStatus();
    if (status) frameStatusStore.status = status;
}
