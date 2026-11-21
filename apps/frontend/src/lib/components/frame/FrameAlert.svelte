<script lang="ts">
    import { weatherStore } from '#lib/stores/WeatherStore.svelte.ts';
    import { alertIcon } from '#lib/constants/weather.ts';

    let { class: className = '' }: { class?: string } = $props();

    // Only the most pressing: a calm screen with one line, not a list of warnings.
    const alert = $derived(weatherStore.alerts[0] ?? null);
</script>

<!--
@component
The frame's weather alert — the Weather service's most pressing one (a storm, frost
tonight, heat…), as one line in pure ink with its icon, or nothing on a calm day.
Shown in the Ambient plate, at the top of Daily's forecast band and on Night.
-->
{#if alert}
    {@const Icon = alertIcon(alert.kind)}
    <p class="flex items-center gap-3 font-semibold text-(--text-1) {className}">
        <Icon class="icon-sm shrink-0" />
        <span class="min-w-0">{alert.text}</span>
    </p>
{/if}
