<script lang="ts">
    import { taskStore } from '#lib/stores/TaskStore.svelte.ts';
    import { itemsThatFit, todayTasks } from '#lib/frame.ts';
    import { formatTime } from '#lib/utils.ts';
    import type { FrameSize } from '#lib/constants/frameTemplates.ts';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `size`: a template slot's size class — L sets the ledger a step larger. Omitted
    // (the free columns) and S are body-sized.
    let { now, lines, size }: { now: Date; lines: number; size?: FrameSize } = $props();

    const large = $derived(size === 'l');
    // A task row is ~1.33 lines (~1.75 in L). When tasks are cut, "+N more" takes the last row.
    const rows = $derived(itemsThatFit(lines, large ? 1.75 : 1.33));
    const tasks = $derived(todayTasks(taskStore.tasks, now));
    const limit = $derived(tasks.length > rows ? Math.max(1, rows - 1) : rows);
</script>

{#if tasks.length === 0}
    <FrameEmpty
        text={taskStore.error ? 'Tasks unavailable' : 'Nothing due today'}
        small={size === undefined || size === 's'}
    />
{:else}
    <!-- A ledger: box, title, then a dotted leader running to the due time. -->
    <ul class={large ? 'space-y-5' : 'space-y-4'}>
        {#each tasks.slice(0, limit) as task (task.id)}
            <li class="flex items-baseline gap-6">
                <span class="shrink-0 translate-y-0.5 border-3 border-(--border) {large ? 'h-9 w-9' : 'h-6.5 w-6.5'}"
                ></span>
                <span class="min-w-0 truncate {large ? 'type-subtitle' : 'type-body'} {task.due_time ? '' : 'flex-1'}"
                    >{task.title}</span
                >
                {#if task.due_time}
                    <span class="min-w-6 flex-1 -translate-y-2 border-b-3 border-dotted border-(--border)"></span>
                    <span class="{large ? 'type-body' : 'type-label'} text-(--text-2)">{formatTime(task.due_time)}</span
                    >
                {/if}
            </li>
        {/each}
    </ul>
    {#if tasks.length > limit}
        <p class="type-label mt-4 text-(--text-2)">+{tasks.length - limit} more</p>
    {/if}
{/if}
