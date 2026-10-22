<script lang="ts">
    import { onMount } from 'svelte';
    import { Monitor, MapPin, LayoutList, Image, Plug, Tv } from '@lucide/svelte';
    import Toggle from '$lib/components/Toggle.svelte';
    import ThemePicker from '$lib/components/settings/ThemePicker.svelte';
    import FontThemePicker from '$lib/components/settings/FontThemePicker.svelte';
    import FontSizeSlider from '$lib/components/settings/FontSizeSlider.svelte';
    import AmbientSourceSettings from '$lib/components/settings/AmbientSourceSettings.svelte';
    import AmbientCadenceSettings from '$lib/components/settings/AmbientCadenceSettings.svelte';
    import AmbientCategorySettings from '$lib/components/settings/AmbientCategorySettings.svelte';
    import LocalPhotosSettings from '$lib/components/settings/LocalPhotosSettings.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { frameSettings, loadFrameSettings, syncFrameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import WidgetVisibilityList from '$lib/components/settings/WidgetVisibilityList.svelte';
    import WidgetLayoutEditor from '$lib/components/settings/WidgetLayoutEditor.svelte';
    import FramePreview from '$lib/components/settings/FramePreview.svelte';
    import {
        allWidgets,
        fixedFooterWidgets,
        fixedHeaderWidgets,
        toggleableWidgets,
        type AllWidgetId,
        type WidgetId,
    } from '$lib/constants/widgets';
    import {
        fixedFrameFooter,
        fixedFrameHeader,
        frameWidgets,
        MAX_COLUMN_WIDGETS,
        normalizeFrameLayout,
        type FrameWidgetId,
    } from '$lib/constants/frameWidgets';
    import SpotifyConnectionSettings from '$lib/components/settings/SpotifyConnectionSettings.svelte';
    import GoogleCalendarConnectionSettings from '$lib/components/settings/GoogleCalendarConnectionSettings.svelte';
    import FeedSettings from '$lib/components/settings/FeedSettings.svelte';
    import TrainStopSettings from '$lib/components/settings/TrainStopSettings.svelte';
    import FrameAppearanceSettings from '$lib/components/settings/FrameAppearanceSettings.svelte';
    import HomeLocationSettings from '$lib/components/settings/HomeLocationSettings.svelte';

    type SectionId = 'display' | 'home' | 'dashboard' | 'ambient' | 'connections' | 'frame';

    const sections = [
        { id: 'display' as SectionId, label: 'Display', subtitle: 'Theme · type', icon: Monitor },
        { id: 'home' as SectionId, label: 'Home', subtitle: 'Location · time zone', icon: MapPin },
        { id: 'dashboard' as SectionId, label: 'Dashboard', subtitle: 'Widgets · layout', icon: LayoutList },
        { id: 'ambient' as SectionId, label: 'Ambient', subtitle: 'Photos · slideshow', icon: Image },
        { id: 'connections' as SectionId, label: 'Connections', subtitle: 'Spotify · calendar', icon: Plug },
        { id: 'frame' as SectionId, label: 'Frame', subtitle: 'E-paper look · kiosk', icon: Tv },
    ] as const;

    const content: Record<SectionId, { heading: string; description: string }> = {
        display: {
            heading: 'How Hearth looks',
            description: 'The theme drives every surface, type colour, and accent across the dashboard and the frame.',
        },
        home: {
            heading: 'Home',
            description:
                'Where Hearth lives — for the weather, nearby birds, daylight, and the frame’s clock. Shared by every device.',
        },
        dashboard: {
            heading: 'Dashboard & Widgets',
            description: 'Manage which widgets appear and configure their layout on the dashboard page.',
        },
        ambient: {
            heading: 'Ambient Mode',
            description:
                'Choose the photos for Ambient mode — shared by the frame and every device — and how this browser’s slideshow plays them.',
        },
        connections: {
            heading: 'Connections',
            description: 'Connect the outside services Hearth reads from, like Spotify and Google Calendar.',
        },
        frame: {
            heading: 'Frame',
            description: 'Control how the e-paper frame looks. These settings are shared by every device.',
        },
    };

    // Shared by the Dashboard and Frame sections' Visibility and Layout cards.
    const dashboardLabel = (id: string) => allWidgets.find((w) => w.id === id)?.label ?? id;
    const frameLabel = (id: string) => frameWidgets.find((w) => w.id === id)?.label ?? id;
    const frameLayout = $derived(normalizeFrameLayout(frameSettings.layout));
    const frameUsed = (id: string) =>
        frameLayout.left.includes(id as FrameWidgetId) || frameLayout.right.includes(id as FrameWidgetId);
    const frameFull = $derived(Math.min(frameLayout.left.length, frameLayout.right.length) >= MAX_COLUMN_WIDGETS);

    // Frame settings are shared through the Display service: load them, then save
    // each change back.
    onMount(() => {
        let stopSync: (() => void) | undefined;
        let cancelled = false;
        loadFrameSettings().then(() => {
            if (!cancelled) stopSync = syncFrameSettings();
        });
        return () => {
            cancelled = true;
            stopSync?.();
        };
    });

    let active = $state<SectionId>('display');
    const section = $derived(content[active]);

    // Mobile tab bar: keep the active tab in view, and show the right-edge fade only while
    // more tabs are hidden off to the right.
    let tabBar = $state<HTMLElement>();
    let moreTabs = $state(false);
    function updateTabFade() {
        if (tabBar) moreTabs = tabBar.scrollLeft + tabBar.clientWidth < tabBar.scrollWidth - 1;
    }
    $effect(() => {
        tabBar?.querySelector(`[data-section="${active}"]`)?.scrollIntoView({ inline: 'nearest', block: 'nearest' });
        updateTabFade();
    });
</script>

<svelte:head>
    <title>Hearth — Settings</title>
</svelte:head>

<svelte:window onresize={updateTabFade} />

<!-- Mobile: section tabs sit right above the global nav (thumb reach); together they read as one tray -->
<div class="fixed right-0 bottom-[calc(4rem+env(safe-area-inset-bottom))] left-0 z-20 bg-(--bg) lg:hidden">
    {#if moreTabs}
        <div
            class="pointer-events-none absolute top-0 right-0 z-10 h-full w-10 bg-gradient-to-l from-(--bg) to-transparent"
        ></div>
    {/if}
    <nav
        bind:this={tabBar}
        onscroll={updateTabFade}
        class="flex [scrollbar-width:none] gap-1 overflow-x-auto border-t-2 border-(--border) px-4 py-1.5 [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden"
    >
        {#each sections as s}
            {@const isActive = active === s.id}
            <button
                onclick={() => (active = s.id)}
                data-section={s.id}
                aria-current={isActive ? 'page' : undefined}
                class="flex shrink-0 items-center gap-1.5 rounded-full px-3 py-1.5 transition-colors
                    {isActive ? 'bg-(--surface-hi) text-(--text-1)' : 'text-(--text-2) hover:text-(--text-1)'}"
            >
                <s.icon class="icon-sm shrink-0" />
                <span class="type-label whitespace-nowrap">{s.label}</span>
            </button>
        {/each}
    </nav>
</div>

<div class="flex min-h-0 flex-1 flex-col overflow-hidden lg:grid lg:grid-cols-[280px_1fr]">
    <!-- Desktop: sidebar -->
    <aside class="hidden flex-col gap-1 overflow-y-auto border-r-2 border-(--border) bg-(--bg) px-3 py-4 lg:flex">
        {#each sections as s}
            {@const isActive = active === s.id}
            <button
                onclick={() => (active = s.id)}
                aria-current={isActive ? 'page' : undefined}
                class="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left transition-colors
                    {isActive
                    ? 'bg-(--surface-hi) text-(--text-1)'
                    : 'text-(--text-2) hover:bg-(--surface) hover:text-(--text-1)'}"
            >
                <s.icon class="icon-md shrink-0 {isActive ? 'text-(--accent)' : ''}" />
                <div class="min-w-0 flex-1">
                    <p class="type-body leading-tight font-medium">{s.label}</p>
                    <p class="type-label leading-tight text-(--text-2)">{s.subtitle}</p>
                </div>
            </button>
        {/each}
    </aside>

    <!-- Content -->
    <main class="min-h-0 flex-1 overflow-y-auto px-4 pt-4 pb-20 sm:px-6 sm:pt-6 lg:px-8 lg:pb-8">
        <h1 class="type-title mb-2 font-semibold text-(--text-1)">{section.heading}</h1>
        <p class="type-body mb-6 text-(--text-2)">{section.description}</p>

        {#snippet card(title: string, description: string, children: import('svelte').Snippet)}
            <div class="rounded-xl border-2 border-(--border) bg-(--surface) p-4 sm:p-6">
                {#if title}<p class="type-subtitle mb-1 font-medium text-(--text-1)">{title}</p>{/if}
                {#if description}<p class="type-body mb-4 text-(--text-2)">{description}</p>{/if}
                {@render children()}
            </div>
        {/snippet}

        {#if active === 'dashboard'}
            {#snippet visibility()}
                <WidgetVisibilityList
                    widgets={toggleableWidgets}
                    isOn={(id) =>
                        settings.widgetColumns.left.includes(id as AllWidgetId) ||
                        settings.widgetColumns.right.includes(id as AllWidgetId)}
                    ontoggle={(id) => settings.toggleWidget(id as WidgetId)}
                    fixed={[
                        ...fixedHeaderWidgets.map((w) => ({
                            label: w.label,
                            caption: 'Fixed in the dashboard header',
                        })),
                        ...fixedFooterWidgets.map((w) => ({
                            label: w.label,
                            caption: 'Fixed in the dashboard footer',
                        })),
                    ]}
                />
            {/snippet}
            {#snippet layout()}
                <WidgetLayoutEditor
                    dragType="dashboard-widget"
                    columns={settings.widgetColumns}
                    labelFor={dashboardLabel}
                    onchange={(c) =>
                        (settings.widgetColumns = { left: c.left as AllWidgetId[], right: c.right as AllWidgetId[] })}
                    width={settings.leftColumnWidth}
                    onwidthchange={(w) => (settings.leftColumnWidth = w)}
                    header={fixedHeaderWidgets}
                    footer={fixedFooterWidgets}
                />
            {/snippet}
            {#snippet feeds()}<FeedSettings />{/snippet}
            {#snippet trainStops()}<TrainStopSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {@render card('Visibility', 'Enable or disable widgets on the dashboard page.', visibility)}
                {@render card('Layout', 'Drag widgets between columns and adjust the column width ratio.', layout)}
                {@render card('News Feeds', 'Manage RSS feeds and how many articles to show.', feeds)}
                {@render card('Trains & Buses', 'Manage the transit stops you want to see departures for.', trainStops)}
            </div>
        {:else if active === 'display'}
            {#snippet themeContent()}<ThemePicker />{/snippet}
            {#snippet fontContent()}
                <FontThemePicker />
                <p class="type-body mt-6 mb-1 font-medium text-(--text-1)">Size</p>
                <p class="type-label mb-4 text-(--text-2)">Scales all text and icons across the dashboard.</p>
                <FontSizeSlider />
            {/snippet}
            <div class="flex flex-col gap-4">
                {@render card(
                    'Theme',
                    'Each theme is a complete colour set. Click to preview — applies instantly.',
                    themeContent
                )}
                {@render card(
                    'Type',
                    'Each font theme sets typeface, weight, and size. Click to preview — applies instantly.',
                    fontContent
                )}
            </div>
        {:else if active === 'home'}
            {#snippet homeContent()}<HomeLocationSettings />{/snippet}
            {@render card(
                'Location & time zone',
                'Use this device’s location, search for a city, or type coordinates.',
                homeContent
            )}
        {:else if active === 'ambient'}
            {#snippet source()}<AmbientSourceSettings />{/snippet}
            {#snippet cadence()}<AmbientCadenceSettings />{/snippet}
            {#snippet categories()}<AmbientCategorySettings />{/snippet}
            {#snippet localPhotos()}<LocalPhotosSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {#if frameSettings.error}
                    <p class="type-label text-(--accent)">
                        Can't reach the Display service — photo choices here won't be saved or reach the frame.
                    </p>
                {/if}
                {@render card(
                    'Photo source',
                    'Curated Unsplash photos, your own uploads, or a mix of both — for the frame and every device.',
                    source
                )}
                {#if frameSettings.photoSource === 'unsplash' || frameSettings.photoSource === 'both'}
                    {@render card(
                        'Categories',
                        'Which Unsplash photos to draw from. Shared with the frame.',
                        categories
                    )}
                {/if}
                {#if frameSettings.photoSource === 'local' || frameSettings.photoSource === 'both'}
                    {@render card('My photos', 'Upload and manage your local photo collection.', localPhotos)}
                {/if}
                {@render card(
                    'Browser slideshow',
                    'How often the /ambient slideshow in this browser changes photo — slower at night — and whether photos drift. The frame keeps its own schedule.',
                    cadence
                )}
            </div>
        {:else if active === 'connections'}
            {#snippet spotifyContent()}<SpotifyConnectionSettings />{/snippet}
            {#snippet calendarContent()}<GoogleCalendarConnectionSettings />{/snippet}
            <div class="flex flex-col gap-4">
                {@render card('Music', 'Show your currently playing track in the dashboard footer.', spotifyContent)}
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
                        <p class="type-body text-(--text-1) select-none">Kiosk Mode</p>
                        <p class="type-label text-(--text-2) select-none">
                            Hides the navigation bar and shows the time, date, and weather header. Best for always-on
                            wall displays. Can also be activated with <code class="font-mono">?kiosk=1</code> in the URL.
                        </p>
                    </div>
                </label>
                {#if settings.kioskMode}
                    <div class="mt-4 border-t border-(--border) pt-4">
                        <a href="/" class="type-label inline-flex items-center gap-1.5 text-(--accent) hover:underline">
                            Go to Dashboard →
                        </a>
                        <p class="type-label mt-0.5 text-(--text-2)">
                            The navigation bar is hidden in kiosk mode — use this link to return to the dashboard.
                        </p>
                    </div>
                {/if}
            {/snippet}
            {#snippet frameAppearanceContent()}
                {#if frameSettings.error}
                    <p class="type-label mb-4 text-(--accent)">
                        Can't reach the Display service — changes here won't be saved or reach the frame.
                    </p>
                {/if}
                <FrameAppearanceSettings />
            {/snippet}
            {#snippet frameVisibility()}
                <WidgetVisibilityList
                    widgets={frameWidgets}
                    isOn={frameUsed}
                    ontoggle={(id) => frameSettings.toggleWidget(id as FrameWidgetId)}
                    isBlocked={() => frameFull}
                    fixed={[
                        ...fixedFrameHeader.map((w) => ({ label: w.label, caption: 'Fixed at the top of the frame' })),
                        ...fixedFrameFooter.map((w) => ({
                            label: w.label,
                            caption: 'Fixed along the bottom of the frame',
                        })),
                    ]}
                />
            {/snippet}
            {#snippet frameLayoutContent()}
                <WidgetLayoutEditor
                    dragType="frame-widget"
                    columns={frameLayout}
                    labelFor={frameLabel}
                    onchange={(c) => (frameSettings.layout = normalizeFrameLayout(c))}
                    width={frameSettings.split}
                    onwidthchange={(w) => (frameSettings.split = w)}
                    header={fixedFrameHeader}
                    footer={fixedFrameFooter}
                    maxPerColumn={MAX_COLUMN_WIDGETS}
                />
                <p class="type-caption mt-2 text-(--text-2)">
                    Lists shrink to fit what shares their column. Portrait keeps the two columns side by side, so they
                    are narrower.
                </p>
            {/snippet}
            {#snippet framePreviewContent()}
                <FramePreview />
            {/snippet}
            <!--
                Wide (container ≥ 56rem): options on the left, the preview pinned beside them
                so it stays in view while scrolling. Narrow: one column, with the preview right
                after the look card — the left column becomes `display: contents` so its cards
                can be ordered around the preview.
            -->
            <div class="@container">
                <div
                    class="flex flex-col gap-4 @4xl:grid @4xl:grid-cols-[minmax(0,1fr)_minmax(320px,42%)] @4xl:items-start"
                >
                    <div class="contents @4xl:flex @4xl:flex-col @4xl:gap-4">
                        <div class="order-1">
                            {@render card(
                                'E-paper frame',
                                'Colours, typeface, orientation, and panel size for the wall frame. Themes use only colours the panel can print cleanly.',
                                frameAppearanceContent
                            )}
                        </div>
                        <div class="order-3">
                            {@render card(
                                'Visibility',
                                'Choose which widgets appear on the frame in Daily mode.',
                                frameVisibility
                            )}
                        </div>
                        <div class="order-4">
                            {@render card(
                                'Layout',
                                'Drag widgets between columns and adjust the column width ratio.',
                                frameLayoutContent
                            )}
                        </div>
                        <div class="order-5">
                            {@render card('Display mode', 'Control how Hearth presents on this device.', kioskContent)}
                        </div>
                    </div>
                    <div class="order-2 @4xl:sticky @4xl:top-0">
                        {@render card(
                            'Preview',
                            'The real frame page, scaled down. Updates as you change settings.',
                            framePreviewContent
                        )}
                    </div>
                </div>
            </div>
        {/if}
    </main>
</div>
