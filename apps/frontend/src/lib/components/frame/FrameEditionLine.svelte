<script lang="ts">
    import { almanacStore } from '#lib/stores/AlmanacStore.svelte.ts';
    import { dayOfYear } from '#lib/frame.ts';

    /** `wrap`: allow a second line (portrait, where it shares the row with the sun path). */
    let { now, wrap = false }: { now: Date; wrap?: boolean } = $props();

    const season = $derived(almanacStore.almanac?.season ?? null);
    // "Winter solstice" → "Solstice": the season label just before it already says which.
    const marker = $derived(season ? season.next_marker.replace(/^\S+\s+(\S)/, (_, c: string) => c.toUpperCase()) : '');
</script>

<!--
@component
The masthead's edition line, a small-caps kicker above the weekday: the page numbered
like an edition (day of the year), the season, and the days to its next marker.
Without the almanac it shows only the edition number.
-->
<p
    class="type-caption min-w-0 font-semibold tracking-[0.12em] text-(--text-2) uppercase {wrap
        ? 'leading-tight'
        : 'truncate leading-none whitespace-nowrap'}"
>
    <!-- Each segment carries its leading separator and never breaks inside, so a
         wrapped line never ends on a dangling "·". -->
    <span class="whitespace-nowrap">No. {dayOfYear(now)}</span>
    {#if season}
        <span class="whitespace-nowrap">&nbsp;·&nbsp; {season.label}</span>
        <span class="whitespace-nowrap"
            >&nbsp;·&nbsp; {marker} in {season.days_until_marker}
            {season.days_until_marker === 1 ? 'day' : 'days'}</span
        >
    {/if}
</p>
