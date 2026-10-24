<script lang="ts">
    import type { Task } from '$lib/api';
    import { ChevronDown, ChevronUp, RefreshCw, Timer } from '@lucide/svelte';
    import Button from './ui/Button.svelte';
    import ChoiceChips from './ui/ChoiceChips.svelte';
    import DayPicker from './ui/DayPicker.svelte';
    import TextArea from './ui/TextArea.svelte';
    import TextInput from './ui/TextInput.svelte';

    interface Props {
        task?: Task;
        initialDate?: string;
        onAdd?: (
            title: string,
            dueDate?: string,
            dueTime?: string,
            description?: string,
            assignee?: string,
            recurrenceUnit?: string,
            recurrenceInterval?: number,
            recurrenceDays?: string,
            recurrenceEndDate?: string,
            isCountdown?: boolean
        ) => void;
        onSave?: (title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) => void;
    }

    let { task, initialDate, onAdd, onSave }: Props = $props();

    const isEdit = $derived(!!task);

    let newTitle = $state('');
    let newDueDate = $state('');
    let newDueTime = $state('');
    let allDay = $state(true);
    let showMore = $state(false);

    let description = $state('');
    let assignee = $state('');
    let isCountdown = $state(false);
    let recurrenceUnit = $state(''); // '' | 'day' | 'week' | 'month'
    let recurrenceInterval = $state(1);
    let recurrenceDays = $state<string[]>([]);
    let recurrenceEndDate = $state('');

    // Ids are what the Tasks service stores in recurrence_days
    const weekdays = [
        { id: 'Mon', label: 'Mon', name: 'Monday' },
        { id: 'Tue', label: 'Tue', name: 'Tuesday' },
        { id: 'Wed', label: 'Wed', name: 'Wednesday' },
        { id: 'Thu', label: 'Thu', name: 'Thursday' },
        { id: 'Fri', label: 'Fri', name: 'Friday' },
        { id: 'Sat', label: 'Sat', name: 'Saturday' },
        { id: 'Sun', label: 'Sun', name: 'Sunday' },
    ];

    $effect(() => {
        if (!task) newDueDate = initialDate ?? '';
    });

    $effect(() => {
        if (!task) return;
        newTitle = task.title;
        newDueDate = task.due_date ? task.due_date.slice(0, 10) : '';
        allDay = !task.due_time;
        newDueTime = task.due_time ?? '';
        description = task.description ?? '';
        assignee = task.assignee ?? '';
        recurrenceUnit = task.recurrence_unit ?? '';
        recurrenceInterval = task.recurrence_interval ?? 1;
        recurrenceDays = task.recurrence_days ? task.recurrence_days.split(',') : [];
        recurrenceEndDate = task.recurrence_end_date ? task.recurrence_end_date.slice(0, 10) : '';
        showMore = !!(task.description || task.assignee || task.recurrence_unit || task.is_countdown);
    });

    function recurrenceLabel(t: Task): string {
        if (!t.recurrence_unit) return '';
        const n = t.recurrence_interval ?? 1;
        if (t.recurrence_unit === 'day') return n === 1 ? 'Daily' : `Every ${n} days`;
        if (t.recurrence_unit === 'week') {
            if (t.recurrence_days) return `Weekly · ${t.recurrence_days}`;
            return n === 2 ? 'Bi-weekly' : 'Weekly';
        }
        if (t.recurrence_unit === 'month') {
            if (n === 12) return 'Yearly';
            return n === 1 ? 'Monthly' : `Every ${n} months`;
        }
        return '';
    }

    const repeatOptions = [
        { unit: '', interval: 1, label: 'None' },
        { unit: 'day', interval: 1, label: 'Daily' },
        { unit: 'week', interval: 1, label: 'Weekly' },
        { unit: 'week', interval: 2, label: 'Bi-weekly' },
        { unit: 'month', interval: 1, label: 'Monthly' },
        { unit: 'month', interval: 12, label: 'Yearly' },
    ];
    // ChoiceChips wants one value per option: "unit:interval"
    const repeatChoices = repeatOptions.map((o) => ({ value: `${o.unit}:${o.interval}`, label: o.label }));

    function chooseRepeat(value: string) {
        const opt = repeatOptions.find((o) => `${o.unit}:${o.interval}` === value)!;
        recurrenceUnit = opt.unit;
        recurrenceInterval = opt.interval;
        recurrenceDays = [];
    }

    function handleDateChange(e: Event) {
        newDueDate = (e.target as HTMLInputElement).value;
        if (!newDueDate) {
            allDay = true;
            newDueTime = '';
        }
    }

    function toggleDay(day: string) {
        recurrenceDays = recurrenceDays.includes(day)
            ? recurrenceDays.filter((d) => d !== day)
            : [...recurrenceDays, day];
    }

    function handleSubmit() {
        const title = newTitle.trim();
        if (!title) return;
        if (newDueDate && !allDay && !newDueTime) return;

        if (isEdit) {
            onSave?.(
                title,
                newDueDate || undefined,
                !allDay && newDueTime ? newDueTime : undefined,
                description.trim() || undefined,
                assignee.trim() || undefined
            );
        } else {
            onAdd?.(
                title,
                newDueDate || undefined,
                !allDay && newDueTime ? newDueTime : undefined,
                description.trim() || undefined,
                assignee.trim() || undefined,
                recurrenceUnit || undefined,
                recurrenceUnit ? recurrenceInterval : undefined,
                recurrenceUnit === 'week' && recurrenceDays.length > 0 ? recurrenceDays.join(',') : undefined,
                recurrenceUnit && recurrenceEndDate ? recurrenceEndDate : undefined,
                isCountdown
            );

            newTitle = '';
            newDueDate = '';
            newDueTime = '';
            allDay = true;
            description = '';
            assignee = '';
            recurrenceUnit = '';
            recurrenceInterval = 1;
            recurrenceDays = [];
            recurrenceEndDate = '';
            isCountdown = false;
            showMore = false;
        }
    }
</script>

<form
    class="space-y-2"
    onsubmit={(e) => {
        e.preventDefault();
        handleSubmit();
    }}
>
    <!-- Main row — wraps on phones so the date and Add button stay on screen -->
    <div class="flex flex-wrap gap-2 sm:flex-nowrap">
        <TextInput
            bind:value={newTitle}
            placeholder="What needs doing?"
            aria-label="Task"
            class="w-full sm:w-auto sm:flex-1"
        />
        <TextInput
            type="date"
            value={newDueDate}
            oninput={handleDateChange}
            aria-label="Due date"
            class="flex-1 sm:flex-none"
        />
        <Button type="submit" variant="primary" size="lg" disabled={!!newDueDate && !allDay && !newDueTime}>
            {isEdit ? 'Save' : 'Add'}
        </Button>
    </div>

    <!-- Time row -->
    {#if newDueDate}
        <div class="flex items-center justify-end gap-3">
            <label class="type-body flex cursor-pointer items-center gap-2 text-(--text-2) select-none">
                <input
                    type="checkbox"
                    checked={allDay}
                    onchange={(e) => {
                        allDay = (e.target as HTMLInputElement).checked;
                        if (allDay) newDueTime = '';
                    }}
                    class="h-4 w-4 cursor-pointer accent-(--accent)"
                />
                All day
            </label>
            {#if !allDay}
                <TextInput type="time" bind:value={newDueTime} step="900" aria-label="Due time" />
            {/if}
        </div>
    {/if}

    <!-- More options toggle -->
    <Button variant="ghost" aria-expanded={showMore} onclick={() => (showMore = !showMore)}>
        {#if showMore}<ChevronUp class="icon-sm" />{:else}<ChevronDown class="icon-sm" />{/if}
        More options
    </Button>

    {#if showMore}
        <div class="space-y-3 border-t border-(--border) pt-3">
            <TextArea
                bind:value={description}
                placeholder="Notes (optional)"
                aria-label="Notes"
                rows={2}
                class="w-full resize-none"
            />

            <TextInput bind:value={assignee} placeholder="Assign to (optional)" aria-label="Assign to" class="w-full" />

            <!-- Countdown toggle (add mode) / indicator (edit mode) -->
            {#if isEdit && task?.is_countdown}
                <div class="type-body flex items-center gap-2 text-(--text-2)">
                    <Timer class="icon-sm" />
                    <span>Event countdown</span>
                </div>
            {:else if !isEdit}
                <label class="group flex cursor-pointer items-center gap-3">
                    <input
                        type="checkbox"
                        bind:checked={isCountdown}
                        class="h-4 w-4 cursor-pointer accent-(--accent)"
                    />
                    <span class="type-body text-(--text-2) transition-colors select-none group-hover:text-(--text-1)">
                        Event countdown
                    </span>
                </label>
            {/if}

            <!-- Recurrence -->
            {#if isEdit && task?.recurrence_unit}
                <div class="type-body flex flex-wrap items-center gap-2 text-(--text-2)">
                    <RefreshCw class="icon-sm" />
                    <span>{recurrenceLabel(task)}</span>
                    <span class="type-label">(recurrence cannot be changed)</span>
                </div>
            {:else if !isEdit && !isCountdown}
                <div class="space-y-2">
                    <p class="eyebrow">Repeat</p>
                    <ChoiceChips
                        label="Repeat"
                        options={repeatChoices}
                        value={`${recurrenceUnit}:${recurrenceInterval}`}
                        onchange={chooseRepeat}
                    />

                    {#if recurrenceUnit === 'week' && recurrenceInterval === 1}
                        <DayPicker days={weekdays} selected={recurrenceDays} ontoggle={toggleDay} label="Repeat on" />
                    {/if}

                    {#if recurrenceUnit && recurrenceUnit !== 'week'}
                        <div class="type-body flex items-center gap-2 text-(--text-2)">
                            <span>Every</span>
                            <TextInput
                                type="number"
                                bind:value={recurrenceInterval}
                                min={1}
                                max={365}
                                aria-label="Repeat interval"
                                class="w-20 text-center"
                            />
                            <span
                                >{recurrenceUnit === 'day'
                                    ? 'day(s)'
                                    : recurrenceUnit === 'month'
                                      ? 'month(s)'
                                      : ''}</span
                            >
                        </div>
                    {/if}

                    <div class="type-body flex flex-wrap items-center gap-2 text-(--text-2)">
                        <span>Ends on</span>
                        <TextInput
                            type="date"
                            bind:value={recurrenceEndDate}
                            min={newDueDate || undefined}
                            aria-label="Repeat ends on"
                        />
                        {#if recurrenceEndDate}
                            <Button variant="ghost" onclick={() => (recurrenceEndDate = '')}>Clear</Button>
                        {:else}
                            <span class="type-label">optional</span>
                        {/if}
                    </div>
                </div>
            {/if}
        </div>
    {/if}
</form>
