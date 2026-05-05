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

    interface Props {
        align?: 'left' | 'right';
    }
    let { align = 'left' }: Props = $props();

    const progressPct = $derived(
        track ? Math.min(100, ((track.progress_ms + (track.is_playing ? tickMs : 0)) / track.duration_ms) * 100) : 0
    );
</script>

{#if track === undefined}
    <p class="type-label text-[var(--text-2)]">Spotify not connected - add it in Settings.</p>
{:else if track !== null}
    <div class="flex items-center gap-3 p-3 rounded-xl bg-[var(--surface)] border border-[var(--border)] min-w-0">
        {#if track.album_art_url}
            <img
                src={track.album_art_url}
                alt={track.album_name}
                class="w-12 h-12 rounded-lg object-cover flex-shrink-0"
            />
        {:else}
            <div class="w-12 h-12 rounded-lg bg-[var(--border)] flex items-center justify-center flex-shrink-0">
                <Music class="icon-lg text-[var(--text-4)]" />
            </div>
        {/if}

        <div class="flex flex-col min-w-0 gap-1 flex-1">
            <div class="min-w-0">
                <p class="type-body font-medium text-[var(--text-1)] truncate leading-tight">
                    {track.title}
                </p>
                <p class="type-label text-[var(--text-2)] truncate leading-tight">{track.artist}</p>
            </div>
            <div class="w-full h-1 bg-[var(--border)] rounded-full overflow-hidden">
                <div
                    class="h-full bg-[var(--text-3)] rounded-full"
                    style="width: {progressPct}%; transition: none"
                ></div>
            </div>
        </div>
    </div>
{:else}
    <p class="type-label text-[var(--text-2)] {align === 'right' ? 'text-right' : ''}">Nothing playing.</p>
{/if}
