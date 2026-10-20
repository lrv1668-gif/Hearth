<script lang="ts">
    import type { Photo } from '$lib/api';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';

    let { now, photo, palette }: { now: Date; photo: Photo | null; palette: 'color' | 'bw' } = $props();

    const time = $derived(now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }));
    const date = $derived(now.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }));
</script>

<!--
@component
Ambient Mode for the e-paper frame: the photo fills the panel, with a small solid
info card in the bottom-left corner (PRODUCT.md — "information is peripheral").
The card is a flat pure-ink (--bg) panel, not a gradient scrim or a tint: this mode is
error-diffusion dithered for the photo, and tints streak under that (see HARDWARE.md).
In the bw palette the photo is greyscaled so the whole frame is black-and-white (PRODUCT.md).
-->
{#if photo}
    <img
        src={photo.url}
        alt={photo.description ?? ''}
        class="absolute inset-0 h-full w-full object-cover {palette === 'bw' ? 'grayscale' : ''}"
    />
{:else}
    <div class="absolute inset-0 flex items-center justify-center">
        <p class="type-subtitle tracking-widest text-(--text-2) uppercase">No photo available</p>
    </div>
{/if}

<div class="absolute bottom-10 left-10 flex items-center gap-8 border-2 border-(--border) bg-(--bg) px-8 py-6">
    <div>
        <p class="type-title leading-none font-bold">{time}</p>
        <p class="type-label mt-2 text-(--text-2)">{date}</p>
    </div>
    {#if weatherStore.current}
        {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
        <div class="flex items-center gap-3 border-l-2 border-(--border) pl-8">
            <WeatherIcon class="icon-lg text-(--text-2)" />
            <span class="type-title leading-none font-semibold">{Math.round(weatherStore.current.temperature_f)}°</span>
        </div>
    {/if}
</div>
