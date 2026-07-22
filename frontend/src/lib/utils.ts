/**
 * Converts a 24-hour "HH:MM" string to 12-hour local format, e.g. "4:00PM".
 * The minutes are cut off if they're 0, e.g. 13:00 become 1PM.
 * @param time The time in the HH:MM format, in 24-hour time.
 * @returns
 */
/**
 * Returns "YYYY-MM-DD" for a CalendarItem in local time.
 *
 * All-day events: slices the date string directly — avoids the UTC midnight shift
 * that new Date("YYYY-MM-DD") causes in negative UTC offset timezones.
 *
 * Timed events: ISO string with timezone offset → new Date() converts to local time
 * correctly in all browsers, so dateKey arithmetic is safe.
 */
export function stripHtml(html: string): string {
    return html
        .replace(/<[^>]*>/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

export function providerLabel(provider: string): string {
    if (provider === 'google') return 'Google Calendar';
    return provider;
}

export function eventDateKey(event: { start?: string | null; is_all_day: boolean }): string {
    if (!event.start) return '';
    if (event.is_all_day) return event.start.slice(0, 10);
    const d = new Date(event.start);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function formatTime(time: string): string {
    const [h, m] = time.split(':').map(Number);
    const period = h >= 12 ? 'PM' : 'AM';
    const hour = h % 12 || 12;

    if (m !== 0) {
        return `${hour}:${String(m).padStart(2, '0')}${period}`;
    }

    return `${hour}${period}`;
}
