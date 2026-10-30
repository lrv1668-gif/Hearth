<script lang="ts">
    import { onMount } from 'svelte';
    import { Images } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '#lib/api.ts';
    import { photoCards, photoIntervals } from '#lib/constants/frameModes.ts';
    import { settingsCardId } from '#lib/constants/settingsSections.ts';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import PhotoModePicker from './PhotoModePicker.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import SettingsGroup from '../ui/SettingsGroup.svelte';

    let uploads = $state<UploadedPhoto[]>([]);
    let loaded = $state(false);

    onMount(async () => {
        uploads = await api.photos.list();
        loaded = true;
    });

    // Ids that were chosen but have since been deleted don't count
    const picked = $derived(uploads.filter((u) => frameSettings.photoIds.includes(u.id)));
    const chosen = $derived(picked.length);
    // The picker grows with every upload, so it opens in a sheet; the card shows a strip of the picks.
    const STRIP_MAX = 6;
    let picking = $state(false);
</script>

<!--
@component
The frame's photo choices: how long Ambient and Photo keep a photo on the panel, Photo
mode's uploads (one pins it, several make an album picked from at random) and its card.
Saved with the rest of the frame settings.
-->
<div class="flex flex-col gap-5">
    <SettingsGroup title="New photo every">
        <SegmentedControl
            options={photoIntervals.map((i) => ({ value: String(i.minutes), label: i.label }))}
            value={String(frameSettings.photoIntervalMinutes)}
            onchange={(v) => (frameSettings.photoIntervalMinutes = Number(v))}
            label="New photo every"
        />
        <p class="type-caption mt-2 text-(--text-2)">
            Ambient and Photo mode on the panel. Each change is a full e-paper refresh, about 30 seconds of flashing.
        </p>
    </SettingsGroup>

    <SettingsGroup title="Photo mode">
        <p class="type-caption mb-2 text-(--text-2)">
            {#if !loaded}
                Loading your photos…
            {:else if uploads.length === 0}
                No uploads yet — add some under <a href="#{settingsCardId('My photos')}" class="underline">My photos</a
                >.
            {:else if chosen === 0}
                Pick one photo to show it, several for an album, or a saved group. With none picked, Photo mode shows
                Ambient’s photos.
            {:else}
                {chosen === 1 ? 'Showing 1 photo.' : `An album of ${chosen} photos, one per refresh.`}
            {/if}
        </p>
        {#if uploads.length > 0}
            <div class="flex flex-wrap items-center gap-2">
                {#each picked.slice(0, STRIP_MAX) as u (u.id)}
                    <img src={u.thumb_url} alt={u.caption ?? ''} class="size-12 rounded-lg object-cover" />
                {/each}
                {#if chosen > STRIP_MAX}
                    <span class="type-label px-1 text-(--text-2) tabular-nums">+{chosen - STRIP_MAX}</span>
                {/if}
                <Button onclick={() => (picking = true)}>
                    <Images class="icon-sm" />
                    {chosen === 0 ? 'Choose photos' : 'Change'}
                </Button>
            </div>
            <PhotoModePicker bind:open={picking} {uploads} />
        {/if}
        <p class="type-label mt-4 mb-2 text-(--text-2)">Card</p>
        <SegmentedControl
            options={photoCards.map((c) => ({ value: c.value, label: c.label }))}
            value={frameSettings.photoCard}
            onchange={(v) => (frameSettings.photoCard = v)}
            label="Photo card"
        />
    </SettingsGroup>
</div>
