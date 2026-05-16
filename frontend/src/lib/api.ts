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
}

export interface CurrentWeather {
    temperature_f: number;
    weather_code: number;
    description: string;
    wind_mph: number;
    fetched_at: string;
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
}

export interface BatchFileResult {
    file_name: string;
    status: 'ok' | 'duplicate' | 'error';
    error: string | null;
    photo: UploadedPhoto | null;
}

// ---- Client ----

class ApiClient {
    readonly tasks = {
        list: (): Promise<Task[]> =>
            this.get<Task[]>('/tasks').then((r) => r ?? []),

        create: (input: CreateTaskInput): Promise<Task> =>
            this.post<Task>('/tasks', input),

        update: (id: number, input: UpdateTaskInput): Promise<Task> =>
            this.put<Task>(`/tasks/${id}`, input),

        delete: (id: number, series = false): Promise<void> =>
            this.del(`/tasks/${id}${series ? '?series=true' : ''}`).then(() => {}),
    };

    readonly spotify = {
        status: (): Promise<SpotifyStatus> =>
            this.get<SpotifyStatus>('/spotify/status').then((r) => r ?? { authenticated: false }),

        disconnect: (): Promise<void> =>
            this.del('/spotify/auth').then(() => {}),

        nowPlaying: async (): Promise<NowPlaying | null | undefined> => {
            const res = await fetch('/spotify/now-playing');
            if (res.status === 401) return undefined;
            if (res.status === 204) return null;
            return res.json() as Promise<NowPlaying>;
        },
    };

    readonly weather = {
        current: (): Promise<CurrentWeather | null> =>
            this.get<CurrentWeather>('/weather/current'),

        forecast: (): Promise<ForecastDay[]> =>
            this.get<ForecastDay[]>('/weather/forecast').then((r) => r ?? []),
    };

    readonly photos = {
        sources: (): Promise<string[]> =>
            this.get<string[]>('/photos/sources').then((r) => r ?? ['unsplash']),

        random: (
            query: string,
            orientation: 'portrait' | 'landscape',
            source = 'unsplash',
        ): Promise<Photo | null> => {
            const params = new URLSearchParams({ query, orientation, source });
            return this.get<Photo>(`/photos/random?${params}`);
        },

        list: (): Promise<UploadedPhoto[]> =>
            this.get<UploadedPhoto[]>('/photos/uploads').then((r) => r ?? []),

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

        delete: (id: string): Promise<boolean> =>
            this.del(`/photos/uploads/${id}`),
    };

    private async get<T>(url: string): Promise<T | null> {
        const res = await fetch(url);
        if (!res.ok) return null;
        return res.json() as Promise<T>;
    }

    private async post<T>(url: string, body: unknown): Promise<T> {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
        return res.json() as Promise<T>;
    }

    private async put<T>(url: string, body: unknown): Promise<T> {
        const res = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        });
        return res.json() as Promise<T>;
    }

    private async del(url: string): Promise<boolean> {
        const res = await fetch(url, { method: 'DELETE' });
        return res.ok;
    }
}

export const api = new ApiClient();
