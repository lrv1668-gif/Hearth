<script lang="ts">
    import { onMount } from 'svelte';
    import { Disc3 } from '@lucide/svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { api } from '$lib/api';
    import Button from '../ui/Button.svelte';
    import ConnectionRow from '../ui/ConnectionRow.svelte';

    // A login saved before the frame's Last Played card asked for listening history
    let needsReconnect = $state(false);

    async function refresh() {
        await refreshNowPlaying();
        needsReconnect = (await api.spotify.status())?.needs_reconnect ?? false;
    }

    onMount(() => {
        refresh();
    });

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);

    async function handleDisconnect() {
        await api.spotify.disconnect();
        await refresh();
    }
</script>

<ConnectionRow
    icon={Disc3}
    name="Spotify"
    status={!spotifyConnected
        ? 'Not connected.'
        : needsReconnect
          ? 'Connected — reconnect to let the frame show what you’ve played.'
          : 'Connected — now playing and listening history are live.'}
>
    {#snippet actions()}
        {#if spotifyConnected}
            {#if needsReconnect}
                <Button href="/spotify/auth" variant="primary">Reconnect</Button>
            {/if}
            <Button onclick={handleDisconnect}>Disconnect</Button>
        {:else}
            <Button href="/spotify/auth">Connect</Button>
        {/if}
    {/snippet}
</ConnectionRow>
