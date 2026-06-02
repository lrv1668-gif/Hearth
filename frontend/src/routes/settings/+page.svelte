<script lang="ts">
    import { Monitor, LayoutList, Image, Plug, Tv } from '@lucide/svelte';
    import Toggle from '$lib/components/Toggle.svelte';
    import ThemePicker from '$lib/components/settings/ThemePicker.svelte';
    import AmbientSourceSettings from '$lib/components/settings/AmbientSourceSettings.svelte';
    import AmbientCadenceSettings from '$lib/components/settings/AmbientCadenceSettings.svelte';
    import AmbientCategorySettings from '$lib/components/settings/AmbientCategorySettings.svelte';
    import LocalPhotosSettings from '$lib/components/settings/LocalPhotosSettings.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import WidgetVisibilitySettings from '$lib/components/settings/WidgetVisibilitySettings.svelte';
    import WidgetLayoutSettings from '$lib/components/settings/WidgetLayoutSettings.svelte';
    import SpotifyConnectionSettings from '$lib/components/settings/SpotifyConnectionSettings.svelte';
    import GoogleCalendarConnectionSettings from '$lib/components/settings/GoogleCalendarConnectionSettings.svelte';
    import FeedSettings from '$lib/components/settings/FeedSettings.svelte';

    type SectionId = 'display' | 'schedule' | 'ambient' | 'connections' | 'frame' | 'about';

    const sections = [
        { id: 'display' as SectionId, label: 'Display', subtitle: 'Theme · color mode', icon: Monitor },
        { id: 'schedule' as SectionId, label: 'Schedule', subtitle: 'Widgets · layout', icon: LayoutList },
        { id: 'ambient' as SectionId, label: 'Ambient', subtitle: 'Photos · cadence & source', icon: Image },
        { id: 'connections' as SectionId, label: 'Connections', subtitle: 'Spotify · calendar', icon: Plug },
        { id: 'frame' as SectionId, label: 'Frame', subtitle: 'Kiosk · display mode', icon: Tv },
    ] as const;

    const content: Record<SectionId, { heading: string; description: string }> = {
        display: {
            heading: 'How Hearth looks',
            description: 'The theme drives every surface, type colour, and accent across the dashboard and the frame.',
        },
        schedule: {
            heading: 'Schedule & Widgets',
            description: 'Manage which widgets appear and configure their layout on the schedule page.',
        },
        ambient: {
            heading: 'Ambient Mode',
            description: 'Configure the photo source, cycling cadence, and category filters.',
        },
        connections: {
            heading: 'Connections',
            description: 'Connect and manage external services like Spotify and weather providers.',
        },
        frame: { heading: 'Frame', description: 'Control how Hearth presents itself on this device.' },
        about: { heading: 'About Hearth', description: 'Version information, acknowledgements, and credits.' },
    };

    let active = $state<SectionId>('display');
    const section = $derived(content[active]);
</script>

<svelte:head>
    <title>Hearth — Settings</title>
</svelte:head>

<!-- Mobile: fixed bottom tab bar (above the global nav) -->
<div class="fixed bottom-16 left-0 right-0 z-20 bg-[var(--bg)] lg:hidden">
    <!-- Right-edge fade hints at scrollable overflow -->
    <div
        class="pointer-events-none absolute right-0 top-0 z-10 h-full w-10 bg-gradient-to-l from-[var(--bg)] to-transparent"
    ></div>
    <nav
        class="flex justify-center gap-2 overflow-x-auto border-t-2 border-[var(--border)] px-2 py-2 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
    >
        {#each sections as s}
            {@const isActive = active === s.id}
            <button
                onclick={() => (active = s.id)}
                class="flex shrink-0 items-center gap-1.5 rounded-full px-3 py-1.5 transition-colors
                    {isActive
                    ? 'bg-[var(--surface-hi)] text-[var(--text-1)]'
                    : 'text-[var(--text-2)] hover:text-[var(--text-1)]'}"
            >
                <s.icon class="icon-sm shrink-0" />
                <span class="type-label whitespace-nowrap">{s.label}</span>
            </button>
        {/each}
    </nav>
</div>

<div class="flex min-h-0 flex-1 flex-col overflow-hidden lg:grid lg:grid-cols-[280px_1fr]">
    <!-- Desktop: sidebar -->
    <aside
        class="hidden flex-col gap-1 overflow-y-auto border-r-2 border-[var(--border)] bg-[var(--bg)] px-3 py-4 lg:flex"
    >
        {#each sections as s}
            {@const isActive = active === s.id}
            <button
                onclick={() => (active = s.id)}
                class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left transition-colors
                    {isActive
                    ? 'bg-[var(--surface-hi)] text-[var(--text-1)]'
                    : 'text-[var(--text-1)] hover:bg-[var(--surface)] hover:text-[var(--text-1)]'}"
            >
                <s.icon class="icon-md shrink-0 {isActive ? 'text-[var(--accent)]' : 'text-[var(--text-1)]'}" />
                <div class="min-w-0 flex-1">
                    <p class="type-body font-medium leading-tight">{s.label}</p>
                    <p class="type-label leading-tight text-[var(--text-2)]">{s.subtitle}</p>
                </div>
            </button>
        {/each}
    </aside>

    <!-- Content -->
    <main class="min-h-0 flex-1 overflow-y-auto px-6 py-6 pb-20 lg:px-10 lg:py-8 lg:pb-8">
        <h1 class="type-title mb-2 font-semibold text-[var(--text-1)]">{section.heading}</h1>
        <p class="type-body mb-6 text-[var(--text-2)]">{section.description}</p>

        {#snippet card(title: string, description: string, children: import('svelte').Snippet)}
            <div class="rounded-xl border-2 border-[var(--border)] bg-[var(--surface)] p-6">
                {#if title}<p class="type-subtitle mb-1 font-medium text-[var(--text-1)]">{title}</p>{/if}
                {#if description}<p class="type-body mb-4 text-[var(--text-2)]">{description}</p>{/if}
                {@render children()}
            </div>
        {/snippet}

        {#if active === 'schedule'}
            {#snippet visibility()}<WidgetVisibilitySettings />{/snippet}
            {#snippet layout()}<WidgetLayoutSettings />{/snippet}
            {#snippet feeds()}<FeedSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {@render card('Visibility', 'Enable or disable widgets on the schedule page.', visibility)}
                {@render card('Layout', 'Drag widgets between columns and adjust the column width ratio.', layout)}
                {@render card('News Feeds', 'Manage RSS feeds and how many articles to show.', feeds)}
            </div>
        {:else if active === 'display'}
            {#snippet themeContent()}
                <p class="type-body mb-4 font-medium text-[var(--text-1)]">Theme</p>
                <p class="type-label mb-4 text-[var(--text-2)]">
                    Each theme is a complete colour set. Click to preview — applies instantly.
                </p>
                <ThemePicker />
            {/snippet}
            {@render card('', '', themeContent)}
        {:else if active === 'ambient'}
            {#snippet source()}<AmbientSourceSettings />{/snippet}
            {#snippet cadence()}<AmbientCadenceSettings />{/snippet}
            {#snippet categories()}<AmbientCategorySettings />{/snippet}
            {#snippet localPhotos()}<LocalPhotosSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {@render card('Photo source', 'Choose between curated Unsplash photos or your own uploads.', source)}
                {@render card('Cadence', 'How often photos cycle in ambient mode.', cadence)}
                {#if settings.photoSource === 'unsplash'}
                    {@render card(
                        'Categories & attribution',
                        'Filter by category and control photographer attribution.',
                        categories
                    )}
                {:else if settings.photoSource === 'local'}
                    {@render card('My photos', 'Upload and manage your local photo collection.', localPhotos)}
                {/if}
            </div>
        {:else if active === 'connections'}
            {#snippet spotifyContent()}<SpotifyConnectionSettings />{/snippet}
            {#snippet calendarContent()}<GoogleCalendarConnectionSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {@render card('Music', 'Show your currently playing track in the Now Playing widget.', spotifyContent)}
                {@render card('Calendar', 'Sync events to the calendar and upcoming views.', calendarContent)}
            </div>
        {:else if active === 'frame'}
            {#snippet kioskContent()}
                <label class="flex cursor-pointer items-center gap-3">
                    <div class="mt-0.5 shrink-0">
                        <Toggle
                            checked={settings.kioskMode}
                            onchange={() => (settings.kioskMode = !settings.kioskMode)}
                        />
                    </div>
                    <div>
                        <p class="type-body select-none text-[var(--text-1)]">Kiosk Mode</p>
                        <p class="type-label select-none text-[var(--text-2)]">
                            Hides the navigation bar and shows the time, date, and weather header. Best for always-on
                            wall displays. Can also be activated with <code class="font-mono">?kiosk=1</code> in the URL.
                        </p>
                    </div>
                </label>
                {#if settings.kioskMode}
                    <div class="mt-4 border-t border-[var(--border)] pt-4">
                        <a
                            href="/"
                            class="type-label inline-flex items-center gap-1.5 text-[var(--accent)] hover:underline"
                        >
                            Go to Schedule →
                        </a>
                        <p class="type-label mt-0.5 text-[var(--text-3)]">
                            The navigation bar is hidden in kiosk mode — use this link to return to the dashboard.
                        </p>
                    </div>
                {/if}
            {/snippet}
            {@render card('Display mode', 'Control how Hearth presents on this device.', kioskContent)}
        {:else}
            {#snippet comingSoon()}<p class="type-body text-[var(--text-1)]">Coming soon.</p>{/snippet}
            {@render card('', '', comingSoon)}
        {/if}
    </main>
</div>
