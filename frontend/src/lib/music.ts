/** Display wording for the frame's Last Played card. */

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
