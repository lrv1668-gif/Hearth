<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { calcMoonPhase, moonPath } from '#lib/constants/moonphase.ts';
    import { nextSunrise } from '#lib/frame.ts';

    let { now }: { now: Date } = $props();

    const moon = $derived(calcMoonPhase(now));
    const sunrise = $derived(nextSunrise(weatherStore.forecast, now));
    const sunriseTime = $derived(sunrise?.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }));
    const temperature = $derived(weatherStore.current ? Math.round(weatherStore.current.temperature_f) : null);
</script>

<!--
@component
Night mode for the e-paper frame: a quiet page for overnight schedule rules — a large
moon in tonight's phase, its name, and one line for the next sunrise and the
temperature. Drawn like the dashboard's MoonPhaseView (outline in --border, lit side in
--text-2, unlit side the ground) but as a poster rather than a widget row. The theme is
whatever the frame uses; a dark one such as Night suits it best.
-->
<div class="absolute inset-0 flex flex-col items-center justify-center gap-10 text-center">
    <svg viewBox="-50 -50 100 100" class="moon" aria-label="Moon phase: {moon.name}" role="img">
        <circle
            r="46"
            fill="none"
            stroke="var(--border)"
            vector-effect="non-scaling-stroke"
            style="stroke-width: var(--stroke)"
        />
        <path d={moonPath(moon.phase, 46)} fill="var(--text-2)" />
    </svg>
    <div>
        <p class="type-hero font-(family-name:--display-family) leading-none font-medium italic">{moon.name}</p>
        <p class="type-body mt-4 text-(--text-2)">
            {moon.illumination}% illuminated · {moon.nextName} in {moon.nextDays} day{moon.nextDays === 1 ? '' : 's'}
        </p>
    </div>
    {#if sunriseTime || temperature !== null}
        <p class="type-subtitle font-semibold">
            {[sunriseTime && `Sunrise ${sunriseTime}`, temperature !== null && `${temperature}°`]
                .filter(Boolean)
                .join(' · ')}
        </p>
    {/if}
</div>

<style>
    .moon {
        width: calc(480px * var(--frame-scale, 1));
        height: calc(480px * var(--frame-scale, 1));
    }
    :global(.frame.portrait) .moon {
        width: calc(560px * var(--frame-scale, 1));
        height: calc(560px * var(--frame-scale, 1));
    }
</style>
