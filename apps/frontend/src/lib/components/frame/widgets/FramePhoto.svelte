<script lang="ts">
    import { framePhotoStore } from '#lib/stores/FramePhotoStore.svelte.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `size`: a template slot's size class — the photo fills the slot (its body is a size
    // container). Omitted (the frame's columns): a fixed-height band across the column.
    let { size }: { size?: FrameSize } = $props();
    // Empty states stay body-sized in the columns and S slots, like the other widgets'.
    const small = $derived(size === undefined || size === 's');

    const photo = $derived(framePhotoStore.photo);
    // Unsplash's API guidelines require crediting the photographer wherever the photo is
    // shown; your own photo shows its caption, if you gave it one.
    const credit = $derived(
        photo?.source === 'unsplash' && photo.photographer_name
            ? `Photo: ${photo.photographer_name} / Unsplash`
            : photo?.source === 'local'
              ? photo.description
              : null
    );
    // A fixed 6-line band in the columns (its budget in lib/frame.ts, with the credit).
    const PHOTO_LINES = 6;
</script>

<!--
@component
The e-paper frame's Photo widget: Daily's photo of the day, drawn from Ambient's photo
choice and held by the Display service for the household's day, so it stays put between
Daily's 15-minute re-renders and matches the panel. Ordered-dithered with the rest of Daily.
-->
{#if framePhotoStore.error}
    <FrameEmpty text="Photos unavailable" {small} />
{:else if !photo}
    <FrameEmpty text="No photo to show" {small} />
{:else}
    <figure class="type-body flex min-h-0 flex-col {size ? 'h-full' : ''}">
        <!-- 3px frame around the photo, so pale ones keep an edge on the paper. -->
        <img
            src={photo.url}
            alt={photo.description ?? ''}
            class="w-full border-3 border-(--border) object-cover {size ? 'min-h-0 flex-1' : ''}"
            style={size ? undefined : `height: ${PHOTO_LINES}lh`}
        />
        {#if credit}
            <figcaption class="type-caption mt-2 shrink-0 truncate text-(--text-2)">{credit}</figcaption>
        {/if}
    </figure>
{/if}
