<script lang="ts">
    import { onMount } from 'svelte';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { api } from '$lib/api';
    import ChoiceChips from '../ui/ChoiceChips.svelte';

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

<ChoiceChips
    label="Photo source"
    options={options.map((src) => ({ value: src, label: sourceLabels[src] ?? src }))}
    value={frameSettings.photoSource}
    onchange={(v) => (frameSettings.photoSource = v)}
/>
