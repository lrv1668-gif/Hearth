<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { calendarStore } from '$lib/stores/CalendarStore.svelte.ts';
    import { weekAhead, type WeekDay } from '$lib/frame';

    let { now, lines }: { now: Date; lines: number } = $props();

    // Entries are counted in text lines so the list fits its share of the column
    // whatever the data; ~15% goes to the gaps between days, and each "+N more"
    // uses a line.
    const PER_DAY = 2;
    const budget = $derived(Math.max(1, Math.floor(lines * 0.85)));

    const rows = $derived.by(() => {
        const out: { day: WeekDay; shown: WeekDay['entries']; more: number }[] = [];
        let left = budget;
        for (const day of weekAhead(taskStore.tasks, calendarStore.items, now)) {
            if (left <= 0) break;
            let take = Math.min(day.entries.length, PER_DAY, left);
            // Leave room for the "+N more" line if anything is cut.
            if (take < day.entries.length && take === left) take = Math.max(1, take - 1);
            const more = day.entries.length - take;
            out.push({ day, shown: day.entries.slice(0, take), more });
            left -= take + (more > 0 ? 1 : 0);
        }
        return out;
    });
</script>

<!--
@component
Week Ahead for the e-paper frame: the next 7 days that have anything on them — calendar
events, Google Tasks and Hearth tasks — one row per day, up to two entries each, trimmed to a line budget that fits the slot.
-->
{#if rows.length === 0}
    <p class="type-body text-(--text-2)">A clear week ahead.</p>
{:else}
    <dl class="grid grid-cols-[auto_minmax(0,1fr)] gap-x-6 gap-y-4">
        {#each rows as { day, shown, more } (day.key)}
            <dt class="type-label pt-1 font-semibold whitespace-nowrap text-(--text-2) uppercase">{day.label}</dt>
            <dd class="min-w-0">
                {#each shown as entry, i (i)}
                    <p class="type-body truncate">
                        {#if entry.time}<span class="type-label mr-2 text-(--text-2)">{entry.time}</span
                            >{/if}{entry.title}
                    </p>
                {/each}
                {#if more > 0}
                    <p class="type-label text-(--text-2)">+{more} more</p>
                {/if}
            </dd>
        {/each}
    </dl>
{/if}
