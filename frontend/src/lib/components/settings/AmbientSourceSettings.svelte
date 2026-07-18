<script lang="ts">
    import { onMount } from 'svelte';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { serviceBlocked, serviceHint } from '$lib/stores/HealthStore.svelte.ts';
    import { api } from '$lib/api';

    const sourceLabels: Record<string, string> = {
        unsplash: 'Unsplash',
        local: 'My photos',
    };

    let availableSources = $state<string[]>(['unsplash']);

    onMount(async () => {
        availableSources = await api.photos.sources();
    });

    // Block selecting Unsplash without a key, but never lock the user out of
    // the currently active source.
    const unsplashBlocked = $derived(serviceBlocked('photos') && settings.photoSource !== 'unsplash');
</script>

<div class="flex flex-wrap gap-2">
    {#each availableSources as src}
        {@const blocked = src === 'unsplash' && unsplashBlocked}
        <button
            onclick={() => (settings.photoSource = src)}
            disabled={blocked}
            class="type-label rounded-full border px-4 py-1.5 tracking-wide transition-colors
                {blocked ? 'cursor-not-allowed opacity-50' : ''}
                {settings.photoSource === src
                ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)]'}"
        >
            {sourceLabels[src] ?? src}
        </button>
    {/each}
</div>
{#if serviceHint('photos')}
    <p class="type-label mt-2 text-[var(--text-3)]">{serviceHint('photos')}</p>
{/if}
