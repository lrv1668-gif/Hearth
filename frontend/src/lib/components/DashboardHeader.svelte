<script lang="ts">
    import { Cloud, CloudRain, CloudSnow, Sun, Zap } from '@lucide/svelte';
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';

    let now = $state(new Date());

    $effect(() => {
        const id = setInterval(() => (now = new Date()), 1000);
        return () => clearInterval(id);
    });

    const time = $derived(now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }));
    const date = $derived(
        now.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' }).toUpperCase()
    );

    function weatherIcon(code: number) {
        if (code === 0 || code === 1) return Sun;
        if (code <= 3) return Cloud;
        if (code <= 57) return CloudRain;
        if (code <= 77) return CloudSnow;
        if (code <= 82) return CloudRain;
        if (code <= 86) return CloudSnow;
        return Zap;
    }
</script>

<div class="shrink-0 border-b border-[var(--border)] px-6 pb-2 md:px-8">
    <div class="flex justify-between">
        <div class="flex flex-col gap-2 sm:flex-row-reverse sm:items-end sm:pt-2">
            <p class="type-body uppercase tracking-widest text-[var(--text-2)]">{date}</p>
            <p class="type-display font-bold leading-none text-[var(--text-1)]">{time}</p>
        </div>

        {#if weatherStore.current}
            {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
            <div class="flex items-end gap-2">
                <WeatherIcon class="icon-lg flex-shrink-0 self-center text-[var(--text-2)]" />
                <span class="type-display font-semibold leading-none text-[var(--text-1)]">
                    {Math.round(weatherStore.current.temperature_f)}°
                </span>
                <span class="type-body text-[var(--text-2)]">
                    {weatherStore.current.description}
                </span>
            </div>
        {/if}
    </div>
</div>
