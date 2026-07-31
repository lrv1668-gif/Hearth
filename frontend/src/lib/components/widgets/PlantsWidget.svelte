<script lang="ts">
    import { Droplet } from '@lucide/svelte';
    import { plantStore, waterPlant } from '$lib/stores/PlantStore.svelte.ts';

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

{#if plantStore.plants.length === 0}
    <p class="type-body text-(--text-2)">No plants added yet.</p>
{:else}
    <ul class="space-y-3">
        {#each plantStore.plants as plant (plant.id)}
            <li class="flex items-center gap-3">
                <div class="min-w-0 flex-1">
                    <p class="type-body truncate leading-tight text-(--text-1)">{plant.name}</p>
                    {#if plant.is_overdue}
                        <p class="type-label font-semibold tracking-widest text-(--accent) uppercase">
                            Overdue since {formatDate(plant.next_watering_due)}
                        </p>
                    {:else}
                        <p class="type-label text-(--text-2)">Water by {formatDate(plant.next_watering_due)}</p>
                    {/if}
                    <p class="type-caption text-(--text-3)">
                        {plant.last_watered_at ? `Last watered ${formatWateredAt(plant.last_watered_at)}` : 'Never watered'}
                    </p>
                </div>
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
