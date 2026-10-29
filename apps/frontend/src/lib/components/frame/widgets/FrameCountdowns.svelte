<script lang="ts">
    import { taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { getFrameScale, upcomingCountdowns } from '$lib/frame';
    import type { FrameSize } from '$lib/constants/frameTemplates';
    import FitText from '../FitText.svelte';
    import FrameEmpty from '../FrameEmpty.svelte';

    // `size`: the template slot's size class. S is the soonest date alone, its title and
    // date beside the stamp. M and L make the soonest a poster — its day count fitted to
    // the slot, as Countdown mode does for the panel — with the rest beside it (wide
    // slot) or below (tall): a short list in M, stamps in L. Omitted (the free columns)
    // is one row of stamps.
    let { now, size }: { now: Date; size?: FrameSize } = $props();

    // 168px stamps, 24px apart (at the reference panel size; both scale with the
    // frame) — as many per row as the column fits, up to four. Fixed height, so
    // allocateLines reserves it rather than passing a line budget.
    const scale = getFrameScale();
    let width = $state(0);
    const perRow = $derived(Math.max(1, Math.min(4, Math.floor((width / scale() + 24) / 192))));
    const limit = $derived(size === 's' ? 1 : size === 'm' ? 4 : size === 'l' ? 5 : perRow);
    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, now, limit));

    const dateLabel = (due: string) =>
        new Date(`${due.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
        });
    const daysLabel = (days: number) => (days === 0 ? 'Today' : days === 1 ? '1 day' : `${days} days`);
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
face. The soonest is struck solid in --accent, the frame's one "soonest/now" signal —
in M and L slots it becomes the poster, its count in --accent.
-->
{#if countdowns.length === 0}
    <FrameEmpty text="No countdowns added yet" small={size === undefined || size === 's'} />
{:else if size === 's'}
    {@const { task, days } = countdowns[0]}
    <div class="flex items-center gap-6">
        {@render stamp(days, true)}
        <div class="min-w-0">
            <p class="type-body line-clamp-2 leading-snug font-semibold">{task.title}</p>
            <p class="type-label mt-1 truncate text-(--text-2)">{dateLabel(task.due_date!)}</p>
        </div>
    </div>
{:else if size}
    {@const [{ task, days }, ...rest] = countdowns}
    <div class="poster flex h-full gap-12">
        <!-- The poster is one group, centred in the slot: the count's box grows to fill
             it but no taller than the slot is wide, so in a tall slot the title stays
             under the number rather than sinking to the bottom. Alone, it's centred. -->
        <div
            class="hero flex min-h-0 min-w-0 flex-1 flex-col justify-center {rest.length
                ? ''
                : 'items-center text-center'}"
        >
            <div class="count min-h-0 w-full flex-1">
                <FitText min={80} max={size === 'l' ? 400 : 260} key={days} class={rest.length ? '' : 'text-center'}>
                    <p
                        class="font-(family-name:--display-family) leading-[0.85] font-semibold tracking-[-0.04em] text-(--accent) {days ===
                        0
                            ? 'italic'
                            : ''}"
                    >
                        {days === 0 ? 'Today' : days}
                    </p>
                    {#if days > 0}
                        <p class="mt-[0.06em] text-[0.14em] font-semibold tracking-[0.24em] text-(--text-2) uppercase">
                            {days === 1 ? 'day to go' : 'days to go'}
                        </p>
                    {/if}
                </FitText>
            </div>
            <div class="my-6 h-3 w-28 shrink-0 bg-(--block)"></div>
            <p
                class="max-w-full shrink-0 truncate font-(family-name:--display-family) leading-tight font-medium {size ===
                'l'
                    ? 'type-hero'
                    : 'type-title'}"
            >
                {task.title}
            </p>
            <p class="type-body mt-2 max-w-full shrink-0 truncate text-(--text-2)">{dateLabel(task.due_date!)}</p>
        </div>
        {#if rest.length > 0}
            <div class="rest flex min-w-0 shrink-0 flex-col justify-center">
                <p class="type-label mb-5 font-semibold tracking-[0.16em] text-(--text-2) uppercase">Then</p>
                {#if size === 'l'}
                    <div class="flex flex-wrap gap-x-6 gap-y-5">
                        {#each rest as c (c.task.id)}
                            <div class="w-42 shrink-0">
                                {@render stamp(c.days, false)}
                                <p class="type-label mt-2 truncate">{c.task.title}</p>
                            </div>
                        {/each}
                    </div>
                {:else}
                    <ul class="space-y-4">
                        {#each rest as c (c.task.id)}
                            <li class="flex items-baseline gap-6">
                                <span class="type-body min-w-0 truncate">{c.task.title}</span>
                                <span class="min-w-6 flex-1 -translate-y-2 border-b-3 border-dotted border-(--border)"
                                ></span>
                                <span class="type-label shrink-0 text-(--text-2)">{daysLabel(c.days)}</span>
                            </li>
                        {/each}
                    </ul>
                {/if}
            </div>
        {/if}
    </div>
{:else}
    <div bind:clientWidth={width} class="flex flex-wrap gap-x-6 gap-y-5">
        {#each countdowns as { task, days }, i (task.id)}
            <div class="w-42 shrink-0">
                {@render stamp(days, i === 0)}
                <p class="type-label mt-2 truncate">{task.title}</p>
            </div>
        {/each}
    </div>
{/if}

<style>
    /* The slot body is a size container (FrameDaily). Wide: the rest sit beside the
       poster in a column ~40% wide (L's stamps: two across). Tall: they go below it. */
    .rest {
        width: 40cqw;
    }
    .count {
        max-height: 70cqw;
    }
    @container (aspect-ratio < 1.3) {
        .poster {
            flex-direction: column;
        }
        .rest {
            width: 100%;
            justify-content: flex-start;
        }
    }
</style>
