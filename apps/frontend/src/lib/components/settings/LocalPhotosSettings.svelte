<script lang="ts">
    import { onMount } from 'svelte';
    import { Images } from '@lucide/svelte';
    import PhotoCollectionModal from '../modals/PhotoCollectionModal.svelte';
    import PhotoUploadButton from '../PhotoUploadButton.svelte';
    import Button from '../ui/Button.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import { api, type UploadedPhoto } from '#lib/api.ts';

    let photos = $state<UploadedPhoto[]>([]);
    let showModal = $state(false);
    let error = $state<string | null>(null);

    onMount(async () => {
        photos = await api.photos.list();
    });
</script>

<div class="space-y-3">
    <p class="type-label text-(--text-2)">
        {photos.length === 0
            ? 'No photos in your collection.'
            : `${photos.length} photo${photos.length === 1 ? '' : 's'} in your collection`}
    </p>

    {#if error}
        <InlineMessage tone="error">{error}</InlineMessage>
    {/if}

    <div class="flex flex-wrap gap-2">
        <PhotoUploadButton
            label="Add photo"
            onuploaded={(p) => (photos = [...photos, p])}
            onerror={(m) => (error = m)}
        />

        {#if photos.length > 0}
            <Button onclick={() => (showModal = true)}>
                <Images class="icon-sm" />
                Manage collection
            </Button>
        {/if}
    </div>
</div>

<PhotoCollectionModal
    bind:open={showModal}
    {photos}
    onUploaded={(p) => (photos = [...photos, p])}
    onDeleted={(id) => (photos = photos.filter((p) => p.id !== id))}
    onCaptioned={(id, caption) => (photos = photos.map((p) => (p.id === id ? { ...p, caption } : p)))}
/>
