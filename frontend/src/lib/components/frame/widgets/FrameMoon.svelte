<script lang="ts">
    import { calcMoonPhase, moonPath } from '$lib/constants/moonphase';

    let { now }: { now: Date } = $props();

    const moon = $derived(calcMoonPhase(now));
    const litPath = $derived(moonPath(moon.phase));
</script>

<div class="flex items-center gap-8">
    <svg viewBox="-50 -50 100 100" class="h-32 w-32 shrink-0" aria-label="Moon phase: {moon.name}" role="img">
        <circle r="40" fill="var(--bg)" stroke="var(--border)" stroke-width="3" />
        <path d={litPath} fill="var(--text-2)" />
    </svg>
    <div class="min-w-0">
        <p class="type-subtitle font-semibold">{moon.name}</p>
        <p class="type-label text-(--text-2)">{moon.illumination}% illuminated</p>
        <p class="type-label text-(--text-2)">
            {moon.nextName} in {moon.nextDays} day{moon.nextDays === 1 ? '' : 's'}
        </p>
    </div>
</div>
