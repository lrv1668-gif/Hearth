// Settings → Frame's status bar: whether the frame client is still checking in.
import type { FrameStatus } from '#lib/api.ts';

/* ── Frame check-in ───────────────────────────────────────────────────────────
   GET /display/frame/status's last_poll: when the frame client (inkypull) last asked
   for its picture. A frame that's gone quiet for longer than this gets a warning. */
export const FRAME_CHECK_IN_STALE_MS = 60 * 60 * 1000;

export interface CheckIn {
    text: string;
    /** True when the frame hasn't checked in for a while (or since Hearth started). */
    stale: boolean;
}

/** "3 min ago", "2 hours ago", "4 days ago". */
export function timeAgo(ms: number): string {
    const minutes = Math.floor(ms / 60_000);
    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
    const days = Math.floor(hours / 24);
    return days === 1 ? '1 day ago' : `${days} days ago`;
}

/** One line on whether the frame is checking in and has the latest picture. */
export function checkIn(status: FrameStatus, now: Date): CheckIn {
    const poll = status.last_poll;
    if (!poll) return { text: 'The frame hasn’t checked in since Hearth last started.', stale: true };
    const age = now.getTime() - new Date(poll.at).getTime();
    const when = `The frame checked in ${timeAgo(age)}`;
    if (age > FRAME_CHECK_IN_STALE_MS) return { text: `${when}. Check its power and Wi-Fi.`, stale: true };
    if (!poll.etag) return { text: `${when}, before anything had rendered.`, stale: false };
    return poll.etag === status.etag
        ? { text: `${when} and has the latest picture.`, stale: false }
        : { text: `${when}; the latest picture reaches it on its next check.`, stale: false };
}

/** A moment for Settings' frame status, "3:15 PM", or "Sat 3:15 PM" when it isn't today. */
export function clockLabel(iso: string, now: Date): string {
    const at = new Date(iso);
    const time = at.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    return at.toDateString() === now.toDateString()
        ? time
        : `${at.toLocaleDateString('en-US', { weekday: 'short' })} ${time}`;
}
