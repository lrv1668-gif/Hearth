<script lang="ts">
    import { Trash2, Pin } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '#lib/api.ts';
    import PhotoUploadButton from '../PhotoUploadButton.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import Modal from '../ui/Modal.svelte';
    import { clearOverride, loadModes, modeStore, pinPhoto } from '#lib/stores/ModeStore.svelte.ts';

    interface Props {
        open?: boolean;
        photos?: UploadedPhoto[];
        onUploaded?: (photo: UploadedPhoto) => void;
        onDeleted?: (id: string) => void;
        onCaptioned?: (id: string, caption: string | null) => void;
    }

    let { open = $bindable(false), photos = [], onUploaded, onDeleted, onCaptioned }: Props = $props();

    let error = $state<string | null>(null);

    $effect(() => {
        if (open) {
            error = null;
            loadModes();
        }
    });

    async function handleDelete(id: string) {
        const ok = await api.photos.delete(id);
        if (!ok) return;
        onDeleted?.(id);
        // A pin never outlives its photo: Display clears it only if this was the pinned one.
        await clearOverride(id);
    }

    /** "Show on frame": pins the photo until switched off; pinning another moves the pin. */
    function togglePin(id: string) {
        if (modeStore.pinnedPhotoId === id) clearOverride(id);
        else pinPhoto(id);
    }

    async function handleCaptionChange(id: string, e: Event) {
        const value = (e.target as HTMLInputElement).value.trim();
        const caption = value === '' ? null : value;
        const ok = await api.photos.setCaption(id, caption);
        if (ok) onCaptioned?.(id, caption);
    }
</script>

{#snippet errorNotice()}
    <InlineMessage tone="error">{error}</InlineMessage>
{/snippet}

<Modal
    bind:open
    title="Photo collection"
    subtitle={photos.length === 0 ? 'No photos yet' : `${photos.length} photo${photos.length === 1 ? '' : 's'}`}
    size="xl"
    divided
    notice={error ? errorNotice : undefined}
>
    {#snippet actions()}
        <PhotoUploadButton onuploaded={(p) => onUploaded?.(p)} onerror={(m) => (error = m)} />
    {/snippet}
    {#if photos.length === 0}
        <p class="type-label py-12 text-center text-(--text-2)">No photos yet. Upload some to get started.</p>
    {:else}
        <div class="grid grid-cols-3 gap-3">
            {#each photos as p (p.id)}
                {@const pinned = modeStore.pinnedPhotoId === p.id}
                <div class="flex flex-col gap-1">
                    <div class="group relative aspect-square overflow-hidden rounded-lg bg-(--surface)">
                        <img
                            src={p.thumb_url}
                            alt={p.caption ?? ''}
                            class="absolute inset-0 h-full w-full object-cover"
                        />
                        <button
                            onclick={() => handleDelete(p.id)}
                            class="absolute top-1.5 right-1.5 rounded-md bg-black/60 p-1.5 opacity-0 transition-opacity group-hover:opacity-100 focus:opacity-100 pointer-coarse:opacity-100"
                            aria-label="Delete photo"
                        >
                            <Trash2 class="h-3.5 w-3.5 text-white" />
                        </button>
                        <button
                            onclick={() => togglePin(p.id)}
                            aria-pressed={pinned}
                            title={pinned ? 'Showing on the frame — click to stop' : 'Show on frame'}
                            class="type-caption absolute bottom-1.5 left-1.5 flex items-center gap-1 rounded-md px-2 py-1 font-semibold transition-opacity
                                    {pinned
                                ? 'bg-(--accent) text-(--accent-fg)'
                                : 'bg-black/60 text-white opacity-0 group-hover:opacity-100 focus:opacity-100 pointer-coarse:opacity-100'}"
                        >
                            <Pin class="h-3.5 w-3.5" />
                            {pinned ? 'On frame' : 'Show on frame'}
                        </button>
                    </div>
                    <input
                        type="text"
                        value={p.caption ?? ''}
                        onchange={(e) => handleCaptionChange(p.id, e)}
                        placeholder="Add a caption"
                        aria-label="Photo caption"
                        class="type-label w-full rounded-md border border-transparent bg-transparent px-1.5 py-1 text-(--text-1) transition-colors placeholder:text-(--text-2) hover:border-(--border) focus:border-(--border) focus:outline-none"
                    />
                </div>
            {/each}
        </div>
    {/if}
</Modal>
