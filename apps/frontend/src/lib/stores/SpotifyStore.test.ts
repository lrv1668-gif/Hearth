import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { NowPlaying } from '#lib/api.ts';
import { pollNowPlaying, refreshNowPlaying, spotifyStore } from './SpotifyStore.svelte.ts';

const track: NowPlaying = {
    title: 'Clair de Lune',
    artist: 'Debussy',
    album_name: 'Suite bergamasque',
    album_art_url: null,
    progress_ms: 61000,
    duration_ms: 300000,
    is_playing: true,
};

const fetchMock = vi.fn<typeof fetch>();
const respond = (status: number, body?: unknown) =>
    fetchMock.mockResolvedValueOnce(new Response(body === undefined ? null : JSON.stringify(body), { status }));

beforeEach(() => {
    vi.useFakeTimers();
    vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
    fetchMock.mockReset();
    vi.unstubAllGlobals();
    vi.useRealTimers();
});

describe('refreshNowPlaying', () => {
    it('shows the track Spotify returns', async () => {
        respond(200, track);
        await refreshNowPlaying();
        expect(spotifyStore.nowPlaying).toEqual(track);
    });

    it('keeps the current track when Spotify is unavailable', async () => {
        spotifyStore.nowPlaying = track;
        respond(502, { error: 'spotify unavailable' });
        await expect(refreshNowPlaying()).resolves.toBeUndefined();
        expect(spotifyStore.nowPlaying).toEqual(track);
    });

    it('keeps the current track when the service returns an empty error body', async () => {
        spotifyStore.nowPlaying = track;
        respond(500);
        await refreshNowPlaying();
        expect(spotifyStore.nowPlaying).toEqual(track);
    });

    it('marks Spotify disconnected only on a 401', async () => {
        spotifyStore.nowPlaying = track;
        respond(401);
        await refreshNowPlaying();
        expect(spotifyStore.nowPlaying).toBeUndefined();
    });
});

describe('pollNowPlaying', () => {
    it('checks a disconnected account about once a minute, not every poll', async () => {
        respond(401);
        await refreshNowPlaying();
        fetchMock.mockClear();

        vi.advanceTimersByTime(5_000);
        await pollNowPlaying();
        expect(fetchMock).not.toHaveBeenCalled();

        respond(401);
        vi.advanceTimersByTime(60_000);
        await pollNowPlaying();
        expect(fetchMock).toHaveBeenCalledOnce();
    });

    it('polls every time while connected', async () => {
        spotifyStore.nowPlaying = null;
        respond(204);
        await pollNowPlaying();
        respond(204);
        await pollNowPlaying();
        expect(fetchMock).toHaveBeenCalledTimes(2);
    });
});
