export interface Task {
    id: number;
    title: string;
    done: boolean;
    due_date: string | null;
    due_time: string | null;
    created_at: string;
}

export async function fetchTasks(): Promise<Task[]> {
    const res = await fetch('/tasks');
    return res.json();
}

export async function createTask(title: string, due_date?: string, due_time?: string): Promise<Task> {
    const res = await fetch('/tasks', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, due_date: due_date ?? null, due_time: due_time ?? null }),
    });
    return res.json();
}

export async function updateTask(id: number, done: boolean): Promise<Task> {
    const res = await fetch(`/tasks/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ done }),
    });
    return res.json();
}

export async function deleteTask(id: number): Promise<void> {
    await fetch(`/tasks/${id}`, { method: 'DELETE' });
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

export async function fetchSpotifyStatus(): Promise<SpotifyStatus> {
    const res = await fetch('/spotify/status');
    return res.json();
}

// undefined = not authenticated, null = authenticated but nothing playing, NowPlaying = track data
export async function disconnectSpotify(): Promise<void> {
    await fetch('/spotify/auth', { method: 'DELETE' });
}

export async function fetchNowPlaying(): Promise<NowPlaying | null | undefined> {
    const res = await fetch('/spotify/now-playing');
    if (res.status === 401) return undefined;
    if (res.status === 204) return null;
    return res.json();
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
}

export async function fetchCurrentWeather(): Promise<CurrentWeather | null> {
    const res = await fetch('/weather/current');
    if (!res.ok) return null;
    return res.json();
}

export async function fetchWeatherForecast(): Promise<ForecastDay[]> {
    const res = await fetch('/weather/forecast');
    if (!res.ok) return [];
    return res.json();
}
