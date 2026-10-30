<script lang="ts">
    import { onMount } from 'svelte';
    import { checkIn, clockLabel } from '#lib/frame.ts';
    import { frameStatusStore, loadFrameStatus } from '#lib/stores/FrameStatusStore.svelte.ts';
    import { poll } from '#lib/utils.ts';
    import InlineMessage from '../ui/InlineMessage.svelte';

    // `now` ticks with each refresh so "3 min ago" stays true
    const STATUS_REFRESH_MS = 60_000;
    let now = $state(new Date());
    onMount(() => {
        loadFrameStatus();
        return poll(() => {
            now = new Date();
            loadFrameStatus();
        }, STATUS_REFRESH_MS);
    });

    const status = $derived(frameStatusStore.status);
    const frame = $derived(status ? checkIn(status, now) : null);
    const nextRedraw = $derived(
        status?.next_render_at && new Date(status.next_render_at) > now ? clockLabel(status.next_render_at, now) : null
    );
</script>

<!--
@component
The top of Settings → Frame: whether the frame itself is still checking in for its
picture, when the panel next redraws, and the last render error if there was one —
the first thing to look at when the wall looks wrong. Hidden until the Display service
has answered.
-->
{#if status && frame}
    <div
        class="mb-4 flex flex-wrap items-center gap-x-4 gap-y-1 rounded-xl border-2 border-(--border) bg-(--surface) px-4 py-3"
        role="status"
    >
        <span
            class="size-2.5 shrink-0 rounded-full {frame.stale ? 'bg-(--danger)' : 'bg-(--accent)'}"
            aria-hidden="true"
        ></span>
        <p class="type-label min-w-0 flex-1 {frame.stale ? 'text-(--danger)' : 'text-(--text-1)'}">{frame.text}</p>
        {#if nextRedraw}
            <p class="type-label text-(--text-2) tabular-nums">Next redraw {nextRedraw}</p>
        {/if}
        {#if status.last_error}
            <InlineMessage tone="error" class="w-full">Last render failed: {status.last_error}</InlineMessage>
        {/if}
    </div>
{/if}
