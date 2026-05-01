<script lang="ts">
    import { onMount } from 'svelte';
    import { Music } from '@lucide/svelte';
    import { nowPlaying, refreshNowPlaying } from '$lib/SpotifyStore';
    import { disconnectSpotify } from '$lib/api';

    async function handleDisconnect() {
        await disconnectSpotify();
        await refreshNowPlaying();
    }

    let fetchedAt = $state(Date.now());
    let tickMs = $state(0);

    onMount(() => {
        refreshNowPlaying();

        const pollId = setInterval(async () => {
            await refreshNowPlaying();
            fetchedAt = Date.now();
            tickMs = 0;
        }, 5000);

        const tickId = setInterval(() => {
            tickMs = Date.now() - fetchedAt;
        }, 1000);

        return () => {
            clearInterval(pollId);
            clearInterval(tickId);
        };
    });

    const track = $derived($nowPlaying);

    const progressPct = $derived(
        track ? Math.min(100, ((track.progress_ms + (track.is_playing ? tickMs : 0)) / track.duration_ms) * 100) : 0
    );
</script>

{#if track === undefined}
    <a
        href="/spotify/auth"
        class="flex items-center gap-1.5 type-label text-[var(--text-4)] hover:text-[var(--text-3)] transition-colors"
    >
        <Music size={14} class="icon-md" />
        <span>Connect Spotify</span>
    </a>
{:else if track !== null}
    <div
        class="flex items-center gap-3 p-3 rounded-xl bg-[var(--surface)] border border-[var(--border)] min-w-0 group relative"
    >
        {#if track.album_art_url}
            <img
                src={track.album_art_url}
                alt={track.album_name}
                class="w-12 h-12 rounded-lg object-cover flex-shrink-0"
            />
        {:else}
            <div class="w-12 h-12 rounded-lg bg-[var(--border)] flex items-center justify-center flex-shrink-0">
                <Music size={20} class="icon-lg text-[var(--text-4)]" />
            </div>
        {/if}

        <div class="flex flex-col min-w-0 gap-1 flex-1">
            <div class="min-w-0">
                <p class="type-body font-medium text-[var(--text-1)] truncate leading-tight">
                    {track.title}
                </p>
                <p class="type-label text-[var(--text-3)] truncate leading-tight">{track.artist}</p>
            </div>
            <div class="w-full h-1 bg-[var(--border)] rounded-full overflow-hidden">
                <div
                    class="h-full bg-[var(--text-3)] rounded-full"
                    style="width: {progressPct}%; transition: none"
                ></div>
            </div>
        </div>
        <button
            onclick={handleDisconnect}
            class="absolute top-1 right-1 opacity-0 group-hover:opacity-100 transition-opacity text-[var(--text-4)] hover:text-[var(--text-2)] p-0.5"
            title="Disconnect Spotify">×</button
        >
    </div>
{:else}
    <button
        onclick={handleDisconnect}
        class="flex items-center gap-1.5 type-label text-[var(--text-4)] hover:text-[var(--text-3)] transition-colors"
        title="Disconnect Spotify"
    >
        <Music size={14} class="icon-md" />
        <span>Nothing playing · Disconnect</span>
    </button>
{/if}
