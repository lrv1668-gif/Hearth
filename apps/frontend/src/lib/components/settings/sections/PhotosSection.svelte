<script lang="ts">
    import CardColumns from '#lib/components/ui/CardColumns.svelte';
    import InlineMessage from '#lib/components/ui/InlineMessage.svelte';
    import SettingsCard from '../SettingsCard.svelte';
    import AmbientSourceSettings from '../AmbientSourceSettings.svelte';
    import AmbientCadenceSettings from '../AmbientCadenceSettings.svelte';
    import AmbientCategorySettings from '../AmbientCategorySettings.svelte';
    import LocalPhotosSettings from '../LocalPhotosSettings.svelte';
    import FramePhotoSettings from '../FramePhotoSettings.svelte';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
</script>

{#if frameSettings.error}
    <InlineMessage tone="error" class="mb-4">
        Can't reach the Display service — photo choices here won't be saved or reach the frame.
    </InlineMessage>
{/if}
<CardColumns>
    {#snippet left()}
        <SettingsCard
            title="Photo source"
            description="Curated Unsplash photos, your own uploads, or a mix of both — for Ambient on the frame and every device."
        >
            <AmbientSourceSettings />
        </SettingsCard>
        {#if frameSettings.photoSource === 'unsplash' || frameSettings.photoSource === 'both'}
            <SettingsCard title="Categories" description="Which Unsplash photos to draw from. Shared with the frame.">
                <AmbientCategorySettings />
            </SettingsCard>
        {/if}
        <!-- Always shown: Photo mode picks from uploads whatever Ambient's source is -->
        <SettingsCard title="My photos" description="Upload and manage your local photo collection.">
            <LocalPhotosSettings />
        </SettingsCard>
    {/snippet}
    {#snippet right()}
        <SettingsCard
            title="On the frame"
            description="How often the panel changes photo, where Ambient’s card sits, and what Photo mode shows."
        >
            <FramePhotoSettings />
        </SettingsCard>
        <SettingsCard
            title="Browser slideshow"
            description="How often the /ambient slideshow in this browser changes photo — slower at night — and whether photos drift. The frame keeps its own schedule."
        >
            <AmbientCadenceSettings />
        </SettingsCard>
    {/snippet}
</CardColumns>
