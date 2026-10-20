<script lang="ts">
    import { calcMoonPhase, moonPath } from '$lib/constants/moonphase';

    const { phase, name, illumination, nextName, nextDays } = calcMoonPhase();
    const litPath = moonPath(phase);
</script>

<div class="flex items-center gap-4">
    <svg viewBox="-50 -50 100 100" class="h-16 w-16 flex-shrink-0" aria-label="Moon phase: {name}" role="img">
        <circle r="40" fill="var(--surface)" stroke="var(--border)" stroke-width="1.5" />
        <path d={litPath} fill="var(--text-2)" />
    </svg>

    <div class="flex min-w-0 flex-col gap-0.5">
        <p class="type-body font-medium text-(--text-1)">{name}</p>
        <p class="type-label text-(--text-2)">{illumination}% illuminated</p>
        <p class="type-label text-(--text-2)">
            {nextName} in {nextDays} day{nextDays === 1 ? '' : 's'}
        </p>
    </div>
</div>
