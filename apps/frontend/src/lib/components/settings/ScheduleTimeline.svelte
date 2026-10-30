<script lang="ts">
    import type { ModeTimeline } from '#lib/api.ts';
    import { frameModeLabel } from '#lib/constants/frameModes.ts';
    import type { QuietHours } from '#lib/frame.ts';

    let { day, quiet }: { day: ModeTimeline; quiet: QuietHours | null } = $props();

    const DAY_MINUTES = 24 * 60;
    const minutes = (hhmm: string) => {
        const [h, m] = hhmm.split(':').map(Number);
        return h * 60 + m;
    };
    const pct = (m: number) => `${(m / DAY_MINUTES) * 100}%`;
    const label = (modes: string[]) => modes.map(frameModeLabel).join(' / ');

    // Quiet hours as one or two stretches of the day (a window past midnight splits).
    const quietSpans = $derived.by(() => {
        if (!quiet) return [];
        const [start, end] = [minutes(quiet.start), minutes(quiet.end)];
        return start < end
            ? [[start, end]]
            : [
                  [start, DAY_MINUTES],
                  [0, end],
              ].filter(([a, b]) => b > a);
    });

    const weekday = $derived(
        new Date(`${day.date}T00:00`).toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' })
    );
</script>

<!--
@component
Today's saved schedule as one bar across the day: each stretch labelled with the mode
(or rotation) showing, rules raised above the default mode, and quiet hours hatched
over the top — so the order of the rules, overlaps and gaps read at a glance. The
spans come from Display (GET /display/mode/day), which resolves them as the frame will.
-->
<div>
    <p class="type-label mb-2 text-(--text-2)">Today, {weekday}</p>
    <div class="relative flex h-11 overflow-hidden rounded-lg border-2 border-(--border)">
        {#each day.spans as span (span.start)}
            {@const text = label(span.modes)}
            <div
                class="flex min-w-0 items-center justify-center border-r border-(--border) last:border-r-0
                    {span.rule === null ? 'bg-(--bg)' : 'bg-(--surface-hi)'}"
                style:width={pct(minutes(span.end) - minutes(span.start))}
                title="{span.start}–{span.end}: {text}{span.rule === null ? ' (default)' : ` (time ${span.rule + 1})`}"
            >
                <span
                    class="type-caption truncate px-1 {span.rule === null
                        ? 'text-(--text-2)'
                        : 'font-semibold text-(--text-1)'}">{text}</span
                >
            </div>
        {/each}
        {#each quietSpans as [a, b] (a)}
            <div
                class="quiet pointer-events-none absolute inset-y-0"
                style:left={pct(a)}
                style:width={pct(b - a)}
                aria-hidden="true"
            ></div>
        {/each}
    </div>
    <div class="type-caption mt-1 flex justify-between text-(--text-2) tabular-nums" aria-hidden="true">
        <span>12 AM</span><span>6 AM</span><span>Noon</span><span>6 PM</span><span>12 AM</span>
    </div>
    {#if quietSpans.length > 0}
        <p class="type-caption mt-1 text-(--text-2)">Hatched: quiet hours, when the panel holds its picture.</p>
    {/if}
</div>

<style>
    .quiet {
        background-image: repeating-linear-gradient(135deg, var(--text-3) 0 1.5px, transparent 1.5px 7px);
        opacity: 0.6;
    }
</style>
