<script lang="ts">
    import { calendarStore } from '#lib/stores/CalendarStore.svelte.ts';
    import { eventDateKey, localDateKey } from '#lib/utils.ts';
    import { getFrameScale, itemsThatFit, nextEvents, railDayLabel } from '#lib/frame.ts';

    let { now, lines }: { now: Date; lines: number } = $props();

    // Below ~640px at the reference panel size (a portrait column) the day/time margin
    // would squeeze the titles, so the rail drops it and puts "TOMORROW · 7:00 PM"
    // above each title instead.
    const scale = getFrameScale();
    let width = $state(0);
    const narrow = $derived(width > 0 && width < 640 * scale());
    // A rail row is ~1.95 lines with the margin (day + time beside the title, padding
    // below), ~2.3 narrow (when-line above the title).
    const events = $derived(
        nextEvents(calendarStore.items, now, Math.min(6, itemsThatFit(lines, narrow ? 2.3 : 1.95)))
    );
    const todayKey = $derived(localDateKey(now));

    function eventTime(start: string, allDay: boolean): string {
        if (allDay) return 'All day';
        return new Date(start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }
</script>

<!--
@component
Coming Up as a rail: the day and time in a right-aligned margin (or above the title
in a narrow column), a 3px vertical line with a pin per event (solid --block for
today, open otherwise), and the title beside.
-->
<div bind:clientWidth={width}>
    {#if events.length === 0}
        <p class="type-body text-(--text-2)">No upcoming events.</p>
    {:else}
        <ul
            class="grid gap-x-6 {narrow
                ? 'grid-cols-[--spacing(6)_minmax(0,1fr)]'
                : 'grid-cols-[--spacing(46)_--spacing(6)_minmax(0,1fr)]'}"
        >
            {#each events as event, i (event.id)}
                <li class="contents">
                    {#if !narrow}
                        <p class="pt-1 pb-6 text-right text-(--text-2)">
                            <span class="type-label block leading-tight font-semibold tracking-[0.16em] uppercase">
                                {railDayLabel(event, now)}
                            </span>
                            <span class="type-label block leading-tight"
                                >{eventTime(event.start!, event.is_all_day)}</span
                            >
                        </p>
                    {/if}
                    <!-- Each pin draws the rail down to the next pin, so the line spans first to last. -->
                    <span class="relative">
                        {#if i < events.length - 1}
                            <span class="absolute top-6 -bottom-6 left-[calc(--spacing(3)-1.5px)] w-[3px] bg-(--border)"
                            ></span>
                        {/if}
                        <span
                            class="relative mt-3 block h-6 w-6 rounded-full border-3 {eventDateKey(event) <= todayKey
                                ? 'border-(--block) bg-(--block)'
                                : 'border-(--border) bg-(--bg)'}"
                        ></span>
                    </span>
                    <div class="min-w-0 pb-6">
                        {#if narrow}
                            <p class="type-label truncate leading-tight text-(--text-2)">
                                <span class="font-semibold tracking-[0.16em] uppercase">{railDayLabel(event, now)}</span
                                >
                                · {eventTime(event.start!, event.is_all_day)}
                            </p>
                        {/if}
                        <p class="type-body truncate">{event.title}</p>
                    </div>
                </li>
            {/each}
        </ul>
    {/if}
</div>
