<script lang="ts">
    import { tick } from 'svelte';
    import { almanacStore } from '#lib/stores/AlmanacStore.svelte.ts';
    import { dayOfYear, nearestHour } from '#lib/frame.ts';

    /** `wrap`: allow a second line (portrait, where it shares the row with the sun path). */
    let { now, wrap = false }: { now: Date; wrap?: boolean } = $props();

    const season = $derived(almanacStore.almanac?.season ?? null);
    // "Winter solstice" → "Solstice": the season label just before it already says which.
    // The edition's hour, snapped like the sun path's disc (nearestHour): it changes on the
    // render that moves the sun anyway, so it never adds a panel refresh of its own, and a
    // frame stuck on an old edition shows it.
    const editionHour = $derived(nearestHour(now).toLocaleTimeString('en-US', { hour: 'numeric' }));
    const marker = $derived(season ? season.next_marker.replace(/^\S+\s+(\S)/, (_, c: string) => c.toUpperCase()) : '');

    // On one line, drop the marker ("Solstice in 52 days") when the line can't hold it
    // beside the time. Measured again when the line's width changes (the sun path
    // arrives with the weather), once web fonts load, and when Settings' preview swaps
    // the font or text size in place.
    let line = $state<HTMLElement>();
    let edition = $state<HTMLElement>();
    let markerFits = $state(true);

    async function fit() {
        if (!edition || wrap) return;
        markerFits = true;
        await tick();
        markerFits = edition.scrollWidth <= edition.clientWidth;
    }

    $effect(() => {
        void marker;
        fit();
        let live = true;
        document.fonts.ready.then(() => {
            if (live) fit();
        });
        // The line's own width is set by the masthead, never by its text — no feedback loop
        const resize = new ResizeObserver(() => fit());
        if (line) resize.observe(line);
        const root = line?.closest<HTMLElement>('.frame');
        const observer = new MutationObserver(fit);
        if (root) observer.observe(root, { attributes: true, attributeFilter: ['data-font', 'style'] });
        return () => {
            live = false;
            resize.disconnect();
            observer.disconnect();
        };
    });
</script>

<!--
@component
The masthead's edition line, a small-caps kicker above the weekday: the page numbered
like an edition (day of the year), the season, the days to its next marker, and when
the edition's hour. Without the almanac it shows only the edition number and hour.
On one line the marker drops out (then the season truncates) before the hour does: a
frame that has stopped updating otherwise looks just like a fresh one.
-->
<p
    bind:this={line}
    class="type-caption min-w-0 font-semibold tracking-[0.12em] text-(--text-2) uppercase {wrap
        ? 'leading-tight'
        : 'flex leading-none whitespace-nowrap'}"
>
    <!-- Each segment carries its leading separator and never breaks inside, so a
         wrapped line never ends on a dangling "·". -->
    <span bind:this={edition} class={wrap ? '' : 'min-w-0 truncate'}>
        <span class="whitespace-nowrap">No. {dayOfYear(now)}</span>
        {#if season}
            <span class="whitespace-nowrap">&nbsp;·&nbsp; {season.label}</span>
            {#if markerFits}<span class="whitespace-nowrap"
                    >&nbsp;·&nbsp; {marker} in {season.days_until_marker}
                    {season.days_until_marker === 1 ? 'day' : 'days'}</span
                >{/if}
        {/if}
    </span>
    <span class="whitespace-nowrap {wrap ? '' : 'shrink-0'}">&nbsp;·&nbsp; {editionHour} edition</span>
</p>
