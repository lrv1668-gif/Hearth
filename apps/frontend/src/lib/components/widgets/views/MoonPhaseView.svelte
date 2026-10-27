<script lang="ts">
    import { calcMoonPhase, moonPath } from '$lib/constants/moonphase';
    import type { FrameSize } from '$lib/constants/frameTemplates';

    /** `size`: a frame template slot's size class — M and L draw a larger moon with a
     *  larger name. Omitted (the dashboard, the frame's columns) is the compact S. */
    let { now = new Date(), size = 's' }: { now?: Date; size?: FrameSize } = $props();

    const moon = $derived(calcMoonPhase(now));
    const litPath = $derived(moonPath(moon.phase));
    const disc = $derived({ s: 'h-[4em] w-[4em]', m: 'h-[6.5em] w-[6.5em]', l: 'h-[10em] w-[10em]' }[size]);
    const name = $derived({ s: '', m: 'type-subtitle', l: 'type-title' }[size]);
</script>

<!--
@component
Tonight's moon and the next phase — shared by the dashboard widget and the e-paper
frame. Sized in em off body text, so the frame's larger type scales the moon too; the
outline uses --stroke (1.5px, 3px on the frame) and the unlit side shows the ground.
-->
<div class="type-body flex items-center {size === 'l' ? 'gap-[1.5em]' : 'gap-[1em]'}">
    <svg viewBox="-50 -50 100 100" class="{disc} shrink-0" aria-label="Moon phase: {moon.name}" role="img">
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
        <p class="{name} font-semibold text-(--text-1)">{moon.name}</p>
        <p class="type-label text-(--text-2)">{moon.illumination}% illuminated</p>
        <p class="type-label text-(--text-2)">
            {moon.nextName} in {moon.nextDays} day{moon.nextDays === 1 ? '' : 's'}
        </p>
    </div>
</div>
