<script lang="ts">
    import type { Task, CalendarItem, Item } from '#lib/api.ts';
    import DayItemList from '../DayItemList.svelte';
    import Modal from '../ui/Modal.svelte';

    interface Props {
        dayKey?: string | null;
        items: Item[];
        onToggle: (task: Task) => void;
        onEdit: (task: Task) => void;
        onEventClick?: (event: CalendarItem) => void;
        onToggleCalendarTask?: (taskListId: string, taskId: string, completed: boolean) => void;
    }

    let { dayKey = $bindable(null), items, onToggle, onEdit, onEventClick, onToggleCalendarTask }: Props = $props();

    function close() {
        dayKey = null;
    }

    function dateLabel(key: string): string {
        const [y, m, d] = key.split('-').map(Number);
        return new Date(y, m - 1, d).toLocaleDateString('en-US', {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
        });
    }
</script>

<Modal open={!!dayKey} onclose={close} title={dayKey ? dateLabel(dayKey) : ''} size="sm">
    <DayItemList {items} {onToggle} {onEdit} {onEventClick} {onToggleCalendarTask} onOpen={close} />
</Modal>
