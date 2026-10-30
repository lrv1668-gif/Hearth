<script lang="ts">
    import type { Photo } from '#lib/api.ts';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import {
        getFrameScale,
        isAmbientPosition,
        onBlockInk,
        pickBlockInk,
        plateRect,
        updatedTime,
        weatherSentence,
        type Pixels,
    } from '#lib/frame.ts';
    import { FRAME_INKS } from '#lib/constants/frameThemes.ts';
    import type { PhotoCard } from '#lib/constants/frameModes.ts';
    import { localDateKey } from '#lib/utils.ts';
    import FrameDateBlock from './FrameDateBlock.svelte';
    import FrameDatePoster from './FrameDatePoster.svelte';

    let {
        now,
        photo,
        pixels = null,
        card = 'full',
    }: { now: Date; photo: Photo | null; pixels?: Pixels | null; card?: PhotoCard } = $props();

    // Photo mode can drop the card, or keep only an upload's caption.
    const caption = $derived(photo?.source === 'local' ? photo.description : null);

    // When the frame was drawn: the panel holds a render for up to the photo interval, so
    // this is a timestamp ("Updated 3:15"), never a clock that would be wrong in between.
    const updated = $derived(updatedTime(now));
    const today = $derived(weatherStore.forecast.find((f) => f.date === localDateKey(now)) ?? null);

    // Theme tokens that are a pure ink the date block may switch to.
    const INK_TOKENS = ['--block', '--accent', '--text-1', '--text-2', '--done', '--border'];

    const scale = getFrameScale();
    let plate = $state<HTMLElement>();
    let plateW = $state(0);
    let plateH = $state(0);
    // The frame root's position and theme. Settings' preview edits these attributes
    // in place, so watch them rather than reading them once.
    let attrs = $state({ position: '', theme: '' });

    $effect(() => {
        const root = plate?.closest<HTMLElement>('.frame');
        if (!root) return;
        const read = () => (attrs = { position: root.dataset.position ?? '', theme: root.dataset.theme ?? '' });
        read();
        const observer = new MutationObserver(read);
        observer.observe(root, { attributes: true, attributeFilter: ['data-position', 'data-theme'] });
        return () => observer.disconnect();
    });

    // The date block's ink: the theme's --block unless the photo around the plate
    // is close to it, then whichever of the theme's other inks stands out most.
    const block = $derived.by(() => {
        const root = plate?.closest<HTMLElement>('.frame');
        const { position, theme } = attrs;
        if (!pixels || !root || !plateW || !isAmbientPosition(position) || !theme) return null;
        const style = getComputedStyle(root);
        const token = (name: string) => style.getPropertyValue(name).trim().toLowerCase();
        const preferred = token('--block');
        const bg = token('--bg');
        const inks: readonly string[] = FRAME_INKS;
        const candidates = [...new Set(INK_TOKENS.map(token))].filter((ink) => inks.includes(ink) && ink !== bg);
        const frame = { w: root.clientWidth, h: root.clientHeight };
        const ink = pickBlockInk(
            pixels,
            frame,
            plateRect(position, { w: plateW, h: plateH }, frame, scale()),
            candidates,
            preferred,
            scale()
        );
        return ink === preferred ? null : { ink, on: onBlockInk(ink) };
    });
</script>

<!--
@component
Ambient Mode for the e-paper frame: the photo fills the panel, with a small plate in
a corner or at the middle of the top or bottom edge (PRODUCT.md — "information is
peripheral"; where is the frame root's data-position, from Settings → Frame or
?position=): a compact date
block joined to a flat --bg panel with the temperature, one sentence of weather and
when the frame was drawn. Both halves are pure inks, not a gradient scrim or a tint: this mode is
error-diffusion dithered for the photo, and tints streak under that (see HARDWARE.md).
The date block takes the theme's --block ink unless the photo around the plate is
close to that colour; then it switches to whichever of the theme's other pure inks
stands out most there (pickBlockInk in lib/frame.ts).
In B&W mode the frame route's CSS greyscales the photo (PRODUCT.md: B&W is pure B&W).
Photo mode reuses this view with `card`: "caption" keeps just an upload's caption on
the flat --bg panel, "none" leaves the photo bare. With no photo at all it shows a date
poster (FrameDatePoster) instead of the plate.
-->
{#if photo}
    <img
        src={photo.url}
        alt={photo.description ?? ''}
        crossorigin="anonymous"
        class="absolute inset-0 h-full w-full object-cover"
    />
{:else}
    <!-- No photo to hang: a date poster rather than an error message, which would stay up for hours -->
    <FrameDatePoster {now} note="No photo to show" />
    <p class="type-caption absolute right-16 bottom-8 text-(--text-2)">Updated {updated}</p>
{/if}

{#if photo && card === 'caption' && caption}
    <div class="plate absolute bg-(--bg) px-8 pt-5 pb-4">
        <p class="type-label font-(family-name:--display-family) text-(--text-1) italic">{caption}</p>
    </div>
{/if}

<!-- No border or shadow — the solid inks alone separate the plate from the photo
     (shadows or translucency would dither into speckle on the panel). -->
{#if photo && card === 'full'}
    <div
        bind:this={plate}
        bind:clientWidth={plateW}
        bind:clientHeight={plateH}
        class="plate absolute flex items-stretch bg-(--bg)"
        style:--block={block?.ink}
        style:--on-block={block?.on}
    >
        <FrameDateBlock {now} compact />
        <div class="px-8 pt-5 pb-4">
            {#if weatherStore.current}
                <p class="type-title leading-none font-semibold">
                    {Math.round(weatherStore.current.temperature_f)}°
                </p>
                <p class="type-label mt-3 font-(family-name:--display-family) text-(--text-2) italic">
                    {weatherSentence(weatherStore.current, today)}
                </p>
            {/if}
            <!-- Unsplash's API guidelines require crediting the photographer wherever the photo is shown -->
            {#if photo?.source === 'unsplash' && photo.photographer_name}
                <p class="type-caption mt-1 text-(--text-2)">Photo: {photo.photographer_name} / Unsplash</p>
            {:else if photo?.source === 'local' && photo.description}
                <!-- Your own photo: its caption, if you gave it one -->
                <p class="type-caption mt-1 text-(--text-2)">{photo.description}</p>
            {/if}
            <p class="type-caption mt-1 text-(--text-2)">Updated {updated}</p>
        </div>
    </div>
{/if}

<style>
    /* Inset 64px (times the frame's scale) from the chosen edge(s), or centred along the top/bottom edge for the
       middle positions. Keyed off the frame root's data-position (not a
       prop) so the Settings preview can move the card in place without reloading. */
    .plate {
        --inset: calc(64px * var(--frame-scale, 1));
        bottom: var(--inset);
        left: var(--inset);
        /* Size to the content, the same at every position — a middle card would
           otherwise only get the half of the frame right of left: 50% and wrap taller. */
        width: max-content;
        max-width: calc(100% - 2 * var(--inset));
    }
    :global(.frame[data-position^='top']) .plate {
        top: var(--inset);
        bottom: auto;
    }
    :global(.frame[data-position$='right']) .plate {
        right: var(--inset);
        left: auto;
    }
    :global(.frame[data-position$='middle']) .plate {
        left: 50%;
        transform: translateX(-50%);
    }
</style>
