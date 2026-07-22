<script lang="ts">
    import type { CalendarItem } from '$lib/api';
    import { formatTime, providerLabel } from '$lib/utils';
    import { CalendarDays, ExternalLink, MapPin, Plug, TextAlignStart, X } from '@lucide/svelte';
    import DOMPurify from 'dompurify';

    interface Props {
        event: CalendarItem | null;
        onClose: () => void;
    }

    let { event, onClose }: Props = $props();

    const safeDescription = $derived(event?.description ? DOMPurify.sanitize(event.description) : '');

    let dialog = $state<HTMLDialogElement | null>(null);

    $effect(() => {
        if (!dialog) return;
        if (event) dialog.showModal();
        else if (dialog.open) dialog.close();
    });

    function formatDateRange(e: CalendarItem): string {
        if (!e.start) return 'No date';
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
        const end = e.end ? new Date(e.end) : start;
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
        <div
            class="mx-4 max-h-[70vh] space-y-3 overflow-y-auto rounded-2xl border border-(--border) bg-(--bg) p-5 shadow-xl"
        >
            <div class="flex items-start justify-between gap-3">
                <h2 class="type-title min-w-0 font-semibold break-words text-(--text-1)">{event.title}</h2>
                <button
                    onclick={onClose}
                    class="flex h-6 w-6 flex-shrink-0 items-center justify-center text-(--text-1) transition-colors hover:text-(--text-1)"
                    aria-label="Close"
                >
                    <X class="icon-sm" />
                </button>
            </div>

            <div class="flex items-center gap-2">
                <CalendarDays class="icon-sm text-(--text-1)" />
                <p class="type-body text-(--text-1)">{formatDateRange(event)}</p>
            </div>

            {#if event.location}
                <div class="flex min-w-0 items-start gap-2">
                    <MapPin class="icon-sm mt-0.5 flex-shrink-0 text-(--text-1)" />
                    <span class="type-body min-w-0 break-words text-(--text-1)">{event.location}</span>
                </div>
            {/if}

            {#if event.description}
                <div class="flex min-w-0 items-start gap-2">
                    <TextAlignStart class="icon-sm mt-0.5 flex-shrink-0 text-(--text-1)" />
                    <p class="event-description type-body min-w-0 whitespace-pre-line text-(--text-1)">
                        {@html safeDescription}
                    </p>
                </div>
            {/if}

            <div class="flex items-center justify-between gap-2">
                <div class="flex items-center gap-2">
                    <Plug class="icon-sm text-(--text-2)" />
                    <p class="type-body text-(--text-2)">{providerLabel(event.provider)}</p>
                </div>
                {#if event.html_link}
                    <a
                        href={event.html_link}
                        target="_blank"
                        rel="noopener noreferrer"
                        class="type-label flex items-center gap-1 text-(--accent) transition-opacity hover:opacity-70"
                    >
                        Open <ExternalLink class="icon-xs" />
                    </a>
                {/if}
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
