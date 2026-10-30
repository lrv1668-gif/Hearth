<script lang="ts">
    import { Disc3 } from '@lucide/svelte';
    import { lastPlayedStore } from '#lib/stores/LastPlayedStore.svelte.ts';
    import { playedLabel } from '#lib/music.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';

    // `size`: a template slot's size class — S drops the sleeve for the text alone, L
    // grows it to the slot's full height (up to half its width; the slot body is a size
    // container) with a larger title. Omitted (the frame's columns) is M.
    let { now, size = 'm' }: { now: Date; size?: FrameSize } = $props();

    const track = $derived(lastPlayedStore.lastPlayed?.track ?? null);
    const topArtist = $derived(lastPlayedStore.lastPlayed?.top_artist ?? null);
    // A fixed 5-line square (its budget in lib/frame.ts): big enough to read as a record
    // sleeve once dithered, and as tall as the four lines of text beside it.
    const ART_LINES = 5;
    const art = $derived(size === 'l' ? 'width: min(100cqh, 50cqw)' : `width: ${ART_LINES}lh`);
</script>

<!--
@component
The e-paper frame's Last Played card: the latest track from the Spotify history with its
album art, when it played, and this month's top artist. History, not now-playing, so it
stays true between the frame's scheduled refreshes.
-->
{#if lastPlayedStore.connection === 'disconnected'}
    <p class="type-body text-(--text-2)">Connect Spotify in Settings to show what you’ve played.</p>
{:else if lastPlayedStore.connection === 'reconnect'}
    <p class="type-body text-(--text-2)">Reconnect Spotify in Settings to show what you’ve played.</p>
{:else if lastPlayedStore.error}
    <p class="type-body text-(--text-2)">Listening history unavailable.</p>
{:else if !track && !topArtist}
    <p class="type-body text-(--text-2)">Nothing played lately.</p>
{:else}
    <div class="type-body flex gap-[0.75em]">
        {#if track && size !== 's'}
            <!-- 3px frame around the sleeve, so pale covers keep an edge on the paper. -->
            <div
                class="flex aspect-square shrink-0 items-center justify-center overflow-hidden border-3 border-(--border) bg-(--block) text-(--on-block)"
                style={art}
            >
                {#if track.album_art_url}
                    <img src={track.album_art_url} alt="" class="h-full w-full object-cover" />
                {:else}
                    <Disc3 class="icon-lg" />
                {/if}
            </div>
        {/if}
        <div class="flex min-w-0 flex-1 flex-col">
            {#if track}
                <p
                    class="line-clamp-2 leading-snug font-semibold text-(--text-1) {size === 'l'
                        ? 'type-title mb-2 font-(family-name:--display-family) tracking-[-0.01em]'
                        : ''}"
                >
                    {track.title}
                </p>
                <p class="type-label truncate text-(--text-2)">{track.artist}</p>
                <p class="type-label truncate text-(--text-2)">Played {playedLabel(track.played_at, now)}</p>
            {/if}
            {#if topArtist}
                <p class="type-label truncate text-(--text-2) {track ? 'mt-auto' : ''}">
                    This month · <span class="font-semibold text-(--text-1)">{topArtist}</span>
                </p>
            {/if}
        </div>
    </div>
{/if}
