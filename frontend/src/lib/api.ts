import { frameParams, type FrameDithering, type FrameOptions, type PortraitRotation } from '$lib/frame';
import type { FrameModeId, ModeDay, PhotoCard } from '$lib/constants/frameModes';

// ---- Types ----

export interface Task {
    id: number;
    title: string;
    done: boolean;
    due_date: string | null;
    due_time: string | null;
    created_at: string;
    description: string | null;
    assignee: string | null;
    recurrence_unit: string | null;
    recurrence_interval: number | null;
    recurrence_days: string | null;
    recurrence_end_date: string | null;
    series_id: number | null;
    is_countdown: boolean;
}

export interface CreateTaskInput {
    title: string;
    due_date?: string | null;
    due_time?: string | null;
    description?: string | null;
    assignee?: string | null;
    recurrence_unit?: string | null;
    recurrence_interval?: number | null;
    recurrence_days?: string | null;
    recurrence_end_date?: string | null;
    is_countdown?: boolean;
}

export interface UpdateTaskInput {
    done: boolean;
    title: string;
    due_date?: string | null;
    due_time?: string | null;
    description?: string | null;
    assignee?: string | null;
}

export interface NowPlaying {
    title: string;
    artist: string;
    album_name: string;
    album_art_url: string | null;
    progress_ms: number;
    duration_ms: number;
    is_playing: boolean;
}

export interface SpotifyStatus {
    authenticated: boolean;
    /** Linked before Hearth asked for listening history — Last Played needs a reconnect. */
    needs_reconnect: boolean;
}

export interface RecentTrack {
    title: string;
    artist: string;
    album_name: string;
    album_art_url: string | null;
    played_at: string;
}

/** The frame's Last Played card; either part can be missing. */
export interface LastPlayed {
    track: RecentTrack | null;
    top_artist: string | null;
}

export interface Quote {
    q: string;
    a: string;
}

export interface CurrentWeather {
    temperature_f: number;
    weather_code: number;
    description: string;
    wind_mph: number;
    fetched_at: string;
    uv_index: number | null;
    us_aqi: number | null;
}

export interface ForecastDay {
    date: string;
    weather_code: number;
    description: string;
    temp_max_f: number;
    temp_min_f: number;
    sunrise: string;
    sunset: string;
}

export interface Photo {
    id: string;
    url: string;
    thumb_url: string | null;
    description: string | null;
    photographer_name: string | null;
    unsplash_link: string | null;
    source: string;
}

export interface UploadedPhoto {
    id: string;
    url: string;
    thumb_url: string;
    caption: string | null;
}

/** A named set of uploads (Photos service), for picking several at once. */
export interface PhotoGroup {
    id: string;
    name: string;
    photo_ids: string[];
}

export interface BatchFileResult {
    file_name: string;
    status: 'ok' | 'duplicate' | 'error';
    error: string | null;
    photo: UploadedPhoto | null;
}

export interface BirdSighting {
    species_code: string;
    common_name: string;
    scientific_name: string;
    location: string;
    observed_at: string; // "YYYY-MM-DD HH:MM" in the observation's local time
    count: number | null;
    distance_mi: number;
    is_notable: boolean;
}

export interface AlmanacSeason {
    name: string; // "spring" | "summer" | "autumn" | "winter"
    label: string; // "Early summer", "Midsummer", ...
    day_of_season: number;
    total_days: number;
    progress: number; // 0–1
    next_marker: string; // "Autumn equinox", ...
    next_marker_date: string; // "YYYY-MM-DD"
    days_until_marker: number;
}

export interface AlmanacMilestone {
    label: string; // "Last 8 pm sunset", ...
    date: string; // "YYYY-MM-DD"
}

export interface AlmanacDaylight {
    trend_minutes_per_day: number; // negative = losing light; 0 = holding steady
    drift_minutes: number; // vs. the nearest solstice extreme
    drift_reference: string; // "longest day" | "shortest day"
    milestones: AlmanacMilestone[];
}

export interface AlmanacFrost {
    label: string; // "First frost" | "Last frost"
    date: string; // "YYYY-MM-DD"
    days_until: number;
}

export interface AlmanacResponse {
    season: AlmanacSeason;
    daylight: AlmanacDaylight | null; // null when the service has no coordinates
    frost: AlmanacFrost | null; // null when frost dates are not configured
    note: string | null; // seasonal phenology note; null for southern hemisphere
}

export interface RssArticle {
    title: string;
    link: string;
    description: string | null;
    published_at: string | null;
}

export interface RssFeedGroup {
    feed_title: string;
    feed_url: string;
    articles: RssArticle[];
}

export interface TrainDeparture {
    route_short_name: string;
    route_long_name: string | null;
    route_type: number; // GTFS numeric mode: 0=tram,1=subway,2=rail,3=bus,4=ferry,...
    mode: string; // human-readable mode derived from route_type, e.g. "bus", "rail"
    headsign: string | null;
    scheduled_departure: string | null;
    estimated_departure: string | null;
    is_realtime: boolean;
}

export interface StopDepartures {
    stop_key: string;
    stop_name: string | null;
    departures: TrainDeparture[];
}

export interface CalendarItem {
    kind: 'event' | 'task'; // "event" = Google Calendar event; "task" = Google Task
    id: string;
    title: string;
    description?: string | null;
    location?: string | null;
    start?: string | null; // ISO 8601 with offset, "YYYY-MM-DD" for all-day/tasks, or null (undated task)
    end?: string | null; // null for tasks
    is_all_day: boolean;
    calendar_name?: string | null;
    provider: string; // "google"
    is_completed?: boolean | null; // null for events; true/false for tasks
    task_list_id?: string | null; // null for events; required for toggle endpoint
    html_link?: string | null; // direct URL to view in provider
}

// Discriminated union shared by Calendar.svelte, DayOverflowModal.svelte, and UpcomingTasksWidget.svelte.
// kind: 'task' = internal Hearth task; kind: 'event' = external calendar item (CalendarItem.kind determines event vs task).
export type Item = { kind: 'task'; data: Task } | { kind: 'event'; data: CalendarItem };

/** The household's location and time zone, held by the Display service and read by
 *  Weather, Birds, Almanac and the frame's renderer. Coordinates come as a pair. */
export interface HomeSettings {
    latitude: number | null;
    longitude: number | null;
    /** A label for the place, e.g. "Denver, Colorado". */
    name: string | null;
    /** IANA zone id, e.g. "America/Denver". */
    time_zone: string | null;
}

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

// ---- Client ----

class ApiClient {
    readonly tasks = {
        list: (): Promise<Task[] | null> => this.get<Task[]>('/tasks'),

        create: (input: CreateTaskInput): Promise<Task> => this.post<Task>('/tasks', input),

        update: (id: number, input: UpdateTaskInput): Promise<Task> => this.put<Task>(`/tasks/${id}`, input),

        delete: (id: number, series = false): Promise<boolean> =>
            this.del(`/tasks/${id}${series ? '?series=true' : ''}`),
    };

    readonly spotify = {
        disconnect: (): Promise<void> => this.del('/spotify/auth').then(() => {}),

        /** undefined = not connected, null = nothing playing; throws when Spotify can't be reached. */
        nowPlaying: async (): Promise<NowPlaying | null | undefined> => {
            const res = await fetch('/spotify/now-playing');
            if (res.status === 401) return undefined;
            if (res.status === 204) return null;
            if (!res.ok) throw new Error(`GET /spotify/now-playing failed: ${res.status}`);
            return res.json() as Promise<NowPlaying>;
        },

        status: (): Promise<SpotifyStatus | null> => this.get<SpotifyStatus>('/spotify/status'),

        /** null = no listening history yet; 'disconnected' / 'reconnect' when the login
         *  is missing or predates the history scopes; throws when Spotify can't be reached. */
        recent: async (): Promise<LastPlayed | null | 'disconnected' | 'reconnect'> => {
            // Bounded, so a hung service can't hold up /frame's ready signal.
            const res = await fetch('/spotify/recent', { signal: AbortSignal.timeout(8000) });
            if (res.status === 401) return 'disconnected';
            if (res.status === 403) return 'reconnect';
            if (res.status === 204) return null;
            if (!res.ok) throw new Error(`GET /spotify/recent failed: ${res.status}`);
            return res.json() as Promise<LastPlayed>;
        },
    };

    readonly display = {
        /** The saved frame settings; null when none have been saved yet, undefined when
         *  the Display service can't be reached. */
        settings: async (): Promise<FrameSettings | null | undefined> => {
            // Bounded, so a hung service can't hold up /frame's ready signal.
            const res = await fetch('/display/settings', { signal: AbortSignal.timeout(5000) }).catch(() => null);
            if (!res?.ok) return undefined;
            if (res.status === 204) return null;
            return res.json() as Promise<FrameSettings>;
        },

        saveSettings: (settings: FrameSettings): Promise<FrameSettings> =>
            this.put<FrameSettings>('/display/settings', settings),

        /** The mode the frame shows now; null when the Display service can't be reached. */
        mode: async (): Promise<ResolvedMode | null> => {
            // Bounded like settings(), so a hung service can't hold up /frame's ready signal.
            const res = await fetch('/display/mode', { signal: AbortSignal.timeout(5000) }).catch(() => null);
            return res?.ok ? (res.json() as Promise<ResolvedMode>) : null;
        },

        schedule: (): Promise<ModeSchedule | null> => this.get<ModeSchedule>('/display/mode/schedule'),

        saveSchedule: (schedule: ModeSchedule): Promise<ModeSchedule> =>
            this.put<ModeSchedule>('/display/mode/schedule', schedule),

        setOverride: (manual: ModeOverride): Promise<ModeOverride> =>
            this.put<ModeOverride>('/display/mode/override', manual),

        /** Back to the schedule. With `photoId`, only if that photo is the one pinned. */
        clearOverride: (photoId?: string): Promise<boolean> =>
            this.del(`/display/mode/override${photoId ? `?${new URLSearchParams({ photo_id: photoId })}` : ''}`),

        /** Steps to the next mode until the schedule's next change — the frame's Button B. */
        next: async (): Promise<ResolvedMode | null> => {
            const res = await fetch('/display/mode/next', { method: 'POST' }).catch(() => null);
            return res?.ok ? (res.json() as Promise<ResolvedMode>) : null;
        },

        /** The panel render and the frame's last check-in; null when Display can't be reached. */
        frameStatus: (): Promise<FrameStatus | null> => this.get<FrameStatus>('/display/frame/status'),

        /** The saved home location; null when none is saved yet, undefined when the
         *  Display service can't be reached. */
        home: async (): Promise<HomeSettings | null | undefined> => {
            const res = await fetch('/display/home').catch(() => null);
            if (!res?.ok) return undefined;
            if (res.status === 204) return null;
            return (res.json() as Promise<HomeSettings>).catch(() => undefined);
        },

        /** Throws HomeSettingsRejected with Display's reason on a 400. */
        saveHome: async (home: HomeSettings): Promise<HomeSettings> => {
            const res = await fetch('/display/home', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(home),
            });
            if (res.status === 400) throw new HomeSettingsRejected(await res.json().catch(() => 'invalid settings'));
            if (!res.ok) throw new Error(`PUT /display/home failed: ${res.status}`);
            return res.json() as Promise<HomeSettings>;
        },

        /** Cities matching `query`, each with its coordinates and time zone; null on failure. */
        places: (query: string): Promise<Place[] | null> =>
            this.get<Place[]>(`/display/places?${new URLSearchParams({ q: query })}`),

        /** The frame rendered and dithered to the panel's six inks, as a PNG URL. `screen`
         *  shows the exact inks the panel is sent; `paper` repaints them as they look on it. */
        previewUrl: (o: FrameOptions, look: 'screen' | 'paper'): string => {
            const params = frameParams(o);
            params.set('look', look);
            return `/display/preview?${params}`;
        },
    };

    readonly weather = {
        current: (): Promise<CurrentWeather | null> => this.get<CurrentWeather>('/weather/current'),

        forecast: (): Promise<ForecastDay[] | null> => this.get<ForecastDay[]>('/weather/forecast'),
    };

    readonly birds = {
        recent: (): Promise<BirdSighting[] | null> => this.get<BirdSighting[]>('/birds/recent'),
    };

    readonly rss = {
        articles: (urls: string[], count: number): Promise<RssFeedGroup[] | null> => {
            const params = new URLSearchParams({ count: String(count) });
            urls.forEach((url) => params.append('url', url));
            return this.get<RssFeedGroup[]>(`/rss/articles?${params}`);
        },
    };

    readonly trains = {
        departures: (stopKeys: string[]): Promise<StopDepartures[] | null> => {
            const params = new URLSearchParams();
            stopKeys.forEach((key) => params.append('stop', key));
            return this.get<StopDepartures[]>(`/trains/departures?${params}`);
        },
    };

    readonly almanac = {
        today: (): Promise<AlmanacResponse | null> => this.get<AlmanacResponse>('/almanac'),
    };

    readonly quote = {
        today: (): Promise<Quote | null> => this.get<Quote>('/quote'),
    };

    readonly calendar = {
        googleStatus: (): Promise<{ authenticated: boolean } | null> => this.get('/calendar/google/status'),

        googleDisconnect: (): Promise<boolean> => this.del('/calendar/google/auth'),

        items: (): Promise<CalendarItem[] | null> => this.get<CalendarItem[]>('/calendar/items'),

        toggleTask: (taskListId: string, taskId: string, completed: boolean): Promise<boolean> =>
            this.patch(`/calendar/google/tasks/${taskListId}/${taskId}`, { completed }),

        refreshCache: async (): Promise<boolean> => {
            const res = await fetch('/calendar/google/refresh', { method: 'POST' });
            return res.ok;
        },
    };

    readonly photos = {
        sources: (): Promise<string[]> => this.get<string[]>('/photos/sources').then((r) => r ?? ['unsplash']),

        random: (query: string, orientation: 'portrait' | 'landscape', source = 'unsplash'): Promise<Photo | null> => {
            const params = new URLSearchParams({ query, orientation, source });
            return this.get<Photo>(`/photos/random?${params}`);
        },

        list: (): Promise<UploadedPhoto[]> => this.get<UploadedPhoto[]>('/photos/uploads').then((r) => r ?? []),

        upload: async (files: File[]): Promise<BatchFileResult[]> => {
            const form = new FormData();
            for (const file of files) form.append('file', file);
            const res = await fetch('/photos/uploads', { method: 'POST', body: form });
            if (res.status === 413) {
                return files.map((f) => ({
                    file_name: f.name,
                    status: 'error' as const,
                    error: 'batch too large — try uploading fewer at a time',
                    photo: null,
                }));
            }
            if (!res.ok) {
                return files.map((f) => ({
                    file_name: f.name,
                    status: 'error' as const,
                    error: 'upload failed',
                    photo: null,
                }));
            }
            return res.json() as Promise<BatchFileResult[]>;
        },

        delete: (id: string): Promise<boolean> => this.del(`/photos/uploads/${id}`),

        setCaption: (id: string, caption: string | null): Promise<boolean> =>
            this.patch(`/photos/uploads/${id}`, { caption }),

        groups: (): Promise<PhotoGroup[]> => this.get<PhotoGroup[]>('/photos/groups').then((r) => r ?? []),

        createGroup: (name: string, photoIds: string[]): Promise<PhotoGroup> =>
            this.post<PhotoGroup>('/photos/groups', { name, photo_ids: photoIds }),

        updateGroup: (id: string, name: string, photoIds: string[]): Promise<PhotoGroup> =>
            this.put<PhotoGroup>(`/photos/groups/${id}`, { name, photo_ids: photoIds }),

        deleteGroup: (id: string): Promise<boolean> => this.del(`/photos/groups/${id}`),
    };

    /** null when the service can't be reached or doesn't answer with JSON. */
    private async get<T>(url: string): Promise<T | null> {
        try {
            const res = await fetch(url);
            if (!res.ok) return null;
            return (await res.json()) as T;
        } catch {
            return null;
        }
    }

    private async post<T>(url: string, body: unknown): Promise<T> {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
        if (!res.ok) throw new Error(`POST ${url} failed: ${res.status}`);
        return res.json() as Promise<T>;
    }

    private async put<T>(url: string, body: unknown): Promise<T> {
        const res = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
        if (!res.ok) throw new Error(`PUT ${url} failed: ${res.status}`);
        return res.json() as Promise<T>;
    }

    private async patch(url: string, body: unknown): Promise<boolean> {
        const res = await fetch(url, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
        return res.ok;
    }

    private async del(url: string): Promise<boolean> {
        const res = await fetch(url, { method: 'DELETE' });
        return res.ok;
    }
}

export const api = new ApiClient();
