<script lang="ts">
    import { calcMoonPhase, moonPath, upcomingPhases } from '$lib/constants/moonphase';

    // L adds the lunar month ahead as a strip of the next four major phases.
    let { now, large = false }: { now: Date; large?: boolean } = $props();

    const moon = $derived(calcMoonPhase(now));
    const phases = $derived(upcomingPhases(now));

    const shortDate = (d: Date) => d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
</script>

{#snippet disc(phase: number, label?: string)}
    <svg viewBox="-50 -50 100 100" class="block h-full w-full" aria-label={label} role={label ? 'img' : undefined}>
        <circle r="45" fill="none" stroke="var(--border)" stroke-width="3" vector-effect="non-scaling-stroke" />
        <path d={moonPath(phase, 45)} fill="var(--block)" />
    </svg>
{/snippet}

<!--
@component
Tonight's moon for a template's M or L slot: the disc sized to the slot (container units
off the slot body), its name in the display face beside it — or below it in a tall slot
— and, in L, the next four major phases with their dates. The lit side is the solid
--block ink; S slots and the free columns use the shared MoonPhaseView.
-->
<div class="moon flex h-full flex-col {large ? 'gap-10' : ''}">
    <div class="hero flex min-h-0 flex-1 items-center gap-12">
        <div class="disc aspect-square shrink-0">{@render disc(moon.phase, `Moon phase: ${moon.name}`)}</div>
        <div class="min-w-0">
            <p
                class="font-(family-name:--display-family) leading-tight font-medium {large
                    ? 'type-hero'
                    : 'type-title'}"
            >
                {moon.name}
            </p>
            <p class="type-body mt-3 text-(--text-2)">{moon.illumination}% illuminated</p>
            <p class="type-body text-(--text-2)">
                {moon.nextName} in {moon.nextDays} day{moon.nextDays === 1 ? '' : 's'}
            </p>
        </div>
    </div>
    {#if large}
        <ul class="strip grid shrink-0 grid-cols-4 gap-6 border-t-3 border-(--border) pt-8">
            {#each phases as p (p.name)}
                <li class="flex items-center gap-4">
                    <span class="type-body h-[2.2em] w-[2.2em] shrink-0">{@render disc(p.phase)}</span>
                    <span class="min-w-0">
                        <span class="type-label block truncate font-semibold">{p.name}</span>
                        <span class="type-label block truncate text-(--text-2)">{shortDate(p.date)}</span>
                    </span>
                </li>
            {/each}
        </ul>
    {/if}
</div>

<style>
    /* The slot body is a size container (FrameDaily): the disc takes most of the hero's
       height, but never more than ~45% of the width, so the name keeps room beside it. */
    .disc {
        height: min(100%, 45cqw);
    }
    /* A tall slot (portrait halves, a landscape Feature slot) stacks the name under the
       disc instead, and the disc sizes to the width. */
    @container (aspect-ratio < 1.1) {
        .hero {
            flex-direction: column;
            justify-content: center;
            align-items: flex-start;
            gap: calc(var(--spacing) * 6);
        }
        .disc {
            height: auto;
            width: min(75cqw, 45cqh);
        }
        .strip {
            grid-template-columns: repeat(2, minmax(0, 1fr));
        }
    }
</style>
