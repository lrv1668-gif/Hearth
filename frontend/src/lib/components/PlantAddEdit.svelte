<script lang="ts">
    import type { Plant } from '$lib/api';

    interface Props {
        plant?: Plant;
        onAdd?: (name: string, species: string | undefined, wateringIntervalDays: number) => void;
        onSave?: (name: string, species: string | undefined, wateringIntervalDays: number) => void;
    }

    let { plant, onAdd, onSave }: Props = $props();

    const isEdit = $derived(!!plant);

    let name = $state('');
    let species = $state('');
    let wateringIntervalDays = $state(7);

    $effect(() => {
        if (!plant) return;
        name = plant.name;
        species = plant.species ?? '';
        wateringIntervalDays = plant.watering_interval_days;
    });

    function handleSubmit() {
        const trimmedName = name.trim();
        if (!trimmedName) return;
        if (wateringIntervalDays <= 0) return;

        if (isEdit) {
            onSave?.(trimmedName, species.trim() || undefined, wateringIntervalDays);
        } else {
            onAdd?.(trimmedName, species.trim() || undefined, wateringIntervalDays);
            name = '';
            species = '';
            wateringIntervalDays = 7;
        }
    }
</script>

<form
    class="space-y-3"
    onsubmit={(e) => {
        e.preventDefault();
        handleSubmit();
    }}
>
    <input
        bind:value={name}
        placeholder="Plant name"
        class="type-body w-full rounded-lg bg-(--surface)
               px-4 py-2.5 text-(--text-1) placeholder-(--text-2) transition
               outline-none focus:ring-1 focus:ring-(--border)"
    />

    <input
        bind:value={species}
        placeholder="Species (optional)"
        class="type-body w-full rounded-lg bg-(--surface)
               px-4 py-2.5 text-(--text-1) placeholder-(--text-2) transition
               outline-none focus:ring-1 focus:ring-(--border)"
    />

    <div class="flex items-center gap-2">
        <span class="type-body text-(--text-2)">Water every</span>
        <input
            type="number"
            bind:value={wateringIntervalDays}
            min={1}
            max={365}
            class="type-body w-16 rounded-lg bg-(--surface) px-3 py-1.5 text-center
                   text-(--text-2) outline-none focus:ring-1 focus:ring-(--border)"
        />
        <span class="type-body text-(--text-2)">day(s)</span>
    </div>

    <button
        type="submit"
        class="type-body rounded-lg bg-(--accent) px-4 py-2.5
               font-medium text-(--accent-fg) transition-colors hover:bg-(--accent-hi)"
    >
        {isEdit ? 'Save' : 'Add'}
    </button>
</form>
