// The frame draws the date and time from the browser's clock, so each capture runs in the
// household's zone: Display sends the one saved in Settings as ?tz=, and the TZ env var
// covers installs that haven't saved one. A container otherwise runs on UTC.

/** True for a zone id Intl (and so Chromium) accepts. */
export function isTimeZone(zone) {
    if (typeof zone !== 'string' || zone === '') return false;
    try {
        new Intl.DateTimeFormat('en', { timeZone: zone });
        return true;
    } catch {
        return false;
    }
}

/**
 * The zone to capture in: the request's `tz` when given, else `fallback` (TZ).
 * `{ error }` when the request names a zone Intl doesn't know; `zone` is undefined
 * (Chromium's own zone, UTC in the container) when neither is set.
 */
export function captureTimeZone(requested, fallback) {
    if (requested === null || requested === undefined || requested === '') return { zone: fallback };
    return isTimeZone(requested) ? { zone: requested } : { error: `tz "${requested}" is not an IANA time zone` };
}
