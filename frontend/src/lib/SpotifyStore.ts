import { writable } from 'svelte/store';
import { fetchNowPlaying, type NowPlaying } from './api';

// undefined = not authenticated, null = authenticated but nothing playing
export const nowPlaying = writable<NowPlaying | null | undefined>(undefined);

export async function refreshNowPlaying() {
    const track = await fetchNowPlaying();
    nowPlaying.set(track);
}
