<script lang="ts">
    import type { Task } from '$lib/api';
    import { ChevronDown, ChevronUp, RefreshCw, Timer } from '@lucide/svelte';

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

    const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

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
    <!-- Main row -->
    <div class="flex gap-2">
        <input
            bind:value={newTitle}
            onkeydown={(e) => e.key === 'Enter' && handleSubmit()}
            placeholder="Enter description..."
            class="type-body flex-1 rounded-lg bg-(--surface)
             px-4 py-2.5 text-(--text-1) placeholder-(--text-2) transition
             outline-none focus:ring-1 focus:ring-(--border)"
        />
        <input
            type="date"
            value={newDueDate}
            oninput={handleDateChange}
            class="type-body rounded-lg bg-(--surface) px-3 py-2.5 text-(--text-2)
             transition outline-none focus:ring-1 focus:ring-(--border)"
        />
        <button
            type="submit"
            disabled={!!newDueDate && !allDay && !newDueTime}
            class="type-body rounded-lg bg-(--accent) px-4 py-2.5
             font-medium text-(--accent-fg) transition-colors hover:bg-(--accent-hi)
             disabled:cursor-not-allowed disabled:opacity-40"
        >
            {isEdit ? 'Save' : 'Add'}
        </button>
    </div>

    <!-- Time row -->
    {#if newDueDate}
        <div class="flex items-center gap-3">
            <div class="flex-1"></div>
            <label class="type-body flex cursor-pointer items-center gap-2 text-(--text-2) select-none">
                <input
                    type="checkbox"
                    checked={allDay}
                    onchange={(e) => {
                        allDay = (e.target as HTMLInputElement).checked;
                        if (allDay) newDueTime = '';
                    }}
                    class="cursor-pointer rounded accent-(--accent)"
                />
                All day
            </label>
            {#if !allDay}
                <input
                    type="time"
                    bind:value={newDueTime}
                    step="900"
                    class="type-body rounded-lg bg-(--surface) px-3 py-2 text-(--text-3)
                           transition outline-none focus:ring-1 focus:ring-(--border)"
                />
            {/if}
        </div>
    {/if}

    <!-- More options toggle -->
    <button
        type="button"
        onclick={() => (showMore = !showMore)}
        class="type-label flex items-center gap-1 text-(--text-2) transition-colors hover:text-(--text-1)"
    >
        {#if showMore}<ChevronUp class="icon-sm" />{:else}
            <ChevronDown class="icon-sm" />{/if}
        More options
    </button>

    {#if showMore}
        <div class="space-y-3 border-t border-(--border) pt-1">
            <!-- Description -->
            <textarea
                bind:value={description}
                placeholder="Description (optional)"
                rows={2}
                class="type-body mt-1 w-full resize-none
                       rounded-lg bg-(--surface) px-4 py-2.5 text-(--text-1) placeholder-(--text-2)
                       transition outline-none focus:ring-1 focus:ring-(--border)"
            ></textarea>

            <!-- Assignee -->
            <input
                bind:value={assignee}
                placeholder="Assign to (optional)"
                class="type-body w-full rounded-lg bg-(--surface)
                       px-4 py-2.5 text-(--text-1) placeholder-(--text-2) transition
                       outline-none focus:ring-1 focus:ring-(--border)"
            />

            <!-- Countdown toggle (add mode) / indicator (edit mode) -->
            {#if isEdit && task?.is_countdown}
                <div class="type-body flex items-center gap-2 text-(--text-3)">
                    <Timer class="icon-sm" />
                    <span>Event countdown</span>
                </div>
            {:else if !isEdit}
                <label class="group flex cursor-pointer items-center gap-3">
                    <input type="checkbox" bind:checked={isCountdown} class="h-4 w-4 accent-(--accent)" />
                    <span class="type-body text-(--text-2) transition-colors select-none group-hover:text-(--text-1)">
                        Event countdown
                    </span>
                </label>
            {/if}

            <!-- Recurrence -->
            {#if isEdit && task?.recurrence_unit}
                <div class="type-body flex items-center gap-2 text-(--text-3)">
                    <RefreshCw class="icon-sm" />
                    <span>{recurrenceLabel(task)}</span>
                    <span class="type-label text-(--text-4)">(recurrence cannot be changed)</span>
                </div>
            {:else if !isEdit && !isCountdown}
                <div class="space-y-2">
                    <p class="type-label tracking-wide text-(--text-2) uppercase">Repeat</p>
                    <div class="flex flex-wrap gap-2">
                        {#each repeatOptions as opt}
                            <button
                                type="button"
                                onclick={() => {
                                    recurrenceUnit = opt.unit;
                                    recurrenceInterval = opt.interval;
                                    recurrenceDays = [];
                                }}
                                class="type-label rounded-full px-3 py-1 transition-colors
                                       {recurrenceUnit === opt.unit && recurrenceInterval === opt.interval
                                    ? 'bg-(--accent) text-(--accent-fg)'
                                    : 'bg-(--surface) text-(--text-2) hover:bg-(--text-4) hover:text-(--text-1)'}"
                            >
                                {opt.label}
                            </button>
                        {/each}
                    </div>

                    {#if recurrenceUnit === 'week' && recurrenceInterval === 1}
                        <div class="flex flex-wrap gap-1">
                            {#each weekdays as day}
                                <button
                                    type="button"
                                    onclick={() => toggleDay(day)}
                                    class="type-label rounded px-2 py-1 transition-colors
                                           {recurrenceDays.includes(day)
                                        ? 'bg-(--accent) text-(--accent-fg)'
                                        : 'bg-(--surface) text-(--text-2) hover:bg-(--text-4) hover:text-(--text-1)'}"
                                >
                                    {day}
                                </button>
                            {/each}
                        </div>
                    {/if}

                    {#if recurrenceUnit && recurrenceUnit !== 'week'}
                        <div class="type-body flex items-center gap-2 text-(--text-2)">
                            <span>Every</span>
                            <input
                                type="number"
                                bind:value={recurrenceInterval}
                                min={1}
                                max={365}
                                class="type-body w-16 rounded-lg bg-(--surface) px-3 py-1.5 text-center
                                       text-(--text-2) outline-none focus:ring-1 focus:ring-(--border)"
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

                    <div class="type-body flex items-center gap-2 text-(--text-2)">
                        <span>Ends on</span>
                        <input
                            type="date"
                            bind:value={recurrenceEndDate}
                            min={newDueDate || undefined}
                            class="type-body rounded-lg bg-(--surface) px-3 py-1.5 placeholder-(--text-2)
                                   transition outline-none focus:ring-1 focus:ring-(--border)"
                        />
                        {#if recurrenceEndDate}
                            <button
                                type="button"
                                onclick={() => (recurrenceEndDate = '')}
                                class="type-label text-(--text-2) transition hover:text-(--text-1)"
                            >
                                Clear
                            </button>
                        {:else}
                            <span class="type-label text-(--text-2)">optional</span>
                        {/if}
                    </div>
                </div>
            {/if}
        </div>
    {/if}
</form>
