<script lang="ts">
    import { taskStore } from '#lib/stores/TaskStore.svelte.ts';
    import { heroCountdown } from '#lib/frame.ts';

    let { now, countdownId = null }: { now: Date; countdownId?: number | null } = $props();

    const countdown = $derived(heroCountdown(taskStore.tasks, now, countdownId));
    const date = $derived(
        countdown?.task.due_date
            ? new Date(`${countdown.task.due_date.slice(0, 10)}T00:00`).toLocaleDateString('en-US', {
                  weekday: 'long',
                  month: 'long',
                  day: 'numeric',
              })
            : ''
    );
</script>

<!--
@component
Countdown mode for the e-paper frame: one countdown as a poster — the day count huge
in the display face, a short --block rule, then the title and date. Shows the chosen
countdown (Settings → Frame) while it's still ahead, else the next one coming up.
Flat inks only, so it takes Daily's ordered dither.
-->
<div class="absolute inset-0 flex flex-col items-center justify-center px-24 text-center">
    {#if countdown}
        {#if countdown.days === 0}
            <p class="type-poster font-(family-name:--display-family) leading-[0.9] font-semibold italic">Today</p>
        {:else}
            <p class="type-poster font-(family-name:--display-family) leading-[0.85] font-semibold tracking-[-0.04em]">
                {countdown.days}
            </p>
            <p class="type-subtitle mt-2 font-semibold tracking-[0.24em] text-(--text-2) uppercase">
                {countdown.days === 1 ? 'day to go' : 'days to go'}
            </p>
        {/if}
        <div class="my-10 h-3 w-40 bg-(--block)"></div>
        <p class="type-hero font-(family-name:--display-family) leading-tight font-medium">{countdown.task.title}</p>
        <p class="type-subtitle mt-4 text-(--text-2)">{date}</p>
    {:else}
        <p class="type-title font-(family-name:--display-family) text-(--text-2) italic">Nothing to count down to</p>
    {/if}
</div>
