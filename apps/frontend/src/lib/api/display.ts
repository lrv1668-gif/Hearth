import { del, get, getSaved, put, sendJson, tryFetch } from './http.ts';
import { frameParams, type FrameDithering, type FrameOptions, type PortraitRotation } from '#lib/frame/settings.ts';
import type { FrameModeId, GuestSecurity, ModeDay, PhotoCard } from '#lib/constants/frameModes.ts';
import type { FramePalette } from '#lib/constants/frameThemes.ts';
import type { Photo } from './photos.ts';

/** The RSS feeds and transit stops every device shares, as Display stores them (snake_case). */
export interface HouseholdSettings {
    rss_feeds: { title: string; url: string }[];
    train_stops: { label: string; stop_key: string; line_filter: string[] | null }[];
}

/** The household's location and time zone, held by the Display service and read by
 *  Weather, Birds, Almanac and the frame's renderer. Coordinates come as a pair. */
export interface HomeSettings {
    latitude: number | null;
    longitude: number | null;
    /** A label for the place, e.g. "Denver, Colorado". */
    name: string | null;
    /** IANA zone id, e.g. "America/Denver". */
    time_zone: string | null;
    /** The weather's units; null (nothing chosen yet) is imperial. */
    units?: WeatherUnits | null;
}

/** °F and mph, or °C and km/h. */
export type WeatherUnits = 'imperial' | 'metric';

/** Display refused the home settings; the message is its reason, e.g. "latitude must be -90–90". */
export class HomeSettingsRejected extends Error {}

/** A city search result (Display's /display/places). */
export interface Place {
    name: string;
    label: string;
    latitude: number;
    longitude: number;
    time_zone: string | null;
}

/** The e-paper frame's shared look and Daily layout, held by the Display service so
 *  every client (and the frame's renderer) sees the same settings. */
export interface FrameSettings {
    theme: string;
    font: string;
    /** Percent on the frame's reading type and icons (100–150). */
    text_size: number;
    orientation: 'landscape' | 'portrait';
    /** The panel's native (landscape) size in pixels; portrait swaps it. */
    width: number;
    height: number;
    left: string[];
    right: string[];
    split: number;
    /** Daily's layout template (constants/frameTemplates.ts); 'columns' uses left/right/split. */
    template: string;
    /** The template's widget per slot, '' for an empty slot. */
    slots: string[];
    /** Where Ambient mode's info card sits. */
    ambient_position: AmbientPosition;
    /** Ambient photos (frame and the /ambient slideshow): "unsplash", "local", "both", … */
    photo_source: string;
    photo_categories: string[];
    /** Photo mode's uploads — one pins it, several make an album — and how much card it keeps. */
    photo_ids: string[];
    photo_card: PhotoCard;
    /** Countdown mode's countdown task; null shows the next one coming up. */
    countdown_id: number | null;
    /** Note mode's heading and body (one line per rule or item; blank lines space sections). */
    note_title: string | null;
    note_body: string | null;
    /** GET /display/frame: minutes Ambient and Photo keep a photo (15–1440). */
    photo_interval_minutes: number;
    /** GET /display/frame: clockwise degrees a portrait frame is turned onto the panel. */
    portrait_rotation: PortraitRotation;
    /** GET /display/frame: dithered by Hearth, or sent undithered for the frame client to dither. */
    dithering: FrameDithering;
    /** GET /display/frame: a nightly "HH:mm" window (home time) with no interval redraws; both or neither. */
    quiet_start: string | null;
    quiet_end: string | null;
    /** Guest mode: a heading (null shows "Welcome"), the guest Wi-Fi for its QR code, and a few notes.
     *  Read from here by /frame — never put in its URL. Optional so older payloads still read. */
    guest_title?: string | null;
    guest_network?: string | null;
    guest_password?: string | null;
    guest_security?: GuestSecurity;
    guest_notes?: string | null;
}

/** The frame client's last GET /display/frame (Display keeps it in memory only). */
export interface FramePoll {
    at: string;
    address: string | null;
    user_agent: string | null;
    /** The picture the frame was left holding; null when nothing was served. */
    etag: string | null;
}

/** GET /display/frame/status: the panel render, and the frame's last check-in. */
export interface FrameStatus {
    mode: FrameModeId | null;
    rendered_at: string | null;
    checked_at: string | null;
    next_render_at: string | null;
    etag: string | null;
    last_error: string | null;
    last_poll: FramePoll | null;
}

/** A time-of-day schedule rule ("HH:mm", household time); an end before the start
 *  runs past midnight, equal times mean all day. With `rotate`, the rule steps from
 *  `mode` through those modes every `rotate_minutes`, starting each window on `mode`. */
export interface ModeRule {
    mode: FrameModeId;
    days: ModeDay[];
    start: string;
    end: string;
    rotate?: FrameModeId[] | null;
    rotate_minutes?: number | null;
}

/** The frame's default mode plus its rules; the first matching rule wins. */
export interface ModeSchedule {
    default_mode: FrameModeId;
    rules: ModeRule[];
}

/** One stretch of today's schedule ("HH:mm", household time; `end` "24:00" for midnight),
 *  the modes shown in it, and the index of the rule holding it — null for the default. */
export interface ModeSpan {
    start: string;
    end: string;
    modes: FrameModeId[];
    rule: number | null;
}

/** Today's schedule, midnight to midnight, for Settings' timeline. */
export interface ModeTimeline {
    date: string;
    spans: ModeSpan[];
}

/** What the frame shows now and why; `until` is when that may next change. */
export interface ResolvedMode {
    mode: FrameModeId;
    reason: 'override' | 'schedule' | 'default';
    until: string | null;
    /** The upload pinned by "Show on frame", when one is. */
    photo_id: string | null;
}

/** A manual choice that beats the schedule until `until` (null: until cleared). */
export interface ModeOverride {
    mode: FrameModeId;
    until?: string | null;
    photo_id?: string | null;
}

export type AmbientPosition =
    'top-left' | 'top-middle' | 'top-right' | 'bottom-left' | 'bottom-middle' | 'bottom-right';

export const display = {
    /** The saved frame settings; null when none have been saved yet, undefined when
     *  the Display service can't be reached. */
    settings: (): Promise<FrameSettings | null | undefined> =>
        // Bounded, so a hung service can't hold up /frame's ready signal.
        getSaved<FrameSettings>('/display/settings', { timeoutMs: 5000 }),

    saveSettings: (settings: FrameSettings): Promise<FrameSettings> =>
        put<FrameSettings>('/display/settings', settings),

    /** The mode the frame shows now; null when the Display service can't be reached. */
    mode: (): Promise<ResolvedMode | null> =>
        // Bounded like settings(), so a hung service can't hold up /frame's ready signal.
        get<ResolvedMode>('/display/mode', { timeoutMs: 5000 }),

    schedule: (): Promise<ModeSchedule | null> => get<ModeSchedule>('/display/mode/schedule'),

    /** Today's schedule as a timeline, in household time. */
    day: (): Promise<ModeTimeline | null> => get<ModeTimeline>('/display/mode/day'),

    saveSchedule: (schedule: ModeSchedule): Promise<ModeSchedule> =>
        put<ModeSchedule>('/display/mode/schedule', schedule),

    setOverride: (manual: ModeOverride): Promise<ModeOverride> => put<ModeOverride>('/display/mode/override', manual),

    /** Back to the schedule. With `photoId`, only if that photo is the one pinned. */
    clearOverride: (photoId?: string): Promise<boolean> =>
        del(`/display/mode/override${photoId ? `?${new URLSearchParams({ photo_id: photoId })}` : ''}`),

    /** Steps to the next mode until the schedule's next change — the frame's Button B. */
    next: async (): Promise<ResolvedMode | null> => {
        const res = await tryFetch('/display/mode/next', { method: 'POST' });
        return res?.ok ? (res.json() as Promise<ResolvedMode>).catch(() => null) : null;
    },

    /** Color or B&W — what the panel and Settings' preview draw /frame in. null when
     *  Display can't be reached. */
    palette: (): Promise<{ palette: FramePalette } | null> =>
        // Bounded like settings(), so a hung service can't hold up /frame's ready signal.
        get<{ palette: FramePalette }>('/display/palette', { timeoutMs: 5000 }),

    setPalette: (palette: FramePalette): Promise<{ palette: FramePalette }> =>
        put<{ palette: FramePalette }>('/display/palette', { palette }),

    /** The photo Ambient or Photo mode shows, or Daily's Photo widget (today's) — the one
     *  Display holds for the panel, so every view matches. null when there's none (or no
     *  Photos service), undefined when Display can't be reached: the caller then picks its own. */
    photo: (mode: 'ambient' | 'photo' | 'daily'): Promise<Photo | null | undefined> =>
        // Bounded like settings(), so a hung service can't hold up /frame's ready signal.
        getSaved<Photo>(`/display/photo?${new URLSearchParams({ mode })}`, { timeoutMs: 8000 }),

    /** The panel render and the frame's last check-in; null when Display can't be reached. */
    frameStatus: (): Promise<FrameStatus | null> => get<FrameStatus>('/display/frame/status'),

    /** The saved home location; null when none is saved yet, undefined when the
     *  Display service can't be reached. */
    home: (): Promise<HomeSettings | null | undefined> => getSaved<HomeSettings>('/display/home'),

    /** Throws HomeSettingsRejected with Display's reason on a 400. */
    saveHome: async (home: HomeSettings): Promise<HomeSettings> => {
        const res = await sendJson('PUT', '/display/home', home);
        if (res.status === 400) throw new HomeSettingsRejected(await res.json().catch(() => 'invalid settings'));
        if (!res.ok) throw new Error(`PUT /display/home failed: ${res.status}`);
        return res.json() as Promise<HomeSettings>;
    },

    /** The shared feeds and stops; null when none are saved yet, undefined when the
     *  Display service can't be reached. */
    household: (): Promise<HouseholdSettings | null | undefined> =>
        // Bounded like settings(), so a hung service can't hold up /frame's ready signal.
        getSaved<HouseholdSettings>('/display/household', { timeoutMs: 5000 }),

    saveHousehold: (household: HouseholdSettings): Promise<HouseholdSettings> =>
        put<HouseholdSettings>('/display/household', household),

    /** Cities matching `query`, each with its coordinates and time zone; null on failure. */
    places: (query: string): Promise<Place[] | null> =>
        get<Place[]>(`/display/places?${new URLSearchParams({ q: query })}`),

    /** The frame rendered and dithered to the panel's six inks, as a PNG URL. `screen`
     *  shows the exact inks the panel is sent; `paper` repaints them as they look on it. */
    previewUrl: (o: FrameOptions, look: 'screen' | 'paper'): string => {
        const params = frameParams(o);
        params.set('look', look);
        return `/display/preview?${params}`;
    },
};
