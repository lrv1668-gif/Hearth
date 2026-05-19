<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade } from 'svelte/transition';
    import { goto } from '$app/navigation';
    import { api, type Photo, type UploadedPhoto } from '$lib/api';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { MediaQuery } from 'svelte/reactivity';

    let photo = $state<Photo | null>(null);
    let loading = $state(true);
    let interval: ReturnType<typeof setInterval>;

    // Shuffle-and-cycle state for local photos
    let localQueue: UploadedPhoto[] = [];
    let localIndex = 0;

    function shuffle<T>(arr: T[]): T[] {
        const a = [...arr];
        for (let i = a.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [a[i], a[j]] = [a[j], a[i]];
        }
        return a;
    }

    function buildQuery(): string {
        const categories = settings.photoCategories;
        return categories.length > 0 ? categories.join(',') : 'nature';
    }

    function nextLocalPhoto(): Photo | null {
        if (localQueue.length === 0) return null;
        if (localIndex >= localQueue.length) {
            localQueue = shuffle(localQueue);
            localIndex = 0;
        }
        const up = localQueue[localIndex++];
        return {
            id: up.id,
            url: up.url,
            thumb_url: up.thumb_url,
            description: null,
            photographer_name: null,
            unsplash_link: null,
            source: 'local',
        };
    }

    async function advance() {
        if (settings.photoSource === 'local') {
            const next = nextLocalPhoto();
            if (next) photo = next;
        } else {
            const isPortrait = new MediaQuery('orientation: portrait');
            const next = await api.photos.random(
                buildQuery(),
                isPortrait.current ? 'portrait' : 'landscape',
                settings.photoSource
            );
            if (next) photo = next;
        }
    }

    function exit() {
        goto('/');
    }

    onMount(async () => {
        if (settings.photoSource === 'local') {
            const uploads = await api.photos.list();
            if (uploads.length === 0) {
                goto('/settings');
                return;
            }
            localQueue = shuffle(uploads);
            localIndex = 0;
        }

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

        <!-- Attribution bar — Unsplash only -->
        {#if settings.showAttribution && photo.source === 'unsplash'}
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
