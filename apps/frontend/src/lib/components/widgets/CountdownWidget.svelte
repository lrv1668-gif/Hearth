<script lang="ts">
    import type { Task } from '$lib/api';
    import { upcomingCountdowns } from '$lib/frame';

    interface Props {
        tasks: Task[];
        onEdit: (task: Task) => void;
        limit?: number;
    }

    let { tasks, onEdit, limit = 5 }: Props = $props();

    function formatDate(dueDate: string): string {
        return new Date(`${dueDate.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
        });
    }

    const upcoming = $derived(upcomingCountdowns(tasks, new Date(), limit));
</script>

{#if upcoming.length === 0}
    <p class="type-label text-(--text-2)">No countdowns added yet.</p>
{:else}
    <ul class="space-y-3">
        {#each upcoming as { task: item, days } (item.id)}
            <li class="flex">
                <button
                    onclick={() => onEdit(item)}
                    class="group flex items-start gap-3 text-right transition-opacity hover:opacity-80"
                >
                    <!-- Width tracks the number's font size so 3-digit counts fit at any scale (incl. /frame). -->
                    <div class="flex w-[calc(var(--font-title)*1.75)] flex-shrink-0 flex-col items-end">
                        {#if days === 0}
                            <span class="type-title leading-none font-bold text-(--accent)">!</span>
                        {:else}
                            <span class="type-title leading-none font-bold text-(--text-1) tabular-nums">
                                {days}
                            </span>
                            <span class="type-caption leading-none text-(--text-2)">
                                {days === 1 ? 'day' : 'days'}
                            </span>
                        {/if}
                    </div>

                    <div class="min-w-0 flex-1">
                        {#if days === 0}
                            <p class="type-label font-semibold tracking-widest text-(--accent) uppercase">Today</p>
                        {/if}

                        <p class="type-body truncate leading-tight text-(--text-1)">{item.title}</p>

                        {#if days !== 0}
                            <p class="type-label text-left text-(--text-2)">{formatDate(item.due_date!)}</p>
                        {/if}
                    </div>
                </button>
            </li>
        {/each}
    </ul>
{/if}
