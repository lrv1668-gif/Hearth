<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { weatherSentence } from '#lib/frame.ts';
    import { localDateKey } from '#lib/utils.ts';
    import FrameDateBlock from './FrameDateBlock.svelte';

    // `note`: one quiet line saying why the mode fell back to this. `weather`: off in the
    // 15-minute modes, where a temperature that moves would make each render a full
    // panel refresh; Ambient and Photo redraw on the photo interval anyway.
    let { now, note = '', weather = true }: { now: Date; note?: string; weather?: boolean } = $props();

    const weekday = $derived(now.toLocaleDateString('en-US', { weekday: 'long' }));
    const today = $derived(weatherStore.forecast.find((f) => f.date === localDateKey(now)) ?? null);
</script>

<!--
@component
The fallback for a mode with nothing of its own to show — Ambient or Photo without a
photo, Countdown without a countdown: the date block and weekday as a poster, the
weather in one sentence (photo modes only), and a small line saying why. Something worth hanging on the
wall for hours, rather than an error message.
-->
<div class="absolute inset-0 flex flex-col items-center justify-center gap-12 px-24 text-center">
    <div class="flex items-end gap-10">
        <FrameDateBlock {now} />
        <p
            class="type-weekday pb-[0.08em] font-(family-name:--display-family) leading-none font-medium tracking-[-0.02em] whitespace-nowrap italic"
        >
            {weekday}
        </p>
    </div>
    {#if weather && weatherStore.current}
        <p class="type-subtitle font-(family-name:--display-family) text-(--text-2) italic">
            {weatherSentence(weatherStore.current, today)}
        </p>
    {/if}
    {#if note}
        <p class="type-label font-semibold tracking-[0.16em] text-(--text-2) uppercase">{note}</p>
    {/if}
</div>
