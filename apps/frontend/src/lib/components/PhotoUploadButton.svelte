<script lang="ts">
    import { ImageUp } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '#lib/api.ts';
    import { checkPhotoFiles, PHOTO_UPLOAD_TYPES, summarizeUploadErrors } from '#lib/photos.ts';
    import FileUploadButton from './ui/FileUploadButton.svelte';

    interface Props {
        label?: string;
        onuploaded: (photo: UploadedPhoto) => void;
        /** The batch's problems as one message, or null once a batch goes through cleanly. */
        onerror: (message: string | null) => void;
    }

    let { label = 'Add photos', onuploaded, onerror }: Props = $props();

    let uploading = $state(false);
    let busyLabel = $state('Uploading…');

    async function upload(files: File[]) {
        const { valid, errors, batchError } = checkPhotoFiles(files);
        if (batchError) return onerror(batchError);
        if (valid.length === 0) return onerror(summarizeUploadErrors(errors));

        onerror(null);
        busyLabel = valid.length > 1 ? `Uploading ${valid.length} photos…` : 'Uploading…';
        uploading = true;
        let results;
        try {
            results = await api.photos.upload(valid);
        } catch {
            // A network failure rejects rather than returning per-file errors
            return onerror('Upload failed — check the connection and try again.');
        } finally {
            uploading = false;
        }

        for (const r of results) {
            if (r.status === 'ok' && r.photo) onuploaded(r.photo);
            else if (r.status === 'error') errors.push(r.error ?? r.file_name);
        }
        onerror(summarizeUploadErrors(errors));
    }
</script>

<!--
@component
Picks photos, checks them against what the Photos service accepts, and uploads them —
shared by the Ambient settings and the photo collection.
-->
<FileUploadButton
    label={uploading ? busyLabel : label}
    icon={ImageUp}
    accept={PHOTO_UPLOAD_TYPES.join(',')}
    multiple
    disabled={uploading}
    onfiles={upload}
/>
