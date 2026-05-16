<script lang="ts">
    import { onMount } from 'svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import Toggle from '$lib/components/Toggle.svelte';
    import SubTitle from '../SubTitle.svelte';
    import LocalPhotosSettings from './LocalPhotosSettings.svelte';
    import { cadenceOptions, categoryOptions } from '$lib/constants/ambient';
    import { api } from '$lib/api';

    const sourceLabels: Record<string, string> = {
        unsplash: 'Unsplash',
        local: 'My photos',
    };

    let availableSources = $state<string[]>(['unsplash']);

    onMount(async () => {
        availableSources = await api.photos.sources();
    });
</script>

<div class="space-y-6">
    <!-- Source selector -->
    <div class="space-y-3">
        <SubTitle
            subTitleText="Photo source"
            subTitleDescription="Choose between curated Unsplash photos or your own uploaded photos."
        />
        <div class="flex flex-wrap gap-2">
            {#each availableSources as src}
                <button
                    onclick={() => (settings.photoSource = src)}
                    class="rounded-full border px-4 py-1.5 text-xs tracking-wide transition-colors
                        {settings.photoSource === src
                        ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                        : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)] hover:text-[var(--text-1)]'} type-label"
                >
                    {sourceLabels[src] ?? src}
                </button>
            {/each}
        </div>
    </div>

    <!-- Cadence -->
    <div class="space-y-3">
        <SubTitle subTitleText="Photo cadence" subTitleDescription="Choose how often photos cycle." />
        <div class="flex flex-wrap gap-2">
            {#each cadenceOptions as opt}
                <button
                    onclick={() => (settings.cadenceSeconds = opt.value)}
                    class="rounded-full border px-4 py-1.5 text-xs tracking-wide transition-colors
                        {settings.cadenceSeconds === opt.value
                        ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                        : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)] hover:text-[var(--text-1)]'} type-label"
                >
                    {opt.label}
                </button>
            {/each}
        </div>
    </div>

    <!-- Categories — dimmed when not using Unsplash -->
    <div class="space-y-3 transition-opacity {settings.photoSource !== 'unsplash' ? 'opacity-40' : ''}">
        <SubTitle
            subTitleText="Photo categories"
            subTitleDescription={settings.photoSource === 'unsplash'
                ? 'Select one or more categories to filter Unsplash photos. If none are selected, no filtering is applied.'
                : 'Categories apply only to Unsplash photos.'}
        />
        <div class="flex flex-col gap-2" class:pointer-events-none={settings.photoSource !== 'unsplash'}>
            {#each categoryOptions as cat}
                <label class="flex cursor-pointer items-center gap-3">
                    <Toggle
                        checked={settings.photoCategories.includes(cat.id)}
                        onchange={() => settings.toggleCategory(cat.id)}
                    />
                    <span class="type-body text-[var(--text-1)]">{cat.label}</span>
                </label>
            {/each}
        </div>
    </div>

    <!-- Attribution — Unsplash only -->
    {#if settings.photoSource === 'unsplash'}
        <div class="space-y-3">
            <SubTitle
                subTitleText="Photographer info"
                subTitleDescription="Control whether photographer info shows at the bottom of the Ambient page."
            />
            <label class="flex cursor-pointer items-center gap-3">
                <Toggle
                    checked={settings.showAttribution}
                    onchange={() => (settings.showAttribution = !settings.showAttribution)}
                />
                <span class="type-body text-[var(--text-1)]">Show photographer name in ambient mode</span>
            </label>
        </div>
    {/if}

    <!-- Local photo management -->
    {#if settings.photoSource === 'local'}
        <LocalPhotosSettings />
    {/if}
</div>
