/** Display wording for eBird sightings — shared by the dashboard widget and the frame. */

export function dayLabel(observedAt: string, now: Date = new Date()): string {
    // "YYYY-MM-DD HH:MM" → local Date; no timezone suffix so it parses as local time
    const observed = new Date(observedAt.replace(' ', 'T'));
    const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    const daysAgo = Math.round((startOfDay(now) - startOfDay(observed)) / 86_400_000);

    if (daysAgo <= 0) return 'today';
    if (daysAgo === 1) return 'yesterday';
    return observed.toLocaleDateString(undefined, { weekday: 'short' });
}

export function distanceLabel(mi: number): string {
    return mi < 0.1 ? 'here' : `${mi} miles`;
}

// eBird location names are often verbose — full street addresses
// ("123 Main St, Springfield, IL, US") or hotspot paths with subsites
// ("Riverside Park--North Meadow (restricted access)"). Keep only the
// most specific segment, without trailing parentheticals.
export function shortLocation(loc: string): string {
    const specific = loc.split('--').pop() ?? loc;
    return specific
        .split(',')[0]
        .replace(/\s*\(.*\)\s*$/, '')
        .trim();
}
