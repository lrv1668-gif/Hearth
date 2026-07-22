<script lang="ts">
    import { onMount } from 'svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { api } from '$lib/api';

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
        <button
            onclick={() => (settings.photoSource = src)}
            class="type-label rounded-full border px-4 py-1.5 tracking-wide transition-colors
                {settings.photoSource === src
                ? 'pointer-events-none border-(--text-1) bg-(--text-1) text-(--bg)'
                : 'border-(--border) text-(--text-1) hover:border-(--text-2) hover:bg-(--surface-hi)'}"
        >
            {sourceLabels[src] ?? src}
        </button>
    {/each}
</div>
