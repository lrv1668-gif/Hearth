<script lang="ts">
    import type { Plant } from '$lib/api';
    import PlantAddEdit from '../PlantAddEdit.svelte';
    import { Trash2, X } from '@lucide/svelte';

    interface Props {
        open?: boolean;
        plant?: Plant | null;
        onAdd?: (name: string, species: string | undefined, wateringIntervalDays: number) => void;
        onSave?: (name: string, species: string | undefined, wateringIntervalDays: number) => void;
        onDelete?: (id: number) => void;
    }

    let { open = $bindable(false), plant = null, onAdd, onSave, onDelete }: Props = $props();

    const isEdit = $derived(!!plant);

    let dialog = $state<HTMLDialogElement | null>(null);

    $effect(() => {
        if (!dialog) return;
        if (open) {
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

    function handleAdd(name: string, species: string | undefined, wateringIntervalDays: number) {
        onAdd?.(name, species, wateringIntervalDays);
        close();
    }

    function handleSave(name: string, species: string | undefined, wateringIntervalDays: number) {
        onSave?.(name, species, wateringIntervalDays);
        close();
    }

    function handleDelete() {
        if (!plant) return;
        onDelete?.(plant.id);
        close();
    }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={close}
    onclick={handleBackdropClick}
    class="w-full max-w-2xl bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    <div class="mx-4 space-y-5 rounded-xl border border-(--border) bg-(--bg) p-6 shadow-xl">
        <div class="flex items-center justify-between">
            <h2 class="text-(--text-1)">{isEdit ? 'Edit Plant' : 'Add a New Plant'}</h2>
            <button
                onclick={close}
                class="flex h-6 w-6 items-center justify-center text-(--text-3) transition-colors hover:text-(--text-1)"
                aria-label="Close"
            >
                <X class="icon-md" />
            </button>
        </div>

        {#if isEdit}
            <PlantAddEdit plant={plant ?? undefined} onSave={handleSave} />
        {:else}
            <PlantAddEdit onAdd={handleAdd} />
        {/if}

        {#if isEdit}
            <div class="border-t border-(--border) pt-2">
                <button
                    onclick={handleDelete}
                    class="text-(--text-3) transition-colors hover:text-(--text-1)"
                    aria-label="Delete plant"
                >
                    <Trash2 class="icon-md" />
                </button>
            </div>
        {/if}
    </div>
</dialog>
