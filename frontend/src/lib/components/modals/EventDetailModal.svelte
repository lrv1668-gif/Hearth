<script lang="ts">
    import type { CalendarEvent } from '$lib/api';
    import { formatTime, providerLabel } from '$lib/utils';
    import { Cable, CalendarDays, MapPin, TextAlignStart, X } from '@lucide/svelte';

    interface Props {
        event: CalendarEvent | null;
        onClose: () => void;
    }

    let { event, onClose }: Props = $props();

    let dialog = $state<HTMLDialogElement | null>(null);

    $effect(() => {
        if (!dialog) return;
        if (event) dialog.showModal();
        else if (dialog.open) dialog.close();
    });

    function formatDateRange(e: CalendarEvent): string {
        if (e.is_all_day) {
            const [y, m, d] = e.start.split('-').map(Number);
            return new Date(y, m - 1, d).toLocaleDateString('en-US', {
                weekday: 'long',
                month: 'long',
                day: 'numeric',
                year: 'numeric',
            });
        }
        const start = new Date(e.start);
        const end = new Date(e.end);
        const datePart = start.toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
        });
        const toHHMM = (d: Date) =>
            `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
        return `${datePart} · ${formatTime(toHHMM(start))} – ${formatTime(toHHMM(end))}`;
    }
</script>

<!-- svelte-ignore a11y_no_noninteractive_element_interactions -->
<dialog
    bind:this={dialog}
    onclose={onClose}
    onclick={(e) => e.target === dialog && onClose()}
    class="w-full max-w-lg bg-transparent p-0 backdrop:bg-black/40 backdrop:backdrop-blur-sm"
>
    {#if event}
        <div class="mx-4 max-h-[70vh] space-y-3 overflow-y-auto rounded-2xl border border-[var(--border)] bg-[var(--bg)] p-5 shadow-xl">
            <div class="flex items-start justify-between gap-3">
                <h2 class="type-title min-w-0 break-words font-semibold text-[var(--text-1)]">{event.title}</h2>
                <button
                    onclick={onClose}
                    class="flex h-6 w-6 flex-shrink-0 items-center justify-center text-[var(--text-1)] transition-colors hover:text-[var(--text-1)]"
                    aria-label="Close"
                >
                    <X class="icon-sm" />
                </button>
            </div>

            <div class="flex items-center gap-2">
                <CalendarDays class="icon-sm text-[var(--text-1)]" />
                <p class="type-body text-[var(--text-1)]">{formatDateRange(event)}</p>
            </div>

            {#if event.location}
                <div class="flex min-w-0 items-start gap-2">
                    <MapPin class="icon-sm mt-0.5 flex-shrink-0 text-[var(--text-1)]" />
                    <span class="type-body min-w-0 break-words text-[var(--text-1)]">{event.location}</span>
                </div>
            {/if}

            {#if event.description}
                <div class="flex min-w-0 items-start gap-2">
                    <TextAlignStart class="icon-sm mt-0.5 flex-shrink-0 text-[var(--text-1)]" />
                    <p class="event-description type-body min-w-0 whitespace-pre-line text-[var(--text-1)]">{@html event.description}</p>
                </div>
            {/if}

            <div class="flex items-center gap-2">
                <Cable class="icon-sm text-[var(--text-2)]" />
                <p class="type-body text-[var(--text-2)]">{providerLabel(event.provider)}</p>
            </div>
        </div>
    {/if}
</dialog>

<style>
    /* break-words doesn't cascade into {@html} children — target them explicitly */
    .event-description :global(*) {
        word-break: break-word;
        overflow-wrap: break-word;
    }
</style>
