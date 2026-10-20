<script lang="ts">
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import { itemsThatFit, nextEvents, relativeDayLabel } from '$lib/frame';

    let { now, lines }: { now: Date; lines: number } = $props();

    // An event (title + day/time) is ~2.15 lines with its gap; the last one needs no gap.
    const events = $derived(nextEvents(calendarStore.items, now, Math.min(6, itemsThatFit(lines + 0.35, 2.15))));

    function eventTime(start: string, allDay: boolean): string {
        if (allDay) return 'All day';
        return new Date(start).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }
</script>

{#if events.length === 0}
    <p class="type-body text-(--text-2)">No upcoming events.</p>
{:else}
    <ul class="space-y-4">
        {#each events as event (event.id)}
            <li class="min-w-0">
                <p class="type-body truncate">{event.title}</p>
                <p class="type-label text-(--text-2)">
                    {relativeDayLabel(event, now)} · {eventTime(event.start!, event.is_all_day)}
                </p>
            </li>
        {/each}
    </ul>
{/if}
