<script lang="ts">
    import { onMount, tick } from 'svelte';
    import { browser } from '$app/env';
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { MediaQuery } from 'svelte/reactivity';
    import { tinykeys } from 'tinykeys';
    import { ChevronLeft, ChevronRight, PanelLeftClose, PanelLeftOpen } from '@lucide/svelte';
    import IconButton from '#lib/components/ui/IconButton.svelte';
    import { settings } from '#lib/stores/SettingsStore.svelte.ts';
    import { loadFrameSettings, syncFrameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import { loadHousehold, syncHousehold } from '#lib/stores/HouseholdStore.svelte.ts';
    import { loadModes } from '#lib/stores/ModeStore.svelte.ts';
    import SettingsSubnav from '#lib/components/settings/SettingsSubnav.svelte';
    import DashboardSection from '#lib/components/settings/sections/DashboardSection.svelte';
    import DisplaySection from '#lib/components/settings/sections/DisplaySection.svelte';
    import HomeSection from '#lib/components/settings/sections/HomeSection.svelte';
    import PhotosSection from '#lib/components/settings/sections/PhotosSection.svelte';
    import ConnectionsSection from '#lib/components/settings/sections/ConnectionsSection.svelte';
    import FrameSection from '#lib/components/settings/sections/FrameSection.svelte';
    import DiagnosticsSection from '#lib/components/settings/sections/DiagnosticsSection.svelte';
    import {
        parseSettingsSection,
        settingsHref,
        settingsSection,
        settingsSections as sections,
        type SettingsSection,
    } from '#lib/constants/settingsSections.ts';
    import { formatKeys, ignoreWhileEditing, ignoreWhileTyping, isMacPlatform, SIDEBAR_KEYS } from '#lib/shortcuts.ts';

    // Frame settings are shared through the Display service: load them, then save
    // each change back.
    onMount(() => {
        let stopSync: (() => void) | undefined;
        let cancelled = false;
        loadModes();
        let stopHouseholdSync: (() => void) | undefined;
        loadFrameSettings().then(() => {
            if (!cancelled) stopSync = syncFrameSettings();
        });
        // The dashboard's feeds and stops are shared the same way.
        loadHousehold().then(() => {
            if (!cancelled) stopHouseholdSync = syncHousehold();
        });
        return () => {
            cancelled = true;
            stopSync?.();
            stopHouseholdSync?.();
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

    // The scroll area's height (the Frame section's pinned preview fits it), and whether
    // the Frame section's floating Preview button needs room under the last card.
    let mainHeight = $state(0);
    let floatingPreview = $state(false);

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
        goto(settingsHref(id), { replace: true, reset: false });
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
            {active === 'frame' && floatingPreview ? 'pb-24' : 'pb-6'}"
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

            {#if active === 'dashboard'}
                <DashboardSection />
            {:else if active === 'display'}
                <DisplaySection />
            {:else if active === 'home'}
                <HomeSection />
            {:else if active === 'photos'}
                <PhotosSection />
            {:else if active === 'connections'}
                <ConnectionsSection />
            {:else if active === 'frame'}
                <FrameSection {mainHeight} bind:floatingPreview />
            {:else if active === 'diagnostics'}
                <DiagnosticsSection />
            {/if}
        </main>
    {/if}
</div>
