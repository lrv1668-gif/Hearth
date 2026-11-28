<script lang="ts">
    import { Leaf, SunMedium, Sunrise, Wind } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { alertIcon, weatherIcon } from '#lib/constants/weather.ts';
    import {
        DENSE_TEXT_SIZE,
        feelsLike,
        hourLabel,
        stripLabels,
        stripPoints,
        upcomingHours,
        weatherDensity,
        weatherTiles,
        type WeatherTileId,
    } from '#lib/frame/weather.ts';
    import { localDateKey } from '#lib/utils.ts';
    import FrameEmpty from '../FrameEmpty.svelte';
    import FrameForecast from '../FrameForecast.svelte';

    let { now, textSize, portrait }: { now: Date; textSize: number; portrait: boolean } = $props();

    const density = $derived(weatherDensity(textSize, portrait));
    const current = $derived(weatherStore.current);
    const today = $derived(weatherStore.forecast.find((d) => d.date === localDateKey(now)) ?? null);
    const alerts = $derived(weatherStore.alerts.slice(0, density.alerts));
    const tiles = $derived(current ? weatherTiles(current, today).filter((t) => density.sun || t.id !== 'sun') : []);
    const feels = $derived(current ? feelsLike(current) : null);

    const hours = $derived(upcomingHours(weatherStore.hourly, now, density.hours));
    const points = $derived(stripPoints(hours));
    const labels = $derived(stripLabels(hours, density.step));
    // The line keeps clear of its box's edges so the stroke and dots aren't clipped.
    const inset = (y: number) => 12 + y * 0.76;
    const line = $derived(points.map((p) => `${p.x},${inset(p.y)}`).join(' '));

    const tileIcons: Record<WeatherTileId, typeof Wind> = { uv: SunMedium, air: Leaf, wind: Wind, sun: Sunrise };

    // Two short lines — the day's range, then how it feels — so neither breaks mid-phrase.
    const summary = $derived.by(() => {
        if (!current) return [];
        const range = today ? [`High ${Math.round(today.temp_max)}°`, `Low ${Math.round(today.temp_min)}°`] : [];
        const feel = [
            feels !== null ? `Feels like ${feels}°` : null,
            current.humidity !== null && current.humidity !== undefined ? `${current.humidity}% humidity` : null,
        ].filter((s): s is string => s !== null);
        return [range, feel].filter((line) => line.length > 0).map((line) => line.join(' · '));
    });
</script>

<!--
@component
Weather mode for the e-paper frame: Daily's weather in full. Alerts lead (two at most),
then today — a large icon and temperature with the high, low, feels-like and humidity —
beside up to four reading tiles (the day's UV peak, air quality, wind, sunrise to
sunset), the next hours as a temperature line with rain chances, and the week as
Daily's forecast band. Pressure, visibility and the like are left out to keep it calm;
at a large text size (weatherDensity) the strip, the week and the alerts shorten
instead of crowding. Portrait stacks the tiles under the temperature and lists the
week a day per row.
-->
{#if !weatherStore.loaded}
    <!-- The frame waits for the first load (data-ready) before it's screenshotted. -->
{:else if !current}
    <FrameEmpty text="Weather unavailable" />
{:else}
    {@const Icon = weatherIcon(current.weather_code)}
    <div class="flex h-full flex-col gap-9 px-16 pt-9">
        {#if alerts.length > 0}
            <ul class="flex shrink-0 flex-col gap-3 border-b-3 border-(--border) pb-5">
                {#each alerts as alert (alert.kind)}
                    {@const AlertIcon = alertIcon(alert.kind)}
                    <li class="type-body flex items-center gap-3 leading-tight font-semibold">
                        <AlertIcon class="icon-sm shrink-0" />
                        <span class="min-w-0">{alert.text}</span>
                    </li>
                {/each}
            </ul>
        {/if}

        <div class="today min-h-0 flex-1 overflow-hidden" class:dense={textSize >= DENSE_TEXT_SIZE}>
            <section class="hero min-w-0">
                <Icon class="hero-icon shrink-0 text-(--text-2)" />
                <p class="temp font-(family-name:--display-family) leading-[0.85] font-medium tracking-[-0.03em]">
                    {Math.round(current.temperature)}°
                </p>
                <div class="words min-w-0">
                    <p class="type-title flex items-center gap-3 leading-tight font-semibold">
                        <Icon class="inline-icon icon-md shrink-0 text-(--text-2)" />
                        {current.description}
                    </p>
                    {#each summary as line (line)}
                        <p class="type-body mt-2 leading-snug text-(--text-2)">{line}</p>
                    {/each}
                </div>
            </section>

            {#if tiles.length > 0}
                <section class="grid min-w-0 grid-cols-2 content-center gap-x-12 gap-y-8">
                    {#each tiles as tile (tile.id)}
                        {@const TileIcon = tileIcons[tile.id]}
                        <div class="min-w-0 border-t-3 border-(--border) pt-4">
                            <p
                                class="type-label flex items-center gap-2 leading-none font-semibold tracking-[0.16em] text-(--text-2) uppercase"
                            >
                                <TileIcon class="icon-xs shrink-0" />
                                {tile.label}
                            </p>
                            <!-- The band sits beside the value, wrapping under it only when it must. -->
                            <p class="mt-3 flex flex-wrap items-baseline gap-x-4 gap-y-1">
                                <span class="type-title leading-none font-semibold whitespace-nowrap">{tile.value}</span
                                >
                                {#if tile.detail}
                                    <span class="type-label leading-tight text-(--text-2)">{tile.detail}</span>
                                {/if}
                            </p>
                        </div>
                    {/each}
                </section>
            {/if}
        </div>

        {#if hours.length > 0}
            <section class="shrink-0" aria-label="The next {hours.length} hours">
                <div class="type-label relative h-[1.2em] font-semibold">
                    {#each labels as label (label.index)}
                        <span
                            class="absolute bottom-0 -translate-x-1/2 leading-none"
                            style:left="{points[label.index].x}%">{Math.round(label.hour.temperature)}°</span
                        >
                    {/each}
                </div>
                <div class="strip relative">
                    <svg viewBox="0 0 100 100" preserveAspectRatio="none" class="absolute inset-0 h-full w-full">
                        <polyline
                            points={line}
                            fill="none"
                            stroke="var(--text-1)"
                            stroke-linejoin="round"
                            vector-effect="non-scaling-stroke"
                            style="stroke-width: var(--stroke)"
                        />
                    </svg>
                    {#each labels as label (label.index)}
                        <span
                            class="dot absolute -translate-1/2 rounded-full {label.index === 0
                                ? 'now bg-(--accent)'
                                : 'bg-(--text-1)'}"
                            style:left="{points[label.index].x}%"
                            style:top="{inset(points[label.index].y)}%"
                        ></span>
                    {/each}
                </div>
                <div class="type-label relative mt-3 {labels.some((l) => l.rain !== null) ? 'h-[2.8em]' : 'h-[1.5em]'}">
                    {#each labels as label, i (label.index)}
                        {@const HourIcon = weatherIcon(label.hour.weather_code)}
                        <div
                            class="absolute top-0 flex -translate-x-1/2 flex-col items-center gap-2 leading-none"
                            style:left="{points[label.index].x}%"
                        >
                            <span class="flex items-center gap-2 whitespace-nowrap">
                                <HourIcon class="icon-sm text-(--text-2)" />
                                <span class="font-semibold">{hourLabel(label.hour, i === 0)}</span>
                            </span>
                            {#if label.rain !== null}
                                <span class="whitespace-nowrap text-(--text-2)">{label.rain}% rain</span>
                            {/if}
                        </div>
                    {/each}
                </div>
            </section>
        {/if}

        <div class="shrink-0">
            <FrameForecast {now} count={density.days} alert={false} rows={portrait} />
        </div>
    </div>
{/if}

<style>
    /* Today: the temperature beside its tiles in landscape, above them in portrait. A size
       container, so the numeral shrinks to the room left once the strip and week are in. */
    .today {
        container-type: size;
        display: grid;
        grid-template-columns: minmax(0, 1.1fr) minmax(0, 1fr);
        align-items: safe center;
        gap: calc(16 * var(--spacing));
    }
    :global(.frame.portrait) .today {
        grid-template-columns: minmax(0, 1fr);
        grid-template-rows: auto auto;
        align-content: safe center;
        gap: calc(12 * var(--spacing));
    }

    /* Landscape: the big icon beside a column of numeral and words. Portrait: numeral and
       words side by side, the icon shrunk into the description line. */
    .hero {
        display: grid;
        grid-template-columns: auto minmax(0, 1fr);
        grid-template-areas: 'icon temp' 'icon words';
        align-items: center;
        column-gap: calc(10 * var(--spacing));
        row-gap: calc(4 * var(--spacing));
    }
    .hero :global(.hero-icon) {
        grid-area: icon;
        width: calc(180px * var(--frame-scale, 1));
        height: calc(180px * var(--frame-scale, 1));
    }
    .hero :global(.inline-icon) {
        display: none;
    }
    .temp {
        grid-area: temp;
        align-self: end;
        font-size: min(calc(var(--frame-display-text) * 260px * var(--font-scale, 1) * var(--frame-scale, 1)), 42cqh);
    }
    .words {
        grid-area: words;
        align-self: start;
    }
    /* Large text in landscape: the tiles take the wider column so their values stay on one
       line, and the hero stacks like portrait's, its icon moved into the description. */
    .today.dense {
        grid-template-columns: minmax(0, 1fr) minmax(0, 1.25fr);
    }
    .dense .hero {
        grid-template-columns: minmax(0, 1fr);
        grid-template-areas: 'temp' 'words';
    }
    .dense .hero :global(.hero-icon) {
        display: none;
    }
    .dense .hero :global(.inline-icon) {
        display: block;
    }
    :global(.frame.portrait) .hero {
        grid-template-columns: auto minmax(0, 1fr);
        grid-template-areas: 'temp words';
    }
    :global(.frame.portrait) .hero :global(.hero-icon) {
        display: none;
    }
    :global(.frame.portrait) .hero :global(.inline-icon) {
        display: block;
    }
    :global(.frame.portrait) .temp {
        align-self: center;
        font-size: min(calc(var(--frame-display-text) * 240px * var(--font-scale, 1) * var(--frame-scale, 1)), 32cqh);
    }
    :global(.frame.portrait) .words {
        align-self: center;
    }

    .strip {
        height: calc(110px * var(--frame-scale, 1));
    }
    .dot {
        width: calc(16px * var(--frame-scale, 1));
        height: calc(16px * var(--frame-scale, 1));
    }
    .dot.now {
        width: calc(26px * var(--frame-scale, 1));
        height: calc(26px * var(--frame-scale, 1));
    }
</style>
