<script lang="ts">
    import { X, Trash2, Upload } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '$lib/api';

    interface Props {
        open?: boolean;
        photos?: UploadedPhoto[];
        onUploaded?: (photo: UploadedPhoto) => void;
        onDeleted?: (id: string) => void;
    }

    let { open = $bindable(false), photos = [], onUploaded, onDeleted }: Props = $props();

    let dialog = $state<HTMLDialogElement | null>(null);
    let uploading = $state(false);
    let uploadLabel = $state('Uploading…');
    let error = $state<string | null>(null);

    $effect(() => {
        if (!dialog) return;
        if (open) {
            error = null;
            dialog.showModal();
        } else if (dialog.open) {
            dialog.close();
        }
    });

    function close() {
        open = false;
    }

    function handleBackdropClick(e: MouseEvent) {
        if (e.target === dialog) close();
    }

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
            if (r.status === 'ok' && r.photo) onUploaded?.(r.photo);
            else if (r.status === 'error') serverErrors.push(r.error ?? r.file_name);
        }

        const allErrors = [...clientErrors, ...serverErrors];
        if (allErrors.length === 1) error = allErrors[0];
        else if (allErrors.length > 1) error = `${allErrors.length} files had issues.`;
    }

    async function handleDelete(id: string) {
        const ok = await api.photos.delete(id);
        if (ok) onDeleted?.(id);
    }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={close}
    onclick={handleBackdropClick}
    class="w-full max-w-3xl bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    <div class="mx-4 flex max-h-[85vh] flex-col rounded-xl border border-[var(--border)] bg-[var(--bg)] shadow-xl">
        <!-- Sticky header -->
        <div class="flex shrink-0 items-center justify-between border-b border-[var(--border)] px-5 py-4">
            <div>
                <h2 class="text-[var(--text-1)]">Photo collection</h2>
                <p class="type-label text-[var(--text-2)]">
                    {photos.length === 0 ? 'No photos yet' : `${photos.length} photo${photos.length === 1 ? '' : 's'}`}
                </p>
            </div>
            <div class="flex items-center gap-2">
                <label
                    class="flex cursor-pointer items-center gap-2 rounded-full border border-[var(--border)] px-3 py-1.5 transition-colors hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)] {uploading
                        ? 'pointer-events-none opacity-50'
                        : ''}"
                >
                    <Upload class="h-3.5 w-3.5 text-[var(--text-1)]" />
                    <span class="type-label text-[var(--text-1)]">{uploading ? uploadLabel : 'Add photos'}</span>
                    <input
                        type="file"
                        accept="image/jpeg,image/png,image/webp"
                        multiple
                        class="sr-only"
                        onchange={handleFileChange}
                        disabled={uploading}
                    />
                </label>
                <button
                    onclick={close}
                    class="flex h-7 w-7 items-center justify-center text-[var(--text-3)] transition-colors hover:text-[var(--text-1)]"
                    aria-label="Close"
                >
                    <X class="icon-md" />
                </button>
            </div>
        </div>

        {#if error}
            <p class="type-label shrink-0 border-b border-[var(--border)] px-5 py-2.5 text-red-500">{error}</p>
        {/if}

        <!-- Scrollable grid -->
        <div class="min-h-0 flex-1 overflow-y-auto p-5">
            {#if photos.length === 0}
                <p class="type-label py-12 text-center text-[var(--text-3)]">
                    No photos yet. Upload some to get started.
                </p>
            {:else}
                <div class="grid grid-cols-3 gap-3">
                    {#each photos as p (p.id)}
                        <div class="group relative aspect-square overflow-hidden rounded-lg bg-[var(--surface)]">
                            <img src={p.thumb_url} alt="" class="absolute inset-0 h-full w-full object-cover" />
                            <button
                                onclick={() => handleDelete(p.id)}
                                class="absolute right-1.5 top-1.5 rounded-md bg-black/60 p-1.5 opacity-0 transition-opacity focus:opacity-100 group-hover:opacity-100"
                                aria-label="Delete photo"
                            >
                                <Trash2 class="h-3.5 w-3.5 text-white" />
                            </button>
                        </div>
                    {/each}
                </div>
            {/if}
        </div>
    </div>
</dialog>
