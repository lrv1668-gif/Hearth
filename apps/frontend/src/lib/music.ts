/** Display wording for the dashboard's Now Playing card and the frame's Last Played card. */

// Plays before 5am count as the night before, the way people say it.
const NIGHT_ENDS = 5;

function partOfDay(hour: number): 'morning' | 'afternoon' | 'evening' | 'night' {
    if (hour < NIGHT_ENDS || hour >= 21) return 'night';
    if (hour < 12) return 'morning';
    if (hour < 17) return 'afternoon';
    return 'evening';
}

/** The day a moment belongs to, with small hours folded into the evening before. */
function listeningDay(d: Date): number {
    const shifted = new Date(d.getTime() - NIGHT_ENDS * 3_600_000);
    return new Date(shifted.getFullYear(), shifted.getMonth(), shifted.getDate()).getTime();
}

/**
 * When a track was played, in words that stay true between frame refreshes: "this morning",
 * "last night", "Tuesday evening", or a date a week on — never "2 hours ago".
 */
export function playedLabel(playedAt: string, now: Date = new Date()): string {
    const played = new Date(playedAt);
    const part = partOfDay(played.getHours());
    const daysAgo = Math.round((listeningDay(now) - listeningDay(played)) / 86_400_000);

    if (daysAgo <= 0) return part === 'night' ? 'tonight' : `this ${part}`;
    if (daysAgo === 1) return part === 'night' ? 'last night' : `yesterday ${part}`;
    if (daysAgo < 7) return `${played.toLocaleDateString('en-US', { weekday: 'long' })} ${part}`;
    return played.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

/** A track length or position as m:ss (h:mm:ss past an hour). */
export function formatTrackTime(ms: number): string {
    const total = Math.max(0, Math.floor(ms / 1000));
    const h = Math.floor(total / 3600);
    const m = Math.floor((total % 3600) / 60);
    const s = String(total % 60).padStart(2, '0');
    return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${s}` : `${m}:${s}`;
}

/**
 * Where playback is now, moving on from what Spotify last reported so the progress bar
 * ticks between polls. Holds still while paused and never runs past the end.
 */
export function trackPosition(
    track: { progress_ms: number; duration_ms: number; is_playing: boolean },
    fetchedAt: number,
    now: number
): number {
    const moved = track.is_playing ? Math.max(0, now - fetchedAt) : 0;
    return Math.min(track.progress_ms + moved, track.duration_ms);
}
