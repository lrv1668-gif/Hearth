/** Display wording for the Almanac service's facts — shared by the dashboard widget and the frame. */

/** "Nov 3" for a "YYYY-MM-DD" date, kept local ("YYYY-MM-DD" alone would parse as UTC). */
export function shortDate(iso: string): string {
    const [y, m, d] = iso.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

export function trendText(perDay: number): string {
    if (perDay === 0) return 'Day length is holding steady';
    const amount = Math.abs(perDay) < 1 ? 'under a minute' : `~${Math.round(Math.abs(perDay))} min`;
    return `${perDay < 0 ? 'Losing' : 'Gaining'} ${amount} of daylight each day`;
}

export function frostTiming(days: number): string {
    if (days === 0) return 'today';
    if (days <= 21) return `in ${days} day${days === 1 ? '' : 's'}`;
    return `in ~${Math.round(days / 7)} weeks`;
}
