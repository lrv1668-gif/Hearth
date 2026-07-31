<script lang="ts">
    import type { Plant } from '$lib/api';
    import { plantStore, addPlant, waterPlant, editPlant, removePlant } from '$lib/stores/PlantStore.svelte.ts';
    import PlantModal from '$lib/components/modals/PlantModal.svelte';
    import { Droplet, Plus } from '@lucide/svelte';

    let modalOpen = $state(false);
    let editingPlant = $state<Plant | null>(null);

    function openNewPlant() {
        editingPlant = null;
        modalOpen = true;
    }

    function openEditPlant(plant: Plant) {
        editingPlant = plant;
        modalOpen = true;
    }

    function handleSave(name: string, species: string | undefined, wateringIntervalDays: number) {
        if (editingPlant) {
            editPlant(editingPlant, name, species, wateringIntervalDays);
            editingPlant = null;
        }
    }

    function formatDate(iso: string): string {
        return new Date(`${iso.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
        });
    }

    function formatWateredAt(iso: string): string {
        const date = new Date(iso);
        const time = date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
        return `${formatDate(iso)}, ${time}`;
    }
</script>

<svelte:head>
    <title>Hearth — Plants</title>
</svelte:head>

<main class="mx-auto flex min-h-0 w-full max-w-7xl flex-col gap-6 px-8 py-8">
    <div class="flex items-center justify-between">
        <h1 class="type-title text-(--text-1)">Plants</h1>
        <button
            onclick={openNewPlant}
            class="type-body flex items-center gap-2 rounded-lg bg-(--accent) px-4
                   py-2.5 font-medium text-(--accent-fg) transition-colors hover:bg-(--accent-hi)"
        >
            <Plus class="icon-sm" />
            Add plant
        </button>
    </div>

    {#if plantStore.plants.length === 0}
        <p class="type-body text-(--text-2)">No plants added yet.</p>
    {:else}
        <ul class="space-y-2">
            {#each plantStore.plants as plant (plant.id)}
                <li class="flex items-center gap-3 rounded-lg bg-(--surface) px-4 py-3">
                    <button
                        onclick={() => openEditPlant(plant)}
                        class="min-w-0 flex-1 text-left transition-opacity hover:opacity-80"
                    >
                        <p class="type-body truncate leading-tight text-(--text-1)">{plant.name}</p>
                        {#if plant.species}
                            <p class="type-label text-(--text-3)">{plant.species}</p>
                        {/if}
                        {#if plant.is_overdue}
                            <p class="type-label font-semibold tracking-widest text-(--accent) uppercase">
                                Overdue since {formatDate(plant.next_watering_due)}
                            </p>
                        {:else}
                            <p class="type-label text-(--text-2)">
                                Water by {formatDate(plant.next_watering_due)} · every {plant.watering_interval_days} day(s)
                            </p>
                        {/if}
                        <p class="type-caption text-(--text-3)">
                            {plant.last_watered_at ? `Last watered ${formatWateredAt(plant.last_watered_at)}` : 'Never watered'}
                        </p>
                    </button>
                    <button
                        onclick={() => waterPlant(plant)}
                        class="flex flex-shrink-0 items-center gap-1 rounded-lg bg-(--surface-hi) px-3 py-1.5 transition-opacity hover:opacity-80"
                        aria-label={`Mark ${plant.name} as watered`}
                    >
                        <Droplet class="icon-sm text-(--accent)" />
                        <span class="type-label text-(--text-1)">Water</span>
                    </button>
                </li>
            {/each}
        </ul>
    {/if}
</main>

<PlantModal
    bind:open={modalOpen}
    plant={editingPlant}
    onAdd={addPlant}
    onSave={handleSave}
    onDelete={(id) => {
        removePlant(id);
        editingPlant = null;
    }}
/>
