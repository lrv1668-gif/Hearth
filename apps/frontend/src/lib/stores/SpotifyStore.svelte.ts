import { api, type NowPlaying } from '../api';
import { MINUTE } from '#lib/utils.ts';

// undefined = not authenticated, null = authenticated but nothing playing
export const spotifyStore = $state({ nowPlaying: undefined as NowPlaying | null | undefined });

let lastChecked = 0;

export async function refreshNowPlaying() {
    lastChecked = Date.now();
    try {
        spotifyStore.nowPlaying = await api.spotify.nowPlaying();
    } catch {
        // Spotify unreachable or rate-limited: keep showing what's there until it answers
    }
}

/**
 * The dashboard's frequent poll. Skips while the tab is hidden, and while Spotify isn't
 * connected checks about once a minute — connecting reloads the page anyway.
 */
export async function pollNowPlaying() {
    if (typeof document !== 'undefined' && document.hidden) return;
    if (spotifyStore.nowPlaying === undefined && Date.now() - lastChecked < MINUTE) return;
    await refreshNowPlaying();
}
