<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade } from 'svelte/transition';
    import { goto } from '$app/navigation';
    import { fetchRandomPhoto, type Photo } from '$lib/api';
    import { settings } from '$lib/SettingsStore';

    let photo = $state<Photo | null>(null);
    let loading = $state(true);
    let interval: ReturnType<typeof setInterval>;

    function buildQuery(): string {
        const cats = $settings.photoCategories;
        return cats.length > 0 ? cats.join(',') : 'nature';
    }

    async function advance() {
        const next = await fetchRandomPhoto(buildQuery());
        if (next) photo = next;
    }

    function exit() {
        goto('/');
    }

    onMount(async () => {
        await advance();
        loading = false;
        interval = setInterval(advance, $settings.cadenceSeconds * 1000);
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
    class="fixed inset-0 z-[100] bg-black flex items-center justify-center cursor-pointer"
>
    {#if loading}
        <p class="text-white/40 type-body tracking-widest uppercase">Loading…</p>
    {:else if photo}
        {#key photo.id}
            <img
                src={photo.url}
                alt={photo.description ?? ''}
                class="absolute inset-0 w-full h-full object-cover"
                in:fade={{ duration: 1500 }}
                out:fade={{ duration: 1500 }}
            />
        {/key}

        <!-- Attribution bar -->
        {#if $settings.showAttribution}
            <div
                class="absolute bottom-0 left-0 right-0 px-6 py-4 bg-gradient-to-t from-black/60 to-transparent flex items-end justify-between"
            >
                <p class="text-white/70 type-label">
                    Photo by
                    <a
                        href={photo.unsplash_link}
                        target="_blank"
                        rel="noopener noreferrer"
                        onclick={(e) => e.stopPropagation()}
                        class="underline hover:text-white transition-colors"
                    >
                        {photo.photographer_name}
                    </a>
                    on Unsplash
                </p>
                <p class="text-white/40 type-label">Click or press any key to exit</p>
            </div>
        {/if}
    {:else}
        <p class="text-white/40 type-body tracking-widest uppercase">No photos available</p>
    {/if}
</div>
