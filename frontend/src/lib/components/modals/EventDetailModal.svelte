<script lang="ts">
    import type { CalendarItem } from '$lib/api';
    import { formatTime, providerLabel } from '$lib/utils';
    import { CalendarDays, ExternalLink, MapPin, Plug, TextAlignStart } from '@lucide/svelte';
    import DOMPurify from 'dompurify';
    import Button from '../ui/Button.svelte';
    import Modal from '../ui/Modal.svelte';

    interface Props {
        event: CalendarItem | null;
        onClose: () => void;
    }

    let { event, onClose }: Props = $props();

    const safeDescription = $derived(event?.description ? DOMPurify.sanitize(event.description) : '');

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

<Modal open={!!event} onclose={onClose} title={event?.title ?? ''}>
    {#if event}
        <div class="space-y-3">
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
                    <Button variant="link" href={event.html_link} target="_blank" rel="noopener noreferrer">
                        Open <ExternalLink class="icon-xs" />
                    </Button>
                {/if}
            </div>
        </div>
    {/if}
</Modal>

<style>
    /* break-words doesn't cascade into {@html} children — target them explicitly */
    .event-description :global(*) {
        word-break: break-word;
        overflow-wrap: break-word;
    }
</style>
