<script lang="ts">
    const SYNODIC_PERIOD = 29.53059; // days
    const KNOWN_NEW_MOON = new Date('2000-01-06T18:14:00Z');

    function calcPhase(): { phase: number; name: string; illumination: number; nextName: string; nextDays: number } {
        const daysSince = (Date.now() - KNOWN_NEW_MOON.getTime()) / 86_400_000;
        const phase = (((daysSince % SYNODIC_PERIOD) + SYNODIC_PERIOD) % SYNODIC_PERIOD) / SYNODIC_PERIOD;
        const illumination = Math.round(((1 - Math.cos(2 * Math.PI * phase)) / 2) * 100);

        const phaseNames: [number, string][] = [
            [0.0625, 'New Moon'],
            [0.1875, 'Waxing Crescent'],
            [0.3125, 'First Quarter'],
            [0.4375, 'Waxing Gibbous'],
            [0.5625, 'Full Moon'],
            [0.6875, 'Waning Gibbous'],
            [0.8125, 'Last Quarter'],
            [0.9375, 'Waning Crescent'],
            [1.0001, 'New Moon'],
        ];
        const name = phaseNames.find(([t]) => phase < t)![1];

        // Next major phase (0 = new, 0.25 = first quarter, 0.5 = full, 0.75 = last quarter)
        const majors: [number, string][] = [
            [0, 'New Moon'],
            [0.25, 'First Quarter'],
            [0.5, 'Full Moon'],
            [0.75, 'Last Quarter'],
        ];
        const next = majors
            .map(([t, n]) => ({ name: n, days: Math.round(((t - phase + 1) % 1) * SYNODIC_PERIOD) }))
            .filter(({ days }) => days > 0)
            .sort((a, b) => a.days - b.days)[0];

        return { phase, name, illumination, nextName: next.name, nextDays: next.days };
    }

    // The lit-portion SVG path. phase in [0,1].
    // Uses a half-circle for the lit side + an elliptical terminator arc back to the top.
    function moonPath(phase: number, r = 40): string {
        const tx = r * Math.cos(2 * Math.PI * phase); // terminator x-radius (signed)
        const rx = Math.abs(tx);
        if (phase < 0.5) {
            // Waxing — right side lit
            const sweep = tx >= 0 ? 1 : 0;
            return `M 0,${-r} A ${r},${r} 0 0 1 0,${r} A ${rx},${r} 0 0 ${sweep} 0,${-r} Z`;
        } else {
            // Waning — left side lit
            const sweep = tx >= 0 ? 0 : 1;
            return `M 0,${-r} A ${r},${r} 0 0 0 0,${r} A ${rx},${r} 0 0 ${sweep} 0,${-r} Z`;
        }
    }

    const { phase, name, illumination, nextName, nextDays } = calcPhase();
    const litPath = moonPath(phase);

    interface Props {
        align?: 'left' | 'right';
    }
    let { align = 'left' }: Props = $props();
</script>

<div class="flex items-center gap-4 {align === 'right' ? 'flex-row-reverse' : ''}">
    <svg viewBox="-50 -50 100 100" class="w-16 h-16 flex-shrink-0" aria-label="Moon phase: {name}" role="img">
        <circle r="40" fill="var(--surface)" stroke="var(--border)" stroke-width="1.5" />
        <path d={litPath} fill="var(--text-2)" />
    </svg>

    <div class="flex flex-col gap-0.5 min-w-0 {align === 'right' ? 'text-right' : ''}">
        <p class="type-body font-medium text-[var(--text-1)]">{name}</p>
        <p class="type-label text-[var(--text-2)]">{illumination}% illuminated</p>
        <p class="type-label text-[var(--text-3)]">
            {nextName} in {nextDays} day{nextDays === 1 ? '' : 's'}
        </p>
    </div>
</div>
