<script lang="ts">
    import { onMount } from 'svelte';
    import { Images, ImageUp, Upload } from '@lucide/svelte';
    import SubTitle from '../SubTitle.svelte';
    import PhotoCollectionModal from '../modals/PhotoCollectionModal.svelte';
    import { api, type UploadedPhoto } from '$lib/api';

    let photos = $state<UploadedPhoto[]>([]);
    let showModal = $state(false);
    let uploading = $state(false);
    let uploadLabel = $state('Uploading…');
    let error = $state<string | null>(null);

    onMount(async () => {
        photos = await api.photos.list();
    });

    async function handleFileChange(e: Event) {
        const input = e.target as HTMLInputElement;
        const all = Array.from(input.files ?? []);
        if (all.length === 0) return;

        const allowed = ['image/jpeg', 'image/png', 'image/webp'];
        const valid: File[] = [];
        const clientErrors: string[] = [];
        for (const f of all) {
            if (!allowed.includes(f.type)) clientErrors.push(`${f.name}: not a JPEG, PNG, or WebP`);
            else if (f.size > 25 * 1024 * 1024) clientErrors.push(`${f.name}: exceeds 25 MB`);
            else valid.push(f);
        }

        input.value = '';

        if (valid.length === 0) {
            error = clientErrors[0];
            return;
        }

        const totalMB = valid.reduce((sum, f) => sum + f.size, 0) / (1024 * 1024);
        if (totalMB > 190) {
            error = `Selected files total ${Math.round(totalMB)} MB — try uploading fewer at a time (200 MB max per batch).`;
            return;
        }

        error = null;
        uploadLabel = valid.length > 1 ? `Uploading ${valid.length} photos…` : 'Uploading…';
        uploading = true;
        const results = await api.photos.upload(valid);
        uploading = false;

        const serverErrors: string[] = [];
        for (const r of results) {
            if (r.status === 'ok' && r.photo) photos = [...photos, r.photo];
            else if (r.status === 'error') serverErrors.push(r.error ?? r.file_name);
        }

        const allErrors = [...clientErrors, ...serverErrors];
        if (allErrors.length === 1) error = allErrors[0];
        else if (allErrors.length > 1) error = `${allErrors.length} files had issues.`;
    }
</script>

<div class="space-y-3">
    <SubTitle
        subTitleText="My photos"
        subTitleDescription="Upload photos from your device. JPEG, PNG, and WebP accepted, up to 25 MB each."
    />

    <p class="type-label text-[var(--text-2)]">
        {photos.length === 0
            ? 'No photos in your collection.'
            : `${photos.length} photo${photos.length === 1 ? '' : 's'} in your collection`}
    </p>

    {#if error}
        <p class="type-label text-red-500">{error}</p>
    {/if}

    <div class="flex flex-wrap gap-2">
        <label
            class="flex cursor-pointer items-center gap-2 rounded-full border border-[var(--border)] px-4 py-1.5 transition-colors hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)] {uploading
                ? 'pointer-events-none opacity-50'
                : ''}"
        >
            <ImageUp class="icon-sm text-[var(--text-1)]" />
            <span class="type-label text-[var(--text-1)]">{uploading ? uploadLabel : 'Add photo'}</span>
            <input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                multiple
                class="sr-only"
                onchange={handleFileChange}
                disabled={uploading}
            />
        </label>

        {#if photos.length > 0}
            <button
                onclick={() => (showModal = true)}
                class="type-label flex items-center gap-2 rounded-full border border-[var(--border)] px-4 py-1.5 text-[var(--text-1)] transition-colors hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)]"
            >
                <Images class="icon-sm text-[var(--text-1)]" />
                <span>Manage collection</span>
            </button>
        {/if}
    </div>
</div>

<PhotoCollectionModal
    bind:open={showModal}
    {photos}
    onUploaded={(p) => (photos = [...photos, p])}
    onDeleted={(id) => (photos = photos.filter((p) => p.id !== id))}
/>
