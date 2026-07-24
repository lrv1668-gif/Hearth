<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade } from 'svelte/transition';
    import { goto } from '$app/navigation';
    import { api, type Photo, type UploadedPhoto } from '$lib/api';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';
    import { MediaQuery } from 'svelte/reactivity';

    const isPortrait = new MediaQuery('orientation: portrait');

    // Ken Burns keyframes are declared -global- in the style block so they can be
    // referenced from an inline style (Svelte would otherwise rename them).
    const kenBurnsAnimations = ['kb-zoom-in', 'kb-zoom-out', 'kb-pan-left', 'kb-pan-right'];

    let photo = $state<Photo | null>(null);
    let loading = $state(true);
    let motionStyle = $state('');
    let timer: ReturnType<typeof setTimeout>;

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
            description: up.caption,
            photographer_name: null,
            unsplash_link: null,
            source: 'local',
        };
    }

    function isNight(): boolean {
        const hour = new Date().getHours();
        return hour >= 22 || hour < 6;
    }

    function cadenceMs(): number {
        const night = settings.nightCadenceSeconds;
        const seconds = isNight() && night !== null ? night : settings.cadenceSeconds;
        return seconds * 1000;
    }

    function preload(url: string): Promise<void> {
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = () => resolve();
            img.onerror = () => reject(new Error(`failed to load ${url}`));
            img.src = url;
        });
    }

    async function fetchNext(): Promise<Photo | null> {
        let source = settings.photoSource;
        if (source === 'both') {
            source = localQueue.length > 0 && Math.random() < 0.5 ? 'local' : 'unsplash';
        }
        if (source === 'local') return nextLocalPhoto();
        return api.photos.random(buildQuery(), isPortrait.current ? 'portrait' : 'landscape', source);
    }

    async function advance() {
        const next = await fetchNext();
        if (!next || next.id === photo?.id) return;
        try {
            // Fully decode off-screen first so the crossfade never reveals a half-loaded image
            await preload(next.url);
            const animation = kenBurnsAnimations[Math.floor(Math.random() * kenBurnsAnimations.length)];
            const duration = cadenceMs() / 1000 + 3;
            motionStyle = `animation: ${animation} ${duration}s linear forwards;`;
            photo = next;
        } catch {
            // Keep the current photo; the next cycle will try again
        }
    }

    function scheduleNext() {
        timer = setTimeout(async () => {
            await advance();
            scheduleNext();
        }, cadenceMs());
    }

    function exit() {
        goto('/');
    }

    onMount(async () => {
        if (settings.photoSource === 'local' || settings.photoSource === 'both') {
            const uploads = await api.photos.list();
            if (uploads.length === 0 && settings.photoSource === 'local') {
                goto('/settings');
                return;
            }
            localQueue = shuffle(uploads);
            localIndex = 0;
        }

        await advance();
        loading = false;
        scheduleNext();
    });

    onDestroy(() => clearTimeout(timer));
</script>

<svelte:window onkeydown={exit} />

<!-- Fullscreen overlay — covers Nav (z-50) -->
<div
    onclick={exit}
    onkeydown={exit}
    role="button"
    tabindex="0"
    aria-label="Exit ambient mode"
    class="fixed inset-0 z-[100] flex cursor-pointer items-center justify-center overflow-hidden bg-black"
>
    {#if loading}
        <p class="type-body tracking-widest text-white/40 uppercase">Loading…</p>
    {:else if photo}
        {#key photo.id}
            <img
                src={photo.url}
                alt={photo.description ?? ''}
                style={settings.ambientMotion ? motionStyle : ''}
                class="absolute inset-0 h-full w-full object-cover will-change-transform"
                in:fade={{ duration: 1500 }}
                out:fade={{ duration: 1500 }}
            />
        {/key}

        {@const showAttribution = photo.source === 'unsplash' && settings.showAttribution}
        {@const showCaption = photo.source === 'local' && photo.description}
        {#if showAttribution || showCaption}
            <div
                class="absolute right-0 bottom-0 left-0 flex items-end justify-between gap-6 bg-gradient-to-t from-black/60 to-transparent px-6 py-4"
            >
                {#if showAttribution}
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
                {:else}
                    <p class="type-label text-white/70">{photo.description}</p>
                {/if}
                <p class="type-label shrink-0 text-white/40">Click or press any key to exit</p>
            </div>
        {/if}
    {:else}
        <p class="type-body tracking-widest text-white/40 uppercase">No photos available</p>
    {/if}
</div>

<style>
    @keyframes -global-kb-zoom-in {
        from {
            transform: scale(1);
        }
        to {
            transform: scale(1.08);
        }
    }
    @keyframes -global-kb-zoom-out {
        from {
            transform: scale(1.08);
        }
        to {
            transform: scale(1);
        }
    }
    @keyframes -global-kb-pan-left {
        from {
            transform: scale(1.06) translateX(1.5%);
        }
        to {
            transform: scale(1.06) translateX(-1.5%);
        }
    }
    @keyframes -global-kb-pan-right {
        from {
            transform: scale(1.06) translateX(-1.5%);
        }
        to {
            transform: scale(1.06) translateX(1.5%);
        }
    }

    @media (prefers-reduced-motion: reduce) {
        img {
            animation: none !important;
        }
    }
</style>
