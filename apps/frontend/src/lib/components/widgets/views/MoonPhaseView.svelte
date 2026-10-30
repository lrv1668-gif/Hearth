<script lang="ts">
    import { calcMoonPhase, moonPath } from '#lib/constants/moonphase.ts';

    let { now = new Date() }: { now?: Date } = $props();

    const moon = $derived(calcMoonPhase(now));
    const litPath = $derived(moonPath(moon.phase));
</script>

<!--
@component
Tonight's moon and the next phase — shared by the dashboard widget and the e-paper
frame. Sized in em off body text, so the frame's larger type scales the moon too; the
outline uses --stroke (1.5px, 3px on the frame) and the unlit side shows the ground.
-->
<div class="type-body flex items-center gap-[1em]">
    <svg viewBox="-50 -50 100 100" class="h-[4em] w-[4em] shrink-0" aria-label="Moon phase: {moon.name}" role="img">
        <circle
            r="40"
            fill="none"
            stroke="var(--border)"
            vector-effect="non-scaling-stroke"
            style="stroke-width: var(--stroke)"
        />
        <path d={litPath} fill="var(--text-2)" />
    </svg>
    <div class="min-w-0">
        <p class="font-semibold text-(--text-1)">{moon.name}</p>
        <p class="type-label text-(--text-2)">{moon.illumination}% illuminated</p>
        <p class="type-label text-(--text-2)">
            {moon.nextName} in {moon.nextDays} day{moon.nextDays === 1 ? '' : 's'}
        </p>
    </div>
</div>
