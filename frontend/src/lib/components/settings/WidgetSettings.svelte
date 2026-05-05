<script lang="ts">
    import { onMount } from 'svelte';
    import { Settings } from '@lucide/svelte';
    import { toggleableWidgets } from '$lib/constants/widgets';
    import { settings, toggleWidget } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
    import { spotifyStore, refreshNowPlaying } from '$lib/stores/SpotifyStore.svelte.ts';
    import { disconnectSpotify } from '$lib/api';
    import WidgetOrderEditor from '$lib/components/settings/WidgetOrderEditor.svelte';
    import SubTitle from '../SubTitle.svelte';

    onMount(() => refreshNowPlaying());

    async function handleDisconnect() {
        await disconnectSpotify();
        await refreshNowPlaying();
    }

    const spotifyConnected = $derived(spotifyStore.nowPlaying !== undefined);
</script>

<div class="space-y-4 flex flex-col gap-2">
    <div>
        <SubTitle
            subTitleText="Visibility"
            subTitleDescription="Enable/disable widgets you want to appear on the Schedules page."
        />
        <div class="flex flex-col gap-2">
            {#each toggleableWidgets as widget}
                <div>
                    <label class="flex items-center gap-3 cursor-pointer">
                        <div class="mt-0.5 shrink-0">
                            <Toggle
                                checked={settings.enabledWidgets.includes(widget.id)}
                                onchange={() => toggleWidget(widget.id)}
                            />
                        </div>
                        <div>
                            <p class="type-body text-[var(--text-1)] select-none">{widget.label}</p>
                            <p class="type-label text-[var(--text-2)] select-none">{widget.description}</p>
                        </div>
                    </label>

                    {#if widget.id === 'now-playing'}
                        <div class="ml-12 mt-1.5">
                            <div class="flex flex-row gap-2">
                                <Settings class="icon-sm" />
                                {#if spotifyConnected}
                                    <p class="type-label text-[var(--text-1)] border-r pr-2 border-r-[var(--text-3)]">
                                        Spotify connected
                                    </p>
                                    <button
                                        onclick={handleDisconnect}
                                        class="type-label text-[var(--text-2)] hover:text-[var(--text-1)] transition-colors"
                                    >
                                        Disconnect
                                    </button>
                                {:else}
                                    <p class="type-label text-[var(--text-1)] border-r pr-2 border-r-[var(--text-3)]">
                                        Spotify disconnected
                                    </p>
                                    <a
                                        href="/spotify/auth"
                                        class="inline-flex items-center gap-1 type-label text-[var(--text-2)] hover:text-[var(--text-1)] transition-colors"
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

    <div>
        <SubTitle
            subTitleText="Widget Layout"
            subTitleDescription="Drag widgets between columns to reorder them on the schedule page. Drag the middle slider to set what you
                want the column width to be."
        />
        <WidgetOrderEditor />
    </div>
</div>
