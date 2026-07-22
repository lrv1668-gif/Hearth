<script lang="ts">
    import { onMount } from 'svelte';
    import { Settings } from '@lucide/svelte';
    import { calendarStore, loadCalendarStatus } from '$lib/stores/CalendarStore.svelte.ts';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { api } from '$lib/api';
    import SubTitle from '../SubTitle.svelte';

    onMount(() => {
        loadCalendarStatus();
        refreshNowPlaying();
    });

    async function handleSpotifyDisconnect() {
        await api.spotify.disconnect();
        await refreshNowPlaying();
    }

    async function handleCalendarDisconnect() {
        await api.calendar.googleDisconnect();
        calendarStore.items = [];
        await loadCalendarStatus();
    }

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);
</script>

<div class="flex flex-col gap-6">
    <div class="flex flex-col">
        <SubTitle
            subTitleText="Spotify"
            subTitleDescription="Show your currently playing Spotify track in the Now Playing widget."
        />
        <div class="flex flex-row items-center gap-2">
            <Settings class="icon-sm" />
            {#if spotifyConnected}
                <p class="type-label border-r border-r-(--text-3) pr-2 text-(--text-1)">Spotify connected</p>
                <button
                    onclick={handleSpotifyDisconnect}
                    class="type-label text-(--text-2) transition-colors hover:text-(--text-1)"
                >
                    Disconnect
                </button>
            {:else}
                <p class="type-label border-r border-r-(--text-3) pr-2 text-(--text-1)">Spotify disconnected</p>
                <a
                    href="/spotify/auth"
                    class="type-label inline-flex items-center gap-1 text-(--text-2) transition-colors hover:text-(--text-1)"
                >
                    Connect
                </a>
            {/if}
        </div>
    </div>

    <div class="flex flex-col">
        <SubTitle
            subTitleText="Google Calendar"
            subTitleDescription="Show Google Calendar events in the calendar and upcoming tasks views."
        />
        <div class="flex flex-row items-center gap-2">
            <Settings class="icon-sm" />
            {#if calendarStore.googleConnected}
                <p class="type-label border-r border-r-(--text-3) pr-2 text-(--text-1)">Google Calendar connected</p>
                <button
                    onclick={handleCalendarDisconnect}
                    class="type-label text-(--text-2) transition-colors hover:text-(--text-1)"
                >
                    Disconnect
                </button>
            {:else}
                <p class="type-label border-r border-r-(--text-3) pr-2 text-(--text-1)">Google Calendar disconnected</p>
                <a
                    href="/calendar/google/auth"
                    class="type-label inline-flex items-center gap-1 text-(--text-2) transition-colors hover:text-(--text-1)"
                >
                    Connect
                </a>
            {/if}
        </div>
    </div>
</div>
