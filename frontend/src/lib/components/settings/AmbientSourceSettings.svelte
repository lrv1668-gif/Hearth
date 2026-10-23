<script lang="ts">
    import { onMount } from 'svelte';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { api } from '$lib/api';
    import { Check } from '@lucide/svelte';

    const sourceLabels: Record<string, string> = {
        unsplash: 'Unsplash',
        local: 'My photos',
        both: 'Both',
    };

    let availableSources = $state<string[]>(['unsplash']);
    // "Both" mixes local and Unsplash, so it only makes sense when more than one source exists
    const options = $derived(availableSources.length > 1 ? [...availableSources, 'both'] : availableSources);

    onMount(async () => {
        availableSources = await api.photos.sources();
    });
</script>

<div class="flex flex-wrap gap-2">
    {#each options as src}
        {@const selected = frameSettings.photoSource === src}
        <!-- Accent fill plus a check, so the choice reads at a glance even mid-transition -->
        <button
            onclick={() => (frameSettings.photoSource = src)}
            aria-pressed={selected}
            class="type-label flex min-h-10 items-center gap-1.5 rounded-full border px-4 py-1.5 tracking-wide transition-colors
                {selected
                ? 'pointer-events-none border-(--accent) bg-(--accent) font-semibold text-(--accent-fg)'
                : 'border-(--border) text-(--text-2) hover:border-(--text-2) hover:text-(--text-1)'}"
        >
            {#if selected}<Check class="icon-sm" aria-hidden="true" />{/if}
            {sourceLabels[src] ?? src}
        </button>
    {/each}
</div>
