<script lang="ts">
    import { onMount } from 'svelte';
    import { SECOND, poll } from '$lib/utils';
    import { Monitor, Music } from '@lucide/svelte';
    import { spotifyStore, refreshNowPlaying, pollNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';

    onMount(() => {
        refreshNowPlaying();
        return poll(pollNowPlaying, 5 * SECOND);
    });

    function formatRefreshed(iso: string): string {
        return new Date(iso).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }

    let { class: extraClass = '' }: { class?: string } = $props();
</script>

<footer class="hidden shrink-0 border-t-2 border-(--border) px-8 py-2 lg:block {extraClass}">
    <div class="flex items-center justify-center gap-2">
        {#if spotifyStore.nowPlaying}
            <span
                class="type-label flex min-w-0 items-center gap-1.5 truncate rounded-lg bg-(--surface) p-2 text-(--text-2)"
            >
                <Music class="icon-sm flex-shrink-0" />
                <span class="font-bold text-(--text-1)">{spotifyStore.nowPlaying.title}</span>
                <span>{spotifyStore.nowPlaying.artist}</span>
            </span>
        {/if}

        {#if weatherStore.current?.fetched_at}
            <div class="type-label flex flex-shrink-0 items-center gap-2 rounded-lg bg-(--surface) p-2">
                <Monitor class="icon-sm text-(--text-2)" />
                <p>Refreshed @ {formatRefreshed(weatherStore.current.fetched_at)}</p>
            </div>
        {/if}
    </div>
</footer>
