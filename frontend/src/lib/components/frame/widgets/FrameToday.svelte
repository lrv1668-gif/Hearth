<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { itemsThatFit, todayTasks } from '$lib/frame';
    import { formatTime } from '$lib/utils';

    let { now, lines }: { now: Date; lines: number } = $props();

    // A task row is ~1.33 lines. When tasks are cut, "+N more" takes the last row.
    const rows = $derived(itemsThatFit(lines, 1.33));
    const tasks = $derived(todayTasks(taskStore.tasks, now));
    const limit = $derived(tasks.length > rows ? Math.max(1, rows - 1) : rows);
</script>

{#if tasks.length === 0}
    <p class="type-body text-(--text-2)">Nothing due today.</p>
{:else}
    <ul class="space-y-4">
        {#each tasks.slice(0, limit) as task (task.id)}
            <li class="flex items-baseline gap-4">
                <span class="h-6 w-6 shrink-0 translate-y-0.5 border-2 border-(--border)"></span>
                <span class="type-body min-w-0 flex-1 truncate">{task.title}</span>
                {#if task.due_time}
                    <span class="type-label text-(--text-2)">{formatTime(task.due_time)}</span>
                {/if}
            </li>
        {/each}
    </ul>
    {#if tasks.length > limit}
        <p class="type-label mt-4 text-(--text-2)">+{tasks.length - limit} more</p>
    {/if}
{/if}
