<script lang="ts">
    import { onMount } from 'svelte';
    import { Settings } from '@lucide/svelte';
    import { toggleableWidgets } from '$lib/constants/widgets';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { api } from '$lib/api';
    import WidgetOrderEditor from '$lib/components/settings/WidgetOrderEditor.svelte';
    import SubTitle from '../SubTitle.svelte';
    import FeedSettings from './FeedSettings.svelte';

    onMount(() => refreshNowPlaying());

    async function handleDisconnect() {
        await api.spotify.disconnect();
        await refreshNowPlaying();
    }

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);
</script>

<div class="flex flex-col gap-2 space-y-4">
    <div>
        <SubTitle
            subTitleText="Widget Layout"
            subTitleDescription="Drag widgets between columns to reorder them on the schedule page. Drag the middle slider to set what you
                want the column width to be."
        />
        <WidgetOrderEditor />
    </div>

    <div>
        <SubTitle
            subTitleText="Visibility"
            subTitleDescription="Enable/disable widgets you want to appear on the Schedules page."
        />
        <div class="flex flex-col gap-2">
            {#each toggleableWidgets as widget}
                <div>
                    <label class="flex cursor-pointer items-center gap-3">
                        <div class="mt-0.5 shrink-0">
                            <Toggle
                                checked={settings.enabledWidgets.includes(widget.id)}
                                onchange={() => settings.toggleWidget(widget.id)}
                            />
                        </div>
                        <div>
                            <p class="type-body select-none text-[var(--text-1)]">{widget.label}</p>
                            <p class="type-label select-none text-[var(--text-2)]">{widget.description}</p>
                        </div>
                    </label>

                    {#if widget.id === 'now-playing' && settings.enabledWidgets.includes('now-playing')}
                        <div class="ml-12 mt-1.5 border-l-2 border-l-[var(--border)] p-4">
                            <div class="flex flex-row gap-2">
                                <Settings class="icon-sm" />
                                {#if spotifyConnected}
                                    <p class="type-label border-r border-r-[var(--text-3)] pr-2 text-[var(--text-1)]">
                                        Spotify connected
                                    </p>
                                    <button
                                        onclick={handleDisconnect}
                                        class="type-label text-[var(--text-2)] transition-colors hover:text-[var(--text-1)]"
                                    >
                                        Disconnect
                                    </button>
                                {:else}
                                    <p class="type-label border-r border-r-[var(--text-3)] pr-2 text-[var(--text-1)]">
                                        Spotify disconnected
                                    </p>
                                    <a
                                        href="/spotify/auth"
                                        class="type-label inline-flex items-center gap-1 text-[var(--text-2)] transition-colors hover:text-[var(--text-1)]"
                                    >
                                        Connect
                                    </a>
                                {/if}
                            </div>
                        </div>
                    {/if}
                </div>
            {/each}
        </div>
    </div>

    {#if settings.enabledWidgets.includes('rss-feeds')}
        <div class="ml-12 border-l-2 border-l-[var(--border)] pl-4">
            <FeedSettings />
        </div>
    {/if}
</div>
