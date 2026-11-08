<script lang="ts">
    import { onMount } from 'svelte';
    import { fly } from 'svelte/transition';
    import { Music, Pause, Play } from '@lucide/svelte';
    import { SECOND, poll } from '#lib/utils.ts';
    import { formatTrackTime, trackPosition } from '#lib/music.ts';
    import { settingsHref } from '#lib/constants/settingsSections.ts';
    import { spotifyStore, refreshNowPlaying, pollNowPlaying } from '#lib/stores/SpotifyStore.svelte.ts';
    import SkeletonLoader from '#lib/components/SkeletonLoader.svelte';
    import Skeleton from '#lib/components/Skeleton.svelte';

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));

    onMount(() => {
        loadPromise = refreshNowPlaying();
        return poll(pollNowPlaying, 5 * SECOND);
    });

    const track = $derived(spotifyStore.nowPlaying);
    const playing = $derived(track?.is_playing ?? false);

    // Ticks once a second while playing so the bar moves between the 5 s polls
    let now = $state(Date.now());
    $effect(() => {
        if (!playing) return;
        now = Date.now();
        const id = setInterval(() => (now = Date.now()), SECOND);
        return () => clearInterval(id);
    });

    const position = $derived(track ? trackPosition(track, spotifyStore.fetchedAt, now) : 0);
    const percent = $derived(track && track.duration_ms > 0 ? (position / track.duration_ms) * 100 : 0);
</script>

<!--
@component
The dashboard's Now Playing widget: a record that spins while Spotify plays (the album art is
its label), a play/pause icon and a progress bar that ticks between polls. Nothing
playing leaves the record still; no Spotify login points to Settings → Connections. It polls
`/spotify/now-playing` every 5 s, so it only polls while it's on the dashboard.
-->
<SkeletonLoader promise={loadPromise}>
    {#snippet skeleton()}
        <div class="flex items-center gap-4">
            <Skeleton class="size-24 shrink-0 rounded-full" />
            <div class="flex flex-1 flex-col gap-2">
                <Skeleton class="h-4 w-2/3" />
                <Skeleton class="h-4 w-1/2" />
                <Skeleton class="h-1 w-full" />
            </div>
        </div>
    {/snippet}

    {#if track === undefined}
        <div class="flex flex-col gap-1 text-(--text-2)">
            <p class="type-label">Spotify isn't connected.</p>
            <a
                href={settingsHref('connections')}
                class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)"
            >
                Connect Spotify in Settings →
            </a>
        </div>
    {:else}
        <section aria-label="Now playing" class="flex min-w-0 items-center gap-4" class:paused={!playing}>
            <div class="record size-24 shrink-0" aria-hidden="true">
                <div class="label">
                    {#if track?.album_art_url}
                        <img src={track.album_art_url} alt="" class="size-full object-cover" />
                    {:else}
                        <Music class="icon-xs text-(--accent-fg)" />
                    {/if}
                </div>
                <span class="spindle"></span>
            </div>

            {#if track}
                {#key `${track.title}\u0000${track.artist}`}
                    <div class="flex min-w-0 flex-1 flex-col gap-1" in:fly={{ y: 6, duration: 300 }}>
                        <div class="flex min-w-0 items-center gap-2">
                            <p class="type-body min-w-0 flex-1 truncate font-bold text-(--text-1)">{track.title}</p>
                            {#if playing}
                                <Play class="icon-sm shrink-0 fill-current text-(--accent)" aria-label="Playing" />
                            {:else}
                                <Pause class="icon-sm shrink-0 fill-current text-(--text-3)" aria-label="Paused" />
                            {/if}
                        </div>
                        <p class="type-label truncate text-(--text-2)">
                            {track.artist}{track.album_name ? ` · ${track.album_name}` : ''}
                        </p>

                        <div class="mt-1 flex items-center gap-2">
                            <div
                                class="h-1 min-w-0 flex-1 overflow-hidden rounded-full bg-(--border)"
                                aria-hidden="true"
                            >
                                <div
                                    class="h-full rounded-full bg-(--accent) transition-[width] duration-1000 ease-linear"
                                    style="width: {percent}%"
                                ></div>
                            </div>
                            <span class="type-label shrink-0 text-(--text-2) tabular-nums">
                                {formatTrackTime(position)} / {formatTrackTime(track.duration_ms)}
                            </span>
                        </div>
                    </div>
                {/key}
            {:else}
                <p class="type-label text-(--text-2)">Nothing playing right now.</p>
            {/if}
        </section>
    {/if}
</SkeletonLoader>

<style>
    /* A vinyl disc: dark body with faint grooves, the album art as its centre label. Records
       are dark on every theme, so the body is the text/background midpoint pressed dark
       (keeping the theme's tint), with a --border rim to lift it off dark cards. */
    .record {
        --vinyl-mid: color-mix(in oklab, var(--text-1), var(--bg));
        --vinyl: oklch(from var(--vinyl-mid) calc(l * 0.32) c h);
        --groove: oklch(from var(--vinyl-mid) calc(l * 0.46) c h);
        position: relative;
        display: grid;
        place-items: center;
        border-radius: 9999px;
        background:
            radial-gradient(
                circle,
                transparent 54%,
                color-mix(in srgb, var(--surface-hi) 30%, transparent) 55%,
                transparent 57%
            ),
            repeating-radial-gradient(circle, var(--vinyl) 0 1.5px, var(--groove) 1.5px 3px);
        box-shadow:
            0 0 0 1px var(--border),
            0 1px 4px color-mix(in srgb, var(--vinyl) 35%, transparent);
        /* Slow, like a turntable seen from across the room — enough to feel alive, not to pull the eye */
        animation: spin 24s linear infinite;
    }
    .label {
        display: grid;
        place-items: center;
        width: 64%;
        height: 64%;
        overflow: hidden;
        border-radius: 9999px;
        background: var(--accent);
    }
    .spindle {
        position: absolute;
        width: 6%;
        height: 6%;
        border-radius: 9999px;
        background: var(--bg);
    }

    /* Paused: the record stops where it is */
    .paused .record {
        animation-play-state: paused;
    }

    @keyframes spin {
        to {
            transform: rotate(360deg);
        }
    }

    @media (prefers-reduced-motion: reduce) {
        .record {
            animation: none;
        }
    }
</style>
