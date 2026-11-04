<script lang="ts">
    import { timetableStore } from '#lib/stores/TimetableStore.svelte.ts';
    import { household } from '#lib/stores/HouseholdStore.svelte.ts';
    import { clockTime, lineKey, timetableStops, windowLabel } from '#lib/timetable.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `lines`: the line budget; `size`: the template slot's size class, omitted in the free columns.
    let { lines, size }: { lines: number; size?: FrameSize } = $props();
    const small = $derived(size === undefined || size === 's');

    // M and L set the rows a step or two larger, so a big slot reads across the room
    // rather than holding a small list.
    const type = $derived(
        size === 'l'
            ? { route: 'type-body', direction: 'type-title', times: 'type-subtitle' }
            : size === 'm'
              ? { route: 'type-body', direction: 'type-subtitle', times: 'type-body' }
              : { route: 'type-label', direction: '', times: 'type-label' }
    );

    // In body lines: the caption, a stop's heading, and a line row (direction plus one
    // row of times in S; up to two rows of larger times in M and L).
    const CAPTION_LINES = 1.4;
    const HEADING_LINES = 1.5;
    const rowLines = $derived(size === 'l' ? 5.2 : size === 'm' ? 4.1 : 2.5);

    const stops = $derived(timetableStops(household.trainStops, timetableStore.timetables));
    const block = $derived(timetableStore.timetables[0]);

    // Fill the budget stop by stop; a quarter keeps to the first stop. Headings show only
    // when there's more than one stop to tell apart.
    const shown = $derived.by(() => {
        const candidates = small ? stops.slice(0, 1) : stops;
        const headed = candidates.length > 1;
        let left = lines - CAPTION_LINES;
        const out: typeof stops = [];
        for (const stop of candidates) {
            const room = Math.floor((left - (headed ? HEADING_LINES : 0)) / rowLines);
            if (room < 1) break;
            const kept = stop.lines.slice(0, room);
            out.push({ ...stop, lines: kept });
            left -= (headed ? HEADING_LINES : 0) + kept.length * rowLines;
        }
        // Always show something: the first line of the first stop.
        if (out.length === 0 && candidates[0]) out.push({ ...candidates[0], lines: candidates[0].lines.slice(0, 1) });
        return { stops: out, headed };
    });
</script>

<!--
@component
The e-paper frame's Scheduled Departures: a printed timetable for the household's stops —
each line and direction with its timetabled times from the top of this hour to the end of
the next. The Trains service picks it once per clock hour, so the panel redraws for it at
most hourly; times that have passed stay up until then, as on a posted timetable. Live
predictions stay on the dashboard's Trains board.
-->
{#if timetableStore.error}
    <FrameEmpty text="Timetable unavailable" {small} />
{:else if household.trainStops.length === 0}
    <FrameEmpty text="Add a stop in Settings to show its timetable" {small} />
{:else if shown.stops.length === 0}
    <FrameEmpty text="No scheduled departures in the next two hours" {small} />
{:else}
    <div class="type-body flex flex-col">
        {#if block}
            <p class="type-label mb-[0.5em] font-semibold tracking-[0.08em] text-(--text-2) uppercase">
                Scheduled · {windowLabel(block.window_start, block.window_end)}
            </p>
        {/if}
        {#each shown.stops as stop (stop.stopKey)}
            <section class="mt-[0.5em] first:mt-0">
                {#if shown.headed}
                    <h3 class="type-label mb-[0.3em] truncate font-semibold text-(--text-1)">{stop.label}</h3>
                {/if}
                <ul class="flex flex-col {small ? 'gap-[0.5em]' : 'gap-[1em]'}">
                    {#each stop.lines as line (lineKey(line))}
                        <li class="flex min-w-0 gap-[0.6em]">
                            <!-- The route on a solid --block tab, like the Sports card's teams. -->
                            <span
                                class="{type.route} flex h-[1.6em] min-w-[2.6em] shrink-0 items-center justify-center bg-(--block) px-[0.3em] font-bold tracking-wide text-(--on-block)"
                            >
                                {line.route_short_name || line.mode}
                            </span>
                            <div class="min-w-0 flex-1">
                                <p class="truncate font-medium text-(--text-1) {type.direction}">
                                    {line.headsign ? `→ ${line.headsign}` : line.mode}
                                </p>
                                <p
                                    class="{type.times} text-(--text-1) tabular-nums {small
                                        ? 'truncate'
                                        : 'line-clamp-2'} [word-spacing:0.5em]"
                                >
                                    {line.departures.map(clockTime).join(' ')}
                                </p>
                            </div>
                        </li>
                    {/each}
                </ul>
            </section>
        {/each}
    </div>
{/if}
