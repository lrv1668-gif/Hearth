<script lang="ts">
    import { weatherStore } from '$lib/stores/WeatherStore.svelte.ts';
    import { weatherIcon } from '$lib/constants/weather';
    import { MINUTE } from '$lib/utils';

    let now = $state(new Date());

    // Only minutes are shown, so tick on each minute boundary. The delay is
    // recomputed every tick (not a fixed setInterval) so the clock re-aligns after
    // a suspend/resume or an NTP clock correction.
    $effect(() => {
        let timeout: ReturnType<typeof setTimeout>;
        const tick = () => {
            now = new Date();
            timeout = setTimeout(tick, MINUTE - (Date.now() % MINUTE));
        };
        timeout = setTimeout(tick, MINUTE - (Date.now() % MINUTE));
        return () => clearTimeout(timeout);
    });

    const time = $derived(now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' }));
    const date = $derived(
        now.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' }).toUpperCase()
    );
</script>

<div class="shrink-0 border-b border-(--border) px-6 pb-2 md:px-8">
    <div class="flex justify-between">
        <div class="flex flex-col gap-2 sm:flex-row-reverse sm:items-end sm:pt-2">
            <p class="type-body tracking-widest text-(--text-2) uppercase">{date}</p>
            <p class="type-display leading-none font-bold text-(--text-1)">{time}</p>
        </div>

        {#if weatherStore.current}
            {@const WeatherIcon = weatherIcon(weatherStore.current.weather_code)}
            <div class="flex items-end gap-2">
                <WeatherIcon class="icon-lg flex-shrink-0 self-center text-(--text-2)" />
                <span class="type-display leading-none font-semibold text-(--text-1)">
                    {Math.round(weatherStore.current.temperature_f)}°
                </span>
                <span class="type-body text-(--text-2)">
                    {weatherStore.current.description}
                </span>
            </div>
        {/if}
    </div>
</div>
