<script lang="ts">
    import { onMount } from 'svelte';
    import { Disc3 } from '@lucide/svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { api } from '$lib/api';
    import Button from '../ui/Button.svelte';
    import ConnectionRow from '../ui/ConnectionRow.svelte';

    onMount(() => refreshNowPlaying());

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);

    async function handleDisconnect() {
        await api.spotify.disconnect();
        await refreshNowPlaying();
    }
</script>

<ConnectionRow
    icon={Disc3}
    name="Spotify"
    status={spotifyConnected ? 'Connected — now playing data is live.' : 'Not connected.'}
>
    {#snippet actions()}
        {#if spotifyConnected}
            <Button onclick={handleDisconnect}>Disconnect</Button>
        {:else}
            <Button href="/spotify/auth">Connect</Button>
        {/if}
    {/snippet}
</ConnectionRow>
