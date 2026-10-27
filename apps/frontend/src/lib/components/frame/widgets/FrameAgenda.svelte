<script lang="ts">
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import { fitAgenda } from '$lib/frame';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `lines`: the slot's line budget. L sets the headings and titles a step larger.
    let { now, lines, large = false }: { now: Date; lines: number; large?: boolean } = $props();

    // A slot much wider than tall flows the agenda into two columns, doubling the room.
    let w = $state(0);
    let h = $state(0);
    const twoColumns = $derived(h > 0 && w / h > 1.8);
    // In body lines: a day heading with the space above it, and an event row.
    const costs = $derived(large ? { headingLines: 2.1, rowLines: 1.75 } : { headingLines: 1.75, rowLines: 1.45 });
    const days = $derived(fitAgenda(calendarStore.items, now, lines * (twoColumns ? 2 : 1), costs));

    function eventTime(start: string, allDay: boolean): string {
        if (allDay) return 'All day';
        return new Date(start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }
</script>

<!--
@component
Coming Up for a template's M or L slot: the next events as an agenda — each day under a
heading in the display face (today's struck in --accent), times in a margin, titles
beside. A wide slot flows it into two columns. S slots and the free columns use the
rail (FrameComingUp).
-->
<div bind:clientWidth={w} bind:clientHeight={h} class="h-full">
    {#if days.length === 0}
        <FrameEmpty text="No upcoming events" />
    {:else}
        <div class="h-full gap-16 {twoColumns ? 'columns-2' : ''}" style="column-fill: auto">
            {#each days as day (day.key)}
                <section class="mt-6 break-inside-avoid first:mt-0">
                    <h3
                        class="mb-3 flex items-center gap-4 border-b-3 border-(--border) pb-2 font-(family-name:--display-family) leading-tight font-medium {large
                            ? 'type-title'
                            : 'type-subtitle'} {day.label === 'Today' ? 'text-(--accent)' : ''}"
                    >
                        {day.label}
                    </h3>
                    <ul class="space-y-2">
                        {#each day.events as event (event.id)}
                            <li class="grid grid-cols-[--spacing(40)_minmax(0,1fr)] items-baseline gap-6">
                                <span class="type-label text-right text-(--text-2)"
                                    >{eventTime(event.start!, event.is_all_day)}</span
                                >
                                <span class="truncate {large ? 'type-subtitle' : 'type-body'}">{event.title}</span>
                            </li>
                        {/each}
                    </ul>
                </section>
            {/each}
        </div>
    {/if}
</div>
