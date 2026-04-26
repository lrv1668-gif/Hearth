<script lang="ts">
  import { onMount } from 'svelte';
  import { Cloud, CloudRain, CloudSnow, Sun, Wind, Zap } from '@lucide/svelte';
  import { fetchCurrentWeather, fetchWeatherForecast, type CurrentWeather, type ForecastDay } from '$lib/api';

  let current = $state<CurrentWeather | null>(null);
  let forecast = $state<ForecastDay[]>([]);

  function weatherIcon(code: number) {
    if (code === 0 || code === 1) return Sun;
    if (code <= 3) return Cloud;
    if (code <= 57) return CloudRain;
    if (code <= 77) return CloudSnow;
    if (code <= 82) return CloudRain;
    if (code <= 86) return CloudSnow;
    return Zap;
  }

  function formatDate(dateStr: string): string {
    const d = new Date(`${dateStr}T00:00`);
    return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  }

  onMount(async () => {
    [current, forecast] = await Promise.all([fetchCurrentWeather(), fetchWeatherForecast()]);
  });
</script>

{#if current}
  <div class="space-y-3">
    <!-- Current conditions -->
    <div class="flex items-center gap-3">
      <svelte:component this={weatherIcon(current.weather_code)} size={20} class="text-[var(--text-2)] flex-shrink-0" />
      <div>
        <span class="text-2xl font-semibold text-[var(--text-1)]">{Math.round(current.temperature_f)}°F</span>
        <span class="text-sm text-[var(--text-3)] ml-2">{current.description}</span>
      </div>
      <div class="ml-auto flex items-center gap-1 text-xs text-[var(--text-4)]">
        <Wind size={12} />
        {Math.round(current.wind_mph)} mph
      </div>
    </div>

    <!-- 5-day forecast -->
    {#if forecast.length > 0}
      <div class="flex gap-1 overflow-x-auto">
        {#each forecast.slice(0, 5) as day (day.date)}
          <div class="flex flex-col items-center gap-1 flex-1 min-w-0 px-1 py-2 rounded-lg bg-[var(--surface)]">
            <span class="text-[10px] text-[var(--text-3)] truncate w-full text-center">
              {new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'short' })}
            </span>
            <svelte:component this={weatherIcon(day.weather_code)} size={14} class="text-[var(--text-3)]" />
            <span class="text-xs text-[var(--text-2)]">{Math.round(day.temp_max_f)}°</span>
            <span class="text-[10px] text-[var(--text-4)]">{Math.round(day.temp_min_f)}°</span>
          </div>
        {/each}
      </div>
    {/if}
  </div>
{/if}
