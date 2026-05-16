import { api, type NowPlaying } from '../api';

// undefined = not authenticated, null = authenticated but nothing playing
export const spotifyStore = $state({ nowPlaying: undefined as NowPlaying | null | undefined });

export async function refreshNowPlaying() {
    spotifyStore.nowPlaying = await api.spotify.nowPlaying();
}
