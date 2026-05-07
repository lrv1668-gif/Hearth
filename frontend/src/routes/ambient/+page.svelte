<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade } from 'svelte/transition';
    import { goto } from '$app/navigation';
    import { fetchRandomPhoto, type Photo } from '$lib/api';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { MediaQuery } from 'svelte/reactivity';

    let photo = $state<Photo | null>(null);
    let loading = $state(true);
    let interval: ReturnType<typeof setInterval>;

    function buildQuery(): string {
        const categories = settings.photoCategories;
        return categories.length > 0 ? categories.join(',') : 'nature';
    }

    async function advance() {
        const isPortrait = new MediaQuery('orientation: portrait');
        console.log(`isPortrait: ${isPortrait.current}`)
        const next = await fetchRandomPhoto(buildQuery(), isPortrait.current ? 'portrait' : 'landscape');
        if (next) photo = next;
    }

    function exit() {
        goto('/');
    }

    onMount(async () => {
        await advance();
        loading = false;
        interval = setInterval(advance, settings.cadenceSeconds * 1000);
    });

    onDestroy(() => clearInterval(interval));
</script>

<svelte:window onkeydown={exit} />

<!-- Fullscreen overlay — covers Nav (z-50) -->
<div
    onclick={exit}
    onkeydown={exit}
    role="button"
    tabindex="0"
    aria-label="Exit ambient mode"
    class="fixed inset-0 z-[100] flex cursor-pointer items-center justify-center bg-black"
>
    {#if loading}
        <p class="type-body uppercase tracking-widest text-white/40">Loading…</p>
    {:else if photo}
        {#key photo.id}
            <img
                src={photo.url}
                alt={photo.description ?? ''}
                class="absolute inset-0 h-full w-full object-cover"
                in:fade={{ duration: 1500 }}
                out:fade={{ duration: 1500 }}
            />
        {/key}

        <!-- Attribution bar -->
        {#if settings.showAttribution}
            <div
                class="absolute bottom-0 left-0 right-0 flex items-end justify-between bg-gradient-to-t from-black/60 to-transparent px-6 py-4"
            >
                <p class="type-label text-white/70">
                    Photo by
                    <a
                        href={photo.unsplash_link}
                        target="_blank"
                        rel="noopener noreferrer"
                        onclick={(e) => e.stopPropagation()}
                        class="underline transition-colors hover:text-white"
                    >
                        {photo.photographer_name}
                    </a>
                    on Unsplash
                </p>
                <p class="type-label text-white/40">Click or press any key to exit</p>
            </div>
        {/if}
    {:else}
        <p class="type-body uppercase tracking-widest text-white/40">No photos available</p>
    {/if}
</div>
