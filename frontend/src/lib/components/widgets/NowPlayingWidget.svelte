<script lang="ts">
    import { onMount } from 'svelte';
    import { Music } from '@lucide/svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';

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

    const track = $derived(spotifyStore.nowPlaying);

    const progressPct = $derived(
        track ? Math.min(100, ((track.progress_ms + (track.is_playing ? tickMs : 0)) / track.duration_ms) * 100) : 0
    );
</script>

{#if track === undefined}
    <p class="type-body text-[var(--text-2)]">Spotify not connected - add it in Settings.</p>
{:else if track !== null}
    <div class="flex min-w-0 items-center gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-3">
        {#if track.album_art_url}
            <img
                src={track.album_art_url}
                alt={track.album_name}
                class="h-12 w-12 flex-shrink-0 rounded-lg object-cover"
            />
        {:else}
            <div class="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-lg bg-[var(--border)]">
                <Music class="icon-lg text-[var(--text-4)]" />
            </div>
        {/if}

        <div class="flex min-w-0 flex-1 flex-col gap-1">
            <div class="min-w-0">
                <p class="type-body truncate font-medium leading-tight text-[var(--text-1)]">
                    {track.title}
                </p>
                <p class="type-label truncate leading-tight text-[var(--text-2)]">{track.artist}</p>
            </div>
            <div class="h-1 w-full overflow-hidden rounded-full bg-[var(--border)]">
                <div
                    class="h-full rounded-full bg-[var(--text-3)]"
                    style="width: {progressPct}%; transition: none"
                ></div>
            </div>
        </div>
    </div>
{:else}
    <p class="type-body text-[var(--text-2)]">Nothing playing.</p>
{/if}
