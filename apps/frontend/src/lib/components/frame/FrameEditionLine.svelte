<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { editionLabel } from '#lib/frame/sky.ts';
    import { localDateKey } from '#lib/utils.ts';

    let { now }: { now: Date } = $props();

    // The edition ("11 AM", or "Evening"/"Early" while the sun is down): it changes only on
    // renders that change the sun path anyway, so it never adds a panel refresh of its own,
    // and a frame stuck on an old edition shows it. The sun times are the sun path's.
    const today = $derived(weatherStore.forecast.find((f) => f.date === localDateKey(now)) ?? null);
    const editionName = $derived(
        editionLabel(today ? new Date(today.sunrise) : null, today ? new Date(today.sunset) : null, now)
    );
</script>

<!--
@component
The masthead's edition line, a small-caps kicker above the weekday naming the edition's
hour (see editionLabel): a frame that has stopped updating otherwise looks just like a
fresh one. Without the sun times it shows nothing.
-->
{#if editionName}
    <p class="type-caption leading-none font-semibold tracking-[0.12em] whitespace-nowrap text-(--text-2) uppercase">
        {editionName} edition
    </p>
{/if}
