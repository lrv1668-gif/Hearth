<script lang="ts">
    import { Leaf, SunMedium, Sunrise, Sunset, Wind } from '@lucide/svelte';
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '#lib/constants/weather.ts';
    import { localDateKey } from '#lib/utils.ts';

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
            <WeatherIcon class="icon-lg flex-shrink-0 text-(--accent)" />
            <div>
                <span class="type-display font-semibold text-(--text-1)">
                    {Math.round(weatherStore.current.temperature_f)}°F
                </span>
                <span class="type-body ml-2 text-(--text-2)">{weatherStore.current.description}</span>
            </div>
        </div>

        {#if weatherStore.forecast.length > 0}
            <div class="flex gap-1 overflow-x-auto">
                {#each weatherStore.forecast.slice(0, 5) as day (day.date)}
                    {@const ForecastIcon = weatherIcon(day.weather_code)}
                    {@const isToday = day.date === localDateKey(new Date())}
                    <div
                        class="flex min-w-0 flex-1 flex-col items-center gap-1 rounded-lg px-1 py-2 {isToday
                            ? 'bg-(--surface-hi)'
                            : 'bg-(--surface)'}"
                    >
                        <span
                            class="type-label w-full truncate text-center {isToday
                                ? 'font-semibold text-(--accent)'
                                : 'text-(--text-1)'}"
                        >
                            {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', {
                                weekday: 'short',
                            })}
                        </span>
                        <ForecastIcon class="icon-md text-(--text-2)" />
                        <div class="type-label flex items-center gap-1">
                            <span class="text-(--text-1)">{Math.round(day.temp_max_f)}°</span>
                            <span class="text-(--text-2)">{Math.round(day.temp_min_f)}°</span>
                        </div>
                    </div>
                {/each}
            </div>

            <div class="type-label flex items-center gap-2 text-(--text-2)">
                {#if weatherStore.forecast[0]?.sunrise}
                    <span class="flex items-center gap-1 border-r border-r-(--border) pr-2">
                        <Sunrise class="icon-sm text-(--text-3)" />{formatSunTime(weatherStore.forecast[0].sunrise)}
                    </span>
                    <span class="flex items-center gap-1 border-r border-r-(--border) pr-2">
                        <Sunset class="icon-sm text-(--text-3)" />{formatSunTime(weatherStore.forecast[0].sunset)}
                    </span>
                {/if}
                {#if weatherStore.current.uv_index != null}
                    <span
                        class="flex items-center gap-1 border-r border-r-(--border) pr-2"
                        title={weatherStore.current.uv_category ?? undefined}
                    >
                        <SunMedium class="icon-sm text-(--text-3)" />UV {Math.round(weatherStore.current.uv_index)}
                    </span>
                {/if}
                {#if weatherStore.current.us_aqi != null}
                    <span
                        class="flex items-center gap-1 border-r border-r-(--border) pr-2"
                        title={weatherStore.current.aqi_category ?? undefined}
                    >
                        <Leaf class="icon-sm text-(--text-3)" />AQI {weatherStore.current.us_aqi}
                    </span>
                {/if}
                <span class="flex items-center gap-1">
                    <Wind class="icon-sm text-(--text-3)" />{Math.round(weatherStore.current.wind_mph)} mph
                </span>
            </div>
        {/if}
    </div>
{/if}
