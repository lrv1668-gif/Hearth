<script lang="ts">
    import { birdsStore } from '$lib/stores/BirdsStore.svelte.ts';
    import { dayLabel, distanceLabel, shortLocation } from '$lib/birds';
    import { itemsThatFit } from '$lib/frame';

    let { now, lines }: { now: Date; lines: number } = $props();

    // Rare sightings first — they're the ones worth walking over to the frame for.
    const sightings = $derived(
        [...birdsStore.sightings]
            .sort((a, b) => Number(b.is_notable) - Number(a.is_notable))
            .slice(0, Math.min(5, itemsThatFit(lines + 0.35, 2.15)))
    );
</script>

{#if sightings.length === 0}
    <p class="type-body text-(--text-2)">No sightings nearby this week.</p>
{:else}
    <ul class="space-y-4">
        {#each sightings as s (s.species_code)}
            <li class="min-w-0">
                <p class="type-body truncate">
                    {s.common_name}
                    {#if s.is_notable}<span class="type-label ml-2 font-semibold text-(--accent) uppercase">Rare</span
                        >{/if}
                </p>
                <p class="type-label truncate text-(--text-2)">
                    {shortLocation(s.location)} · {distanceLabel(s.distance_mi)}, {dayLabel(s.observed_at, now)}
                </p>
            </li>
        {/each}
    </ul>
{/if}
