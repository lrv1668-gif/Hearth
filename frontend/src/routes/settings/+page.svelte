<script lang="ts">
    import { onMount, tick } from 'svelte';
    import { browser } from '$app/environment';
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { MediaQuery } from 'svelte/reactivity';
    import { innerHeight } from 'svelte/reactivity/window';
    import { tinykeys } from 'tinykeys';
    import { ChevronLeft, ChevronRight, Eye, PanelLeftClose, PanelLeftOpen } from '@lucide/svelte';
    import Button from '$lib/components/ui/Button.svelte';
    import CardColumns from '$lib/components/ui/CardColumns.svelte';
    import IconButton from '$lib/components/ui/IconButton.svelte';
    import InlineMessage from '$lib/components/ui/InlineMessage.svelte';
    import Modal from '$lib/components/ui/Modal.svelte';
    import ToggleRow from '$lib/components/ui/ToggleRow.svelte';
    import ThemePicker from '$lib/components/settings/ThemePicker.svelte';
    import FontThemePicker from '$lib/components/settings/FontThemePicker.svelte';
    import SizeSlider from '$lib/components/ui/SizeSlider.svelte';
    import { fontSizeStore, setFontSize, MIN_FONT_SCALE, MAX_FONT_SCALE } from '$lib/stores/FontSizeStore.svelte.ts';
    import AmbientSourceSettings from '$lib/components/settings/AmbientSourceSettings.svelte';
    import AmbientCadenceSettings from '$lib/components/settings/AmbientCadenceSettings.svelte';
    import AmbientCategorySettings from '$lib/components/settings/AmbientCategorySettings.svelte';
    import LocalPhotosSettings from '$lib/components/settings/LocalPhotosSettings.svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { frameSettings, loadFrameSettings, syncFrameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
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
    import SportsTeamSettings from '$lib/components/settings/SportsTeamSettings.svelte';
    import FrameAppearanceSettings from '$lib/components/settings/FrameAppearanceSettings.svelte';
    import FrameModeSettings from '$lib/components/settings/FrameModeSettings.svelte';
    import FrameModeOptions from '$lib/components/settings/FrameModeOptions.svelte';
    import { loadModes } from '$lib/stores/ModeStore.svelte.ts';
    import HomeLocationSettings from '$lib/components/settings/HomeLocationSettings.svelte';
    import ServiceHealthSettings from '$lib/components/settings/ServiceHealthSettings.svelte';
    import LogsSettings from '$lib/components/settings/LogsSettings.svelte';
    import SettingsSubnav from '$lib/components/settings/SettingsSubnav.svelte';
    import {
        parseSettingsSection,
        settingsCardId,
        settingsHref,
        settingsSection,
        settingsSections as sections,
        type SettingsSection,
    } from '$lib/constants/settingsSections';
    import { formatKeys, ignoreWhileEditing, ignoreWhileTyping, isMacPlatform, SIDEBAR_KEYS } from '$lib/shortcuts';

    // Shared by the Dashboard and Frame sections' Widgets cards.
    const dashboardLabel = (id: string) => allWidgets.find((w) => w.id === id)?.label ?? id;
    const frameLabel = (id: string) => frameWidgets.find((w) => w.id === id)?.label ?? id;
    const frameLayout = $derived(normalizeFrameLayout(frameSettings.layout));

    // Frame settings are shared through the Display service: load them, then save
    // each change back.
    onMount(() => {
        let stopSync: (() => void) | undefined;
        let cancelled = false;
        loadModes();
        loadFrameSettings().then(() => {
            if (!cancelled) stopSync = syncFrameSettings();
        });
        return () => {
            cancelled = true;
            stopSync?.();
        };
    });

    // The section lives in the URL so a refresh keeps it and other pages can link to one.
    const active = $derived(parseSettingsSection(page.url.searchParams.get('section')));
    const section = $derived(settingsSection(active));
    // The scroll area, which the sidebar's sub-items read their cards from and scroll.
    let main = $state<HTMLElement>();
    // Phones open /settings on the section list and drill into one; wide screens show the
    // sidebar and the default section side by side.
    const picking = $derived(!page.url.searchParams.has('section'));
    // Matches Tailwind's `lg`, where the sidebar replaces the section list. Below it, the
    // list stands alone — the section body isn't mounted behind it.
    const isWide = new MediaQuery('(min-width: 1024px)');

    // Frame section: the preview sits beside the options once there's room for both (the
    // @4xl container query — 56rem), and in a sheet behind a floating button below that.
    // Width 0 means not measured yet: show neither, so a phone never mounts the inline one.
    let frameColumnWidth = $state(0);
    const measured = $derived(frameColumnWidth > 0);
    const previewInline = $derived(
        measured && frameColumnWidth >= 56 * parseFloat(getComputedStyle(document.documentElement).fontSize)
    );
    let previewOpen = $state(false);
    // The inline preview is pinned in view, so it must fit the scroll area's height: what's
    // left after its card's title, description and padding, and a gap above and below.
    // Those are read from where the preview starts, never from the preview's own size, so
    // its height can't feed back into the fit.
    let mainHeight = $state(0);
    let previewWrap = $state<HTMLElement>();
    const previewMaxHeight = $derived.by(() => {
        void frameColumnWidth; // the card's description rewraps as the column changes
        const card = previewWrap?.parentElement;
        if (!previewWrap || !card || !mainHeight) return 0;
        const style = getComputedStyle(card);
        const chrome =
            previewWrap.offsetTop -
            card.offsetTop +
            parseFloat(style.paddingBottom) +
            parseFloat(style.borderBottomWidth);
        return mainHeight - chrome - 3 * parseFloat(getComputedStyle(document.documentElement).fontSize);
    });
    // The full-size preview fills its dialog: 85vh, less the dialog's header and padding.
    const fullPreviewMaxHeight = $derived(
        (innerHeight.current ?? 0) * 0.85 - 5.5 * parseFloat(getComputedStyle(document.documentElement).fontSize)
    );

    // Wide screens can fold the sidebar away for more room; Mod+B (⌘B / Ctrl+B) brings it back.
    const mac = browser && isMacPlatform(navigator.platform);
    const sidebarHidden = $derived(settings.settingsSidebarHidden);
    const sidebarLabel = $derived(`${sidebarHidden ? 'Show' : 'Hide'} sidebar (${formatKeys(SIDEBAR_KEYS, mac)})`);
    let aside = $state<HTMLElement>();

    async function toggleSidebar() {
        // Hiding the sidebar under the focus would drop it on <body>: hand it to the toggle.
        const hadFocus = !!aside?.contains(document.activeElement);
        settings.settingsSidebarHidden = !settings.settingsSidebarHidden;
        if (hadFocus) {
            await tick();
            document.getElementById('settings-sidebar-toggle')?.focus();
        }
    }

    function goToSection(id: SettingsSection) {
        goto(settingsHref(id), { replaceState: true, noScroll: true, keepFocus: true });
    }

    function stepSection(by: 1 | -1) {
        const i = sections.findIndex((s) => s.id === active);
        goToSection(sections[(i + by + sections.length) % sections.length].id);
    }

    // Mod+B toggles the sidebar; 1–7 jump to a section and J/K step through them.
    $effect(() => {
        const stopMod = tinykeys(
            window,
            {
                '$mod+KeyB': (e) => {
                    if (!isWide.current) return;
                    e.preventDefault();
                    toggleSidebar();
                },
            },
            { ignore: ignoreWhileEditing }
        );
        const stopKeys = tinykeys(
            window,
            {
                // Shift is optional so AZERTY's shifted digits count too.
                [`[Shift]+(Digit[1-${sections.length}])`]: (e) => {
                    e.preventDefault();
                    goToSection(sections[Number(e.code.slice(-1)) - 1].id);
                },
                j: (e) => {
                    e.preventDefault();
                    stepSection(1);
                },
                k: (e) => {
                    e.preventDefault();
                    stepSection(-1);
                },
            },
            { ignore: ignoreWhileTyping }
        );
        return () => {
            stopMod();
            stopKeys();
        };
    });
</script>

<svelte:head>
    <title>Hearth — Settings</title>
</svelte:head>

<div
    class="flex min-h-0 flex-1 flex-col overflow-hidden lg:grid
    {sidebarHidden ? 'lg:grid-cols-1' : 'lg:grid-cols-[280px_1fr]'}"
>
    <!-- Phone: the section list, standing in for the sidebar -->
    {#if picking}
        <nav class="min-h-0 flex-1 overflow-y-auto px-4 pt-4 pb-6 sm:px-6 sm:pt-6 lg:hidden">
            <h1 class="type-title mb-4 font-semibold text-(--text-1)">Settings</h1>
            <ul class="divide-y divide-(--border) overflow-hidden rounded-xl border-2 border-(--border) bg-(--surface)">
                {#each sections as s}
                    <li>
                        <a
                            href={settingsHref(s.id)}
                            class="flex min-h-14 items-center gap-3 px-4 py-3 text-(--text-1) transition-colors active:bg-(--surface-hi)"
                        >
                            <s.icon class="icon-md shrink-0 text-(--text-2)" />
                            <div class="min-w-0 flex-1">
                                <p class="type-body leading-tight font-medium">{s.label}</p>
                                <p class="type-label leading-tight text-(--text-2)">{s.subtitle}</p>
                            </div>
                            <ChevronRight class="icon-sm shrink-0 text-(--text-3)" aria-hidden="true" />
                        </a>
                    </li>
                {/each}
            </ul>
        </nav>
    {/if}

    <!-- Desktop: sidebar, unless folded away -->
    {#if !sidebarHidden}
        <aside
            bind:this={aside}
            id="settings-sidebar"
            class="hidden flex-col gap-1 overflow-y-auto border-r-2 border-(--border) bg-(--bg) px-3 py-4 lg:flex"
        >
            {#each sections as s}
                {@const isActive = active === s.id}
                <a
                    href={settingsHref(s.id)}
                    data-sveltekit-replacestate
                    data-sveltekit-noscroll
                    data-sveltekit-keepfocus
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
                </a>
                {#if isActive && isWide.current}<SettingsSubnav container={main} />{/if}
            {/each}
        </aside>
    {/if}

    <!-- Content -->
    {#if !picking || isWide.current}
        <main
            bind:this={main}
            bind:clientHeight={mainHeight}
            class="min-h-0 flex-1 overflow-y-auto px-4 pt-4 sm:px-6 sm:pt-6 lg:px-8 lg:pb-8
            {active === 'frame' && measured && !previewInline ? 'pb-24' : 'pb-6'}"
        >
            <a
                href="/settings"
                class="type-label mb-3 -ml-1 inline-flex min-h-10 items-center gap-1 text-(--text-2) transition-colors hover:text-(--text-1) lg:hidden"
            >
                <ChevronLeft class="icon-sm" aria-hidden="true" />
                Settings
            </a>
            <div class="mb-2 flex items-center gap-2">
                <!-- Wide screens only: phones have no sidebar to fold -->
                {#if isWide.current}
                    <IconButton
                        id="settings-sidebar-toggle"
                        label={sidebarLabel}
                        title={sidebarLabel}
                        aria-controls="settings-sidebar"
                        aria-expanded={!sidebarHidden}
                        onclick={toggleSidebar}
                        class="-ml-2"
                    >
                        {#if sidebarHidden}
                            <PanelLeftOpen class="icon-md" aria-hidden="true" />
                        {:else}
                            <PanelLeftClose class="icon-md" aria-hidden="true" />
                        {/if}
                    </IconButton>
                {/if}
                <h1 class="type-title font-semibold text-(--text-1)">{section.heading}</h1>
            </div>
            <p class="type-body mb-6 text-(--text-2)">{section.description}</p>

            <!-- A titled card is listed under its section in the sidebar (SettingsSubnav) unless `listed` is false. -->
            {#snippet card(title: string, description: string, children: import('svelte').Snippet, listed = true)}
                {@const inNav = listed && !!title}
                <div
                    id={inNav ? settingsCardId(title) : undefined}
                    data-settings-card={inNav ? title : undefined}
                    tabindex="-1"
                    class="scroll-mt-6 rounded-xl border-2 border-(--border) bg-(--surface) p-4 outline-none sm:p-6"
                >
                    {#if title}<p class="type-subtitle mb-1 font-medium text-(--text-1)">{title}</p>{/if}
                    {#if description}<p class="type-body mb-4 text-(--text-2)">{description}</p>{/if}
                    {@render children()}
                </div>
            {/snippet}

            {#if active === 'dashboard'}
                {#snippet widgets()}
                    <WidgetLayoutEditor
                        dragType="dashboard-widget"
                        columns={settings.widgetColumns}
                        labelFor={dashboardLabel}
                        onchange={(c) =>
                            (settings.widgetColumns = {
                                left: c.left as AllWidgetId[],
                                right: c.right as AllWidgetId[],
                            })}
                        width={settings.leftColumnWidth}
                        onwidthchange={(w) => (settings.leftColumnWidth = w)}
                        header={fixedHeaderWidgets}
                        footer={fixedFooterWidgets}
                        widgets={toggleableWidgets}
                        ontoggle={(id) => settings.toggleWidget(id as WidgetId)}
                    />
                {/snippet}
                {#snippet feeds()}<FeedSettings />{/snippet}
                {#snippet trainStops()}<TrainStopSettings />{/snippet}
                {#snippet sportsTeams()}<SportsTeamSettings />{/snippet}
                <div class="flex flex-col gap-4">
                    {@render card(
                        'Widgets',
                        'Drag widgets between columns, hide the ones you don’t need, and adjust the column width. Phones show one column — the left first, then the right — with Now Playing above it; the rest of the footer shows on wide screens only.',
                        widgets
                    )}
                    <CardColumns>
                        {#snippet left()}
                            {@render card('News Feeds', 'Manage RSS feeds and how many articles to show.', feeds)}
                            {@render card(
                                'Sports',
                                'The teams the Sports widget and the frame’s Last Result & Next Game follow. Shared by every device.',
                                sportsTeams
                            )}
                        {/snippet}
                        {#snippet right()}
                            {@render card(
                                'Trains & Buses',
                                'Manage the transit stops you want to see departures for.',
                                trainStops
                            )}
                        {/snippet}
                    </CardColumns>
                </div>
            {:else if active === 'display'}
                {#snippet themeContent()}<ThemePicker />{/snippet}
                {#snippet kioskContent()}
                    <ToggleRow
                        checked={settings.kioskMode}
                        onchange={() => (settings.kioskMode = !settings.kioskMode)}
                        title="Kiosk Mode"
                    >
                        Hides the navigation bar and shows the time, date, and weather header. Best for always-on wall
                        displays. Can also be activated with <code class="font-mono">?kiosk=1</code> in the URL.
                    </ToggleRow>
                    {#if settings.kioskMode}
                        <div class="mt-4 border-t border-(--border) pt-4">
                            <Button variant="link" href="/">Go to Dashboard →</Button>
                            <p class="type-label mt-0.5 text-(--text-2)">
                                The navigation bar is hidden in kiosk mode — use this link to return to the dashboard.
                            </p>
                        </div>
                    {/if}
                {/snippet}
                {#snippet fontContent()}
                    <FontThemePicker />
                    <p class="type-body mt-6 mb-1 font-medium text-(--text-1)">Size</p>
                    <p class="type-label mb-4 text-(--text-2)">Scales all text and icons across the dashboard.</p>
                    <SizeSlider
                        value={fontSizeStore.scale}
                        min={MIN_FONT_SCALE}
                        max={MAX_FONT_SCALE}
                        oninput={setFontSize}
                    />
                {/snippet}
                <div class="flex flex-col gap-4">
                    {@render card(
                        'Theme',
                        'Each theme is a complete colour set. Pick one to preview — applies instantly.',
                        themeContent
                    )}
                    {@render card(
                        'Type',
                        'Each font theme sets typeface, weight, and size. Pick one to preview — applies instantly.',
                        fontContent
                    )}
                    {@render card('Kiosk mode', 'How Hearth presents in this browser.', kioskContent)}
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
                {#if frameSettings.error}
                    <InlineMessage tone="error" class="mb-4">
                        Can't reach the Display service — photo choices here won't be saved or reach the frame.
                    </InlineMessage>
                {/if}
                <CardColumns>
                    {#snippet left()}
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
                    {/snippet}
                    {#snippet right()}
                        {#if frameSettings.photoSource === 'local' || frameSettings.photoSource === 'both'}
                            {@render card('My photos', 'Upload and manage your local photo collection.', localPhotos)}
                        {/if}
                        {@render card(
                            'Browser slideshow',
                            'How often the /ambient slideshow in this browser changes photo — slower at night — and whether photos drift. The frame keeps its own schedule.',
                            cadence
                        )}
                    {/snippet}
                </CardColumns>
            {:else if active === 'connections'}
                {#snippet spotifyContent()}<SpotifyConnectionSettings />{/snippet}
                {#snippet calendarContent()}<GoogleCalendarConnectionSettings />{/snippet}
                <div class="flex flex-col gap-4">
                    {@render card(
                        'Music',
                        'Show what’s playing on the dashboard, and what you last played on the frame.',
                        spotifyContent
                    )}
                    {@render card('Calendar', 'Sync events to the calendar and upcoming views.', calendarContent)}
                </div>
            {:else if active === 'frame'}
                {#snippet frameAppearanceContent()}
                    {#if frameSettings.error}
                        <InlineMessage tone="error" class="mb-4">
                            Can't reach the Display service — changes here won't be saved or reach the frame.
                        </InlineMessage>
                    {/if}
                    <FrameAppearanceSettings />
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
                        widgets={frameWidgets}
                        ontoggle={(id) => frameSettings.toggleWidget(id as FrameWidgetId)}
                    />
                    <p class="type-caption mt-2 text-(--text-2)">
                        Lists shrink to fit what shares their column. Portrait keeps the two columns side by side, so
                        they are narrower.
                    </p>
                {/snippet}
                {#snippet frameModeContent()}<FrameModeSettings />{/snippet}
                {#snippet frameModeOptionsContent()}<FrameModeOptions />{/snippet}
                {#snippet framePreviewContent()}
                    <div bind:this={previewWrap}>
                        <FramePreview maxHeight={previewMaxHeight} onexpand={() => (previewOpen = true)} />
                    </div>
                {/snippet}
                <!--
                Wide (container ≥ 56rem): options on the left, the preview pinned beside them
                so it stays in view while scrolling. Narrow: one column, and the preview opens
                in a sheet from a floating button instead, so it's one tap away from any card
                rather than below all of them. The left column becomes `display: contents` on
                narrow screens so its cards keep their order.
            -->
                <div class="@container" bind:clientWidth={frameColumnWidth}>
                    <div
                        class="flex flex-col gap-4 @4xl:grid @4xl:grid-cols-[minmax(0,1fr)_minmax(320px,42%)] @4xl:items-start"
                    >
                        <div class="contents @4xl:flex @4xl:flex-col @4xl:gap-4">
                            <div class="order-1">
                                {@render card(
                                    'Mode',
                                    'What the frame shows now, and a schedule for the rest of the day.',
                                    frameModeContent
                                )}
                            </div>
                            <div class="order-1">
                                {@render card(
                                    'E-paper frame',
                                    'Colours, typeface, orientation, and panel size for the wall frame. Themes use only colours the panel can print cleanly.',
                                    frameAppearanceContent
                                )}
                            </div>
                            <div class="order-3">
                                {@render card(
                                    'Photo, Countdown & Note',
                                    'What Photo, Countdown and Note modes show.',
                                    frameModeOptionsContent
                                )}
                            </div>
                            <div class="order-3">
                                {@render card(
                                    'Widgets',
                                    'What Daily mode shows: drag widgets between columns, hide the ones you don’t need, and adjust the column width ratio.',
                                    frameLayoutContent
                                )}
                            </div>
                        </div>
                        {#if previewInline}
                            <div class="order-2 @4xl:sticky @4xl:top-0">
                                {@render card(
                                    'Preview',
                                    'The real frame page, scaled down. Updates as you change settings.',
                                    framePreviewContent,
                                    false
                                )}
                            </div>
                        {/if}
                    </div>
                </div>
                {#if measured && !previewInline}
                    <button
                        onclick={() => (previewOpen = true)}
                        class="type-label fixed right-4 bottom-[calc(5rem+env(safe-area-inset-bottom))] z-20 flex min-h-11 items-center gap-2 rounded-full bg-(--accent) px-4 font-semibold text-(--accent-fg) shadow-lg transition-colors hover:bg-(--accent-hi) lg:bottom-6"
                    >
                        <Eye class="icon-sm" aria-hidden="true" />
                        Preview
                    </button>
                {/if}
                <!-- The full-size preview: the card's expand button on wide screens, the floating button on narrow ones -->
                <Modal
                    bind:open={previewOpen}
                    title="Preview"
                    size={frameSettings.orientation === 'portrait' ? 'xl' : 'full'}
                >
                    <FramePreview maxHeight={fullPreviewMaxHeight} />
                </Modal>
            {:else if active === 'diagnostics'}
                {#snippet servicesContent()}<ServiceHealthSettings />{/snippet}
                {#snippet logsContent()}<LogsSettings />{/snippet}
                <div class="flex flex-col gap-4">
                    {@render card(
                        'Services',
                        'Each part of Hearth checks itself: its database, the keys it needs, and whether its outside source is answering. Checked every 30 seconds.',
                        servicesContent
                    )}
                    {@render card(
                        'Logs',
                        'Live logs from every container on the host, searchable, in Dozzle.',
                        logsContent
                    )}
                </div>
            {/if}
        </main>
    {/if}
</div>
