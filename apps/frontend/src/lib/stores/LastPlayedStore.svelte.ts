import { api, type LastPlayed } from '#lib/api.ts';

class LastPlayedStore {
    /** null until loaded, or when there's no listening history yet. */
    lastPlayed = $state<LastPlayed | null>(null);
    /** Why there's nothing to show: no Spotify login, or one that needs reconnecting. */
    connection = $state<'connected' | 'disconnected' | 'reconnect'>('connected');
    error = $state(false);
}

export const lastPlayedStore = new LastPlayedStore();

/** Loads the card and waits for its album art, so the frame's screenshot never catches it half-drawn. */
export async function loadLastPlayed() {
    try {
        const result = await api.spotify.recent();
        lastPlayedStore.error = false;
        if (result === 'disconnected' || result === 'reconnect') {
            lastPlayedStore.connection = result;
            lastPlayedStore.lastPlayed = null;
            return;
        }
        lastPlayedStore.connection = 'connected';
        lastPlayedStore.lastPlayed = result;
        const art = result?.track?.album_art_url;
        if (art) await preload(art);
    } catch {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last card.
        lastPlayedStore.error = lastPlayedStore.lastPlayed === null;
    }
}

function preload(url: string): Promise<void> {
    // No crossOrigin: the frame only draws the art, it never reads its pixels back.
    const img = new Image();
    img.src = url;
    return img.decode().catch(() => {});
}
