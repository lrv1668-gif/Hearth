import { del, get } from './http.ts';

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

export const spotify = {
    disconnect: (): Promise<void> => del('/spotify/auth').then(() => {}),

    /** undefined = not connected, null = nothing playing; throws when Spotify can't be reached. */
    nowPlaying: async (): Promise<NowPlaying | null | undefined> => {
        const res = await fetch('/spotify/now-playing');
        if (res.status === 401) return undefined;
        if (res.status === 204) return null;
        if (!res.ok) throw new Error(`GET /spotify/now-playing failed: ${res.status}`);
        return res.json() as Promise<NowPlaying>;
    },

    status: (): Promise<SpotifyStatus | null> => get<SpotifyStatus>('/spotify/status'),

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
