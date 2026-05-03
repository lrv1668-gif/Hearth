<script lang="ts">
    import type { Task } from '$lib/api';

    interface Props {
        tasks: Task[];
        onEdit: (task: Task) => void;
    }

    let { tasks, onEdit }: Props = $props();

    function daysUntil(dueDate: string): number {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const due = new Date(`${dueDate.slice(0, 10)}T00:00`);
        return Math.round((due.getTime() - today.getTime()) / 86_400_000);
    }

    function formatDate(dueDate: string): string {
        return new Date(`${dueDate.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
        });
    }

    const upcoming = $derived(
        tasks
            .filter((t) => t.is_countdown && t.due_date && !t.done)
            .map((t) => ({ ...t, days: daysUntil(t.due_date!) }))
            .filter((t) => t.days >= 0)
            .sort((a, b) => a.days - b.days)
            .slice(0, 5)
    );
</script>

{#if upcoming.length === 0}
    <p class="type-label text-[var(--text-2)]">No countdowns added yet.</p>
{:else}
    <ul class="space-y-3">
        {#each upcoming as item (item.id)}
            <li>
                <button
                    onclick={() => onEdit(item)}
                    class="w-full text-left flex items-start gap-3 group hover:opacity-80 transition-opacity"
                >
                    <div class="flex flex-col items-end flex-shrink-0 w-12">
                        {#if item.days === 0}
                            <span class="type-title font-bold text-[var(--accent)] leading-none">!</span>
                        {:else}
                            <span class="type-title font-bold text-[var(--text-1)] leading-none tabular-nums">
                                {item.days}
                            </span>
                            <span class="type-caption leading-none text-[var(--text-2)]">
                                {item.days === 1 ? 'day' : 'days'}
                            </span>
                        {/if}
                    </div>

                    <div class="min-w-0 flex-1">
                        {#if item.days === 0}
                            <p class="type-label font-semibold uppercase tracking-widest text-[var(--accent)]">Today</p>
                        {/if}

                        <p class="type-body text-[var(--text-1)] truncate leading-tight">{item.title}</p>

                        {#if item.days !== 0}
                            <p class="type-label text-[var(--text-2)]">{formatDate(item.due_date!)}</p>
                        {/if}
                    </div>
                </button>
            </li>
        {/each}
    </ul>
{/if}
