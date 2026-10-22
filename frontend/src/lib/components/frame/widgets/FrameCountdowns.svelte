<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { getFrameScale, upcomingCountdowns } from '$lib/frame';

    let { now }: { now: Date } = $props();

    // One row of 168px stamps, 24px apart (at the reference panel size; both scale
    // with the frame) — as many as the column fits, up to four. Fixed height, so
    // allocateLines reserves it rather than passing a line budget.
    const scale = getFrameScale();
    let width = $state(0);
    const perRow = $derived(Math.max(1, Math.min(4, Math.floor((width / scale() + 24) / 192))));
    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, now, perRow));
</script>

<!--
@component
Countdowns as stamps: an outlined square per date with the day count in the display
face. The soonest is struck solid in --accent, the frame's one "soonest/now" signal.
-->
<div bind:clientWidth={width}>
    {#if countdowns.length === 0}
        <p class="type-body text-(--text-2)">No countdowns added yet.</p>
    {:else}
        <div class="flex gap-6">
            {#each countdowns as { task, days }, i (task.id)}
                <div class="w-42 shrink-0">
                    <div
                        class="flex h-42 flex-col items-center justify-center gap-1.5 border-3 {i === 0
                            ? 'border-(--accent) bg-(--accent) text-(--accent-fg)'
                            : 'border-(--border)'}"
                    >
                        {#if days === 0}
                            <p class="type-title font-(family-name:--display-family) font-semibold italic">Today</p>
                        {:else}
                            <p
                                class="type-stamp font-(family-name:--display-family) leading-[0.9] font-semibold tracking-[-0.03em]"
                            >
                                {days}
                            </p>
                            <p class="type-caption leading-none font-semibold tracking-[0.16em] uppercase">
                                {days === 1 ? 'day' : 'days'}
                            </p>
                        {/if}
                    </div>
                    <p class="type-label mt-2 truncate">{task.title}</p>
                </div>
            {/each}
        </div>
    {/if}
</div>
