<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { calcMoonPhase, moonPath } from '#lib/constants/moonphase.ts';
    import { nextSunrise } from '#lib/frame/sky.ts';
    import FrameAlert from '../FrameAlert.svelte';

    let { now }: { now: Date } = $props();

    const DAY = 86_400_000;
    const moon = $derived(calcMoonPhase(now));
    const lastNight = $derived(calcMoonPhase(new Date(now.getTime() - DAY)));
    const tomorrow = $derived(calcMoonPhase(new Date(now.getTime() + DAY)));
    const sunrise = $derived(nextSunrise(weatherStore.forecast, now));
    const sunriseTime = $derived(sunrise?.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }));
    const temperature = $derived(weatherStore.current ? Math.round(weatherStore.current.temperature) : null);
</script>

{#snippet disc(phase: number, label: string, cls: string)}
    <svg viewBox="-50 -50 100 100" class={cls} aria-label={label} role="img">
        <circle
            r="46"
            fill="none"
            stroke="var(--border)"
            vector-effect="non-scaling-stroke"
            style="stroke-width: var(--stroke)"
        />
        <path d={moonPath(phase, 46)} fill="var(--text-2)" />
    </svg>
{/snippet}

{#snippet neighbour(m: ReturnType<typeof calcMoonPhase>, when: string)}
    <figure class="flex flex-col items-center gap-5">
        {@render disc(m.phase, `${when}: ${m.name}`, 'side')}
        <figcaption>
            <span class="type-label block font-semibold">{when}</span>
            <span class="type-label block text-(--text-2)">{m.illumination}%</span>
        </figcaption>
    </figure>
{/snippet}

<!--
@component
Night mode for the e-paper frame: a quiet page for overnight schedule rules — a large
moon in tonight's phase flanked by smaller ones for last night and tomorrow night, its
name, and one line for the next sunrise and the temperature — and the weather alert,
when there is one (frost tonight). Drawn like the dashboard's MoonPhaseView (outline in
--border, lit side in --text-2, unlit side the ground) but as a poster rather than a
widget row. The theme is whatever the frame uses; a dark one such as Night suits it best.
-->
<div class="absolute inset-0 flex flex-col items-center justify-center gap-10 text-center">
    <div class="flex items-center gap-16">
        {@render neighbour(lastNight, 'Last night')}
        {@render disc(moon.phase, `Moon phase: ${moon.name}`, 'moon')}
        {@render neighbour(tomorrow, 'Tomorrow')}
    </div>
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
    <FrameAlert class="type-body -mt-4" />
</div>

<style>
    .moon {
        width: calc(480px * var(--frame-scale, 1));
        height: calc(480px * var(--frame-scale, 1));
    }
    :global(.frame.portrait) .moon {
        width: calc(400px * var(--frame-scale, 1));
        height: calc(400px * var(--frame-scale, 1));
    }
    /* The neighbours sit level with the big moon's centre; their captions hang below. */
    figure :global(.side) {
        width: calc(160px * var(--frame-scale, 1));
        height: calc(160px * var(--frame-scale, 1));
    }
    :global(.frame.portrait) figure :global(.side) {
        width: calc(140px * var(--frame-scale, 1));
        height: calc(140px * var(--frame-scale, 1));
    }
</style>
