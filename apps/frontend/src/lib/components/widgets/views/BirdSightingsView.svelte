<script lang="ts">
    import type { BirdSighting } from '#lib/api.ts';
    import { dayLabel, distanceLabel, rareFirst, shortLocation } from '#lib/birds.ts';
    import { itemsThatFit } from '#lib/frame.ts';

    /** `lines`: the frame's line budget — shows only what fits (at most 5). Omit to
     *  show every sighting (the dashboard scrolls them). */
    let { sightings, now = new Date(), lines }: { sightings: BirdSighting[]; now?: Date; lines?: number } = $props();

    // A sighting (name + where/when) is ~2.15 lines with its gap; the last needs no gap.
    const shown = $derived.by(() => {
        const sorted = rareFirst(sightings);
        return lines === undefined ? sorted : sorted.slice(0, Math.min(5, itemsThatFit(lines + 0.35, 2.15)));
    });
</script>

<!--
@component
Recent eBird sightings nearby, rare ones first — shared by the dashboard widget and
the e-paper frame. Gaps are in em, so they grow with the frame's larger type.
-->
{#if shown.length === 0}
    <p class="type-body text-(--text-2)">No sightings reported nearby this week.</p>
{:else}
    <ul class="type-body flex flex-col gap-[0.5em]">
        {#each shown as s (s.species_code)}
            <li class="min-w-0">
                <p class="truncate text-(--text-1)">
                    {s.common_name}
                    {#if s.is_notable}<span
                            class="type-label ml-[0.5em] font-semibold tracking-wide text-(--accent) uppercase"
                            >Rare</span
                        >{/if}
                </p>
                <p class="type-label truncate text-(--text-2)" title={s.location}>
                    {shortLocation(s.location)} · {distanceLabel(s.distance_mi)}, {dayLabel(s.observed_at, now)}
                </p>
            </li>
        {/each}
    </ul>
{/if}
