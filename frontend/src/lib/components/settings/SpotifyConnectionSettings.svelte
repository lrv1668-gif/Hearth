<script lang="ts">
    import { onMount } from 'svelte';
    import { Disc3 } from '@lucide/svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { api } from '$lib/api';

    onMount(() => refreshNowPlaying());

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);

    async function handleDisconnect() {
        await api.spotify.disconnect();
        await refreshNowPlaying();
    }
</script>

<div class="flex items-center gap-4">
    <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-(--surface-hi)">
        <Disc3 class="icon-md text-(--text-2)" />
    </div>
    <div class="flex flex-1 items-center justify-between gap-4">
        <div>
            <p class="type-body font-medium text-(--text-1)">Spotify</p>
            <p class="type-label text-(--text-2)">
                {spotifyConnected ? 'Connected — now playing data is live.' : 'Not connected.'}
            </p>
        </div>
        <div class="flex items-center gap-3">
            {#if spotifyConnected}
                <div class="flex flex-col items-center gap-2 sm:flex-row">
                    <span class="type-body text-(--text-1)">Connected</span>
                    <button
                        onclick={handleDisconnect}
                        class="type-body rounded-full border border-(--border) px-3 py-1 text-(--text-1) transition-colors hover:border-(--text-2) hover:text-(--text-1)"
                    >
                        Disconnect
                    </button>
                </div>
            {:else}
                <a
                    href="/spotify/auth"
                    class="type-body rounded-full border border-(--border) px-3 py-1 text-(--text-1) transition-colors hover:border-(--text-2) hover:text-(--text-1)"
                >
                    Connect
                </a>
            {/if}
        </div>
    </div>
</div>
