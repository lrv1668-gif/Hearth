<script lang="ts">
    import { Cloud, CloudRain, CloudSnow, Leaf, Sun, SunMedium, Sunrise, Sunset, Wind, Zap } from '@lucide/svelte';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';

    function weatherIcon(code: number) {
        if (code === 0 || code === 1) return Sun;
        if (code <= 3) return Cloud;
        if (code <= 57) return CloudRain;
        if (code <= 77) return CloudSnow;
        if (code <= 82) return CloudRain;
        if (code <= 86) return CloudSnow;
        return Zap;
    }

    function formatSunTime(iso: string): string {
        return new Date(iso).toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit',
            hour12: true,
        });
    }
</script>

{#if weatherStore.current}
    {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
    <div class="space-y-3">
        <div class="flex items-center gap-3">
            <WeatherIcon class="icon-lg flex-shrink-0 text-[var(--text-1)]" />
            <div>
                <span class="type-display font-semibold text-[var(--text-1)]">
                    {Math.round(weatherStore.current.temperature_f)}°F
                </span>
                <span class="type-body ml-2 text-[var(--text-2)]">{weatherStore.current.description}</span>
            </div>
        </div>

        {#if weatherStore.forecast.length > 0}
            <div class="flex gap-1 overflow-x-auto">
                {#each weatherStore.forecast.slice(0, 5) as day (day.date)}
                    {@const ForecastIcon = weatherIcon(day.weather_code)}
                    <div
                        class="flex min-w-0 flex-1 flex-col items-center gap-1 rounded-lg bg-[var(--surface)] px-1 py-2"
                    >
                        <span class="type-label w-full truncate text-center text-[var(--text-1)]">
                            {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', {
                                weekday: 'short',
                            })}
                        </span>
                        <ForecastIcon class="icon-md text-[var(--text-1)]" />
                        <div class="type-label flex items-center gap-1">
                            <span class="text-[var(--text-1)]">{Math.round(day.temp_max_f)}°</span>
                            <span class="text-[var(--text-2)]">{Math.round(day.temp_min_f)}°</span>
                        </div>
                    </div>
                {/each}
            </div>

            <div class="type-label flex items-center gap-2 text-[var(--text-2)]">
                {#if weatherStore.forecast[0]?.sunrise}
                    <span class="flex items-center gap-1 border-r border-r-[var(--border)] pr-2">
                        <Sunrise class="icon-sm" />{formatSunTime(weatherStore.forecast[0].sunrise)}
                    </span>
                    <span class="flex items-center gap-1 border-r border-r-[var(--border)] pr-2">
                        <Sunset class="icon-sm" />{formatSunTime(weatherStore.forecast[0].sunset)}
                    </span>
                {/if}
                {#if weatherStore.current.uv_index != null}
                    <span class="flex items-center gap-1 border-r border-r-[var(--border)] pr-2">
                        <SunMedium class="icon-sm" />UV {Math.round(weatherStore.current.uv_index)}
                    </span>
                {/if}
                {#if weatherStore.current.us_aqi != null}
                    <span class="flex items-center gap-1 border-r border-r-[var(--border)] pr-2">
                        <Leaf class="icon-sm" />AQI {weatherStore.current.us_aqi}
                    </span>
                {/if}
                <span class="flex items-center gap-1">
                    <Wind class="icon-sm" />{Math.round(weatherStore.current.wind_mph)} mph
                </span>
            </div>
        {/if}
    </div>
{/if}
