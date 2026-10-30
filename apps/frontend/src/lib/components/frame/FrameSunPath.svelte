<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { almanacStore } from '#lib/stores/AlmanacStore.svelte.ts';
    import { durationLabel, getFrameScale, nearestHour, sunProgress } from '#lib/frame.ts';
    import { localDateKey } from '#lib/utils.ts';

    /** `compact`: a smaller dome that fits beside the weekday in portrait — the times
     *  move into one row under the horizon and the daylight trend is dropped. */
    let { now, compact = false }: { now: Date; compact?: boolean } = $props();

    // Dome geometry in SVG units: an 18-unit ring over a horizon at CY. Full size keeps
    // 72 units either side of its base for the sunrise and sunset times; compact keeps
    // just enough for the times row beneath to span the base.
    const R = $derived(compact ? 118 : 150);
    const r = $derived(R - 18);
    const CX = $derived(R + (compact ? 24 : 72));
    const W = $derived(2 * CX);
    const CY = $derived(R + 12);
    const H = $derived(CY + 52);
    // The dome is drawn at the 1600px reference and scaled with the frame; its labels
    // sit outside that group, at scaled positions, since their type tokens already scale.
    const frameScale = getFrameScale();
    const k = $derived(frameScale());

    const today = $derived(weatherStore.forecast.find((f) => f.date === localDateKey(now)) ?? null);
    // Open-Meteo sends local ISO times without an offset, which Date parses as local.
    const sunrise = $derived(today ? new Date(today.sunrise) : null);
    const sunset = $derived(today ? new Date(today.sunset) : null);
    // The sun moves in whole hours, so a re-render within the hour keeps the same pixels
    const sunAt = $derived(nearestHour(now));
    const progress = $derived(sunrise && sunset ? sunProgress(sunrise, sunset, sunAt) : null);
    // After sunset the whole day is lit; before sunrise none of it.
    const lit = $derived(progress ?? (sunset && sunAt > sunset ? 1 : 0));
    const temperature = $derived(weatherStore.current ? Math.round(weatherStore.current.temperature_f) : null);

    const trend = $derived.by(() => {
        const perDay = almanacStore.almanac?.daylight?.trend_minutes_per_day;
        if (perDay == null) return '';
        const m = Math.round(perDay);
        return m === 0 ? ' · holding steady' : ` · ${m > 0 ? '+' : '−'}${Math.abs(m)} m a day`;
    });

    function point(radius: number, f: number): [number, number] {
        const a = Math.PI * (1 - f);
        return [CX + radius * Math.cos(a), CY - radius * Math.sin(a)];
    }

    /** The ring between f0 and f1 (fractions of daylight) as a closed path. */
    function sector(f0: number, f1: number): string {
        const [o0, o1, i1, i0] = [point(R, f0), point(R, f1), point(r, f1), point(r, f0)];
        return `M${o0} A${R} ${R} 0 0 1 ${o1} L${i1} A${r} ${r} 0 0 0 ${i0} Z`;
    }

    function clock(d: Date): string {
        return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }).replace(/\s?[AP]M$/, '');
    }
</script>

<!--
@component
The masthead's sun path: today's daylight as a dome from sunrise to sunset. The
elapsed part fills with --block, the sun is an --accent disc on the ring, and the
current temperature sits under the dome. Below the horizon, a hatched ground strip,
sunrise and sunset beside the dome's base, and the daylight length (with the
almanac's trend) beneath. Without sun
times it falls back to the temperature alone.
-->
{#if sunrise && sunset}
    {@const sun = progress === null ? null : point((R + r) / 2, progress)}
    <svg
        viewBox="0 0 {W * k} {H * k}"
        width={W * k}
        height={H * k}
        class="shrink-0"
        role="img"
        aria-label="Daylight {durationLabel(sunrise, sunset)}, sunrise {clock(sunrise)}, sunset {clock(sunset)}"
    >
        <defs>
            <pattern id="sunpath-hatch" width="9" height="9" patternUnits="userSpaceOnUse">
                <rect width="3" height="9" fill="var(--border)" />
            </pattern>
        </defs>
        <g transform="scale({k})">
            <path d={sector(0, 1)} fill="none" stroke="var(--border)" stroke-width="3" />
            {#if lit > 0}<path d={sector(0, lit)} fill="var(--block)" />{/if}
            <line x1="0" y1={CY + 1.5} x2={W} y2={CY + 1.5} stroke="var(--border)" stroke-width="3" />
            <rect x="0" y={CY + 3} width={W} height="18" fill="url(#sunpath-hatch)" />
            {#if sun}
                <circle cx={sun[0]} cy={sun[1]} r="16" fill="var(--accent)" stroke="var(--bg)" stroke-width="6" />
            {/if}
        </g>
        {#if temperature !== null}
            <text
                x={CX * k}
                y={(CY - 12) * k}
                text-anchor="middle"
                fill="var(--text-1)"
                class="{compact
                    ? 'type-stamp'
                    : 'type-hero'} font-(family-name:--display-family) font-medium tracking-[-0.04em]"
            >
                {temperature}°
            </text>
        {/if}
        <!-- Placed in the dome's fixed geometry, so they keep the tuned size whatever
             the frame's text size (--frame-text) — larger would run together. -->
        <g class="type-label" fill="var(--text-2)" style="font-size: calc(var(--font-label) / var(--frame-text, 1))">
            {#if compact}
                <text x="0" y={(CY + 50) * k}>{clock(sunrise)}</text>
                <text x={CX * k} y={(CY + 50) * k} text-anchor="middle">{durationLabel(sunrise, sunset)}</text>
                <text x={W * k} y={(CY + 50) * k} text-anchor="end">{clock(sunset)}</text>
            {:else}
                <text x={(CX - R - 12) * k} y={(CY - 10) * k} text-anchor="end">{clock(sunrise)}</text>
                <text x={(CX + R + 12) * k} y={(CY - 10) * k}>{clock(sunset)}</text>
                <text x={CX * k} y={(CY + 50) * k} text-anchor="middle">{durationLabel(sunrise, sunset)}{trend}</text>
            {/if}
        </g>
    </svg>
{:else if temperature !== null}
    <p class="type-hero shrink-0 font-(family-name:--display-family) leading-[0.85] font-medium tracking-[-0.04em]">
        {temperature}°
    </p>
{/if}
