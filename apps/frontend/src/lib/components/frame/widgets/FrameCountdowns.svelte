<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { getFrameScale, upcomingCountdowns } from '$lib/frame';
    import type { FrameSize } from '$lib/constants/frameTemplates';

    // `size`: the template slot's size class — S is the soonest date alone, with its
    // title and date beside the stamp; M one row of stamps; L up to two rows.
    let { now, size = 'm' }: { now: Date; size?: FrameSize } = $props();

    // 168px stamps, 24px apart (at the reference panel size; both scale with the
    // frame) — as many per row as the column fits, up to four. Fixed height, so
    // allocateLines reserves it rather than passing a line budget.
    const scale = getFrameScale();
    let width = $state(0);
    const perRow = $derived(Math.max(1, Math.min(4, Math.floor((width / scale() + 24) / 192))));
    const limit = $derived(size === 's' ? 1 : size === 'l' ? perRow * 2 : perRow);
    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, now, limit));

    const dateLabel = (due: string) =>
        new Date(`${due.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
        });
</script>

{#snippet stamp(days: number, soonest: boolean)}
    <div
        class="flex h-42 w-42 shrink-0 flex-col items-center justify-center gap-1.5 border-3 {soonest
            ? 'border-(--accent) bg-(--accent) text-(--accent-fg)'
            : 'border-(--border)'}"
    >
        {#if days === 0}
            <p class="type-title font-(family-name:--display-family) font-semibold italic">Today</p>
        {:else}
            <p class="type-stamp font-(family-name:--display-family) leading-[0.9] font-semibold tracking-[-0.03em]">
                {days}
            </p>
            <p class="type-caption leading-none font-semibold tracking-[0.16em] uppercase">
                {days === 1 ? 'day' : 'days'}
            </p>
        {/if}
    </div>
{/snippet}

<!--
@component
Countdowns as stamps: an outlined square per date with the day count in the display
face. The soonest is struck solid in --accent, the frame's one "soonest/now" signal.
-->
<div bind:clientWidth={width}>
    {#if countdowns.length === 0}
        <p class="type-body text-(--text-2)">No countdowns added yet.</p>
    {:else if size === 's'}
        {@const { task, days } = countdowns[0]}
        <div class="flex items-center gap-6">
            {@render stamp(days, true)}
            <div class="min-w-0">
                <p class="type-body line-clamp-2 leading-snug font-semibold">{task.title}</p>
                <p class="type-label mt-1 truncate text-(--text-2)">{dateLabel(task.due_date!)}</p>
            </div>
        </div>
    {:else}
        <div class="flex flex-wrap gap-x-6 gap-y-5">
            {#each countdowns as { task, days }, i (task.id)}
                <div class="w-42 shrink-0">
                    {@render stamp(days, i === 0)}
                    <p class="type-label mt-2 truncate">{task.title}</p>
                </div>
            {/each}
        </div>
    {/if}
</div>
