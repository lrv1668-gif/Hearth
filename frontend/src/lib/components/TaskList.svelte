<script lang="ts">
    import type { Task } from '$lib/api';
    import { ChevronDown, ChevronUp, RefreshCw } from '@lucide/svelte';

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
            recurrenceEndDate?: string
        ) => void;
        onSave?: (title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) => void;
    }

    let { task, initialDate, onAdd, onSave }: Props = $props();

    const isEdit = $derived(!!task);

    let newTitle = $state('');
    let newDueDate = $state(initialDate ?? '');
    let newDueTime = $state('');
    let allDay = $state(true);
    let showMore = $state(false);

    let description = $state('');
    let assignee = $state('');
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
        showMore = !!(task.description || task.assignee || task.recurrence_unit);
    });

    function recurrenceLabel(t: Task): string {
        if (!t.recurrence_unit) return '';
        const n = t.recurrence_interval ?? 1;
        if (t.recurrence_unit === 'day') return n === 1 ? 'Daily' : `Every ${n} days`;
        if (t.recurrence_unit === 'week') {
            if (t.recurrence_days) return `Weekly · ${t.recurrence_days}`;
            return n === 2 ? 'Bi-weekly' : 'Weekly';
        }
        if (t.recurrence_unit === 'month') return n === 1 ? 'Monthly' : `Every ${n} months`;
        return '';
    }

    const repeatOptions = [
        { unit: '', interval: 1, label: 'None' },
        { unit: 'day', interval: 1, label: 'Daily' },
        { unit: 'week', interval: 1, label: 'Weekly' },
        { unit: 'week', interval: 2, label: 'Bi-weekly' },
        { unit: 'month', interval: 1, label: 'Monthly' },
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
                recurrenceUnit && recurrenceEndDate ? recurrenceEndDate : undefined
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
            class="flex-1 bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
             rounded-lg px-4 py-2.5 type-body outline-none
             focus:ring-1 focus:ring-[var(--border)] transition"
        />
        <input
            type="date"
            value={newDueDate}
            oninput={handleDateChange}
            class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2.5 type-body
             outline-none focus:ring-1 focus:ring-[var(--border)] transition"
        />
        <button
            type="submit"
            disabled={!!newDueDate && !allDay && !newDueTime}
            class="px-4 py-2.5 bg-[var(--accent)] hover:bg-[var(--accent-hi)] text-[var(--accent-fg)]
             rounded-lg type-body font-medium transition-colors
             disabled:opacity-40 disabled:cursor-not-allowed"
        >
            {isEdit ? 'Save' : 'Add'}
        </button>
    </div>

    <!-- Time row -->
    {#if newDueDate}
        <div class="flex items-center gap-3">
            <div class="flex-1"></div>
            <label class="flex items-center gap-2 type-body text-[var(--text-2)] cursor-pointer select-none">
                <input
                    type="checkbox"
                    checked={allDay}
                    onchange={(e) => {
                        allDay = (e.target as HTMLInputElement).checked;
                        if (allDay) newDueTime = '';
                    }}
                    class="rounded accent-[var(--accent)] cursor-pointer"
                />
                All day
            </label>
            {#if !allDay}
                <input
                    type="time"
                    bind:value={newDueTime}
                    step="900"
                    class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-2 type-body
                           outline-none focus:ring-1 focus:ring-[var(--border)] transition"
                />
            {/if}
        </div>
    {/if}

    <!-- More options toggle -->
    <button
        type="button"
        onclick={() => (showMore = !showMore)}
        class="flex items-center gap-1 type-label text-[var(--text-4)] hover:text-[var(--text-2)] transition-colors"
    >
        {#if showMore}<ChevronUp size={12} class="icon-sm" />{:else}<ChevronDown size={12} class="icon-sm" />{/if}
        More options
    </button>

    {#if showMore}
        <div class="space-y-3 pt-1 border-t border-[var(--border)]">
            <!-- Description -->
            <textarea
                bind:value={description}
                placeholder="Description (optional)"
                rows={2}
                class="w-full bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
                       rounded-lg px-4 py-2.5 text-sm outline-none resize-none
                       focus:ring-1 focus:ring-[var(--border)] transition mt-1"
            ></textarea>

            <!-- Assignee -->
            <input
                bind:value={assignee}
                placeholder="Assign to"
                class="w-full bg-[var(--surface)] text-[var(--text-1)] placeholder-[var(--text-4)]
                       rounded-lg px-4 py-2.5 text-sm outline-none
                       focus:ring-1 focus:ring-[var(--border)] transition"
            />

            <!-- Recurrence -->
            {#if isEdit && task?.recurrence_unit}
                <div class="flex items-center gap-2 type-body text-[var(--text-3)]">
                    <RefreshCw size={12} class="icon-sm" />
                    <span>{recurrenceLabel(task)}</span>
                    <span class="type-label text-[var(--text-4)]">(recurrence cannot be changed)</span>
                </div>
            {:else if !isEdit}
                <div class="space-y-2">
                    <label class="type-label text-[var(--text-3)] uppercase tracking-wide">Repeat</label>
                    <div class="flex gap-2 flex-wrap">
                        {#each repeatOptions as opt}
                            <button
                                type="button"
                                onclick={() => {
                                    recurrenceUnit = opt.unit;
                                    recurrenceInterval = opt.interval;
                                    recurrenceDays = [];
                                }}
                                class="px-3 py-1 rounded-full type-label transition-colors
                                       {recurrenceUnit === opt.unit && recurrenceInterval === opt.interval
                                    ? 'bg-[var(--accent)] text-[var(--accent-fg)]'
                                    : 'bg-[var(--surface)] text-[var(--text-2)] hover:text-[var(--text-1)]'}"
                            >
                                {opt.label}
                            </button>
                        {/each}
                    </div>

                    {#if recurrenceUnit === 'week' && recurrenceInterval === 1}
                        <div class="flex gap-1 flex-wrap">
                            {#each weekdays as day}
                                <button
                                    type="button"
                                    onclick={() => toggleDay(day)}
                                    class="px-2 py-1 rounded type-label transition-colors
                                           {recurrenceDays.includes(day)
                                        ? 'bg-[var(--accent)] text-[var(--accent-fg)]'
                                        : 'bg-[var(--surface)] text-[var(--text-3)] hover:text-[var(--text-1)]'}"
                                >
                                    {day}
                                </button>
                            {/each}
                        </div>
                    {/if}

                    {#if recurrenceUnit && recurrenceUnit !== 'week'}
                        <div class="flex items-center gap-2 type-body text-[var(--text-2)]">
                            <span>Every</span>
                            <input
                                type="number"
                                bind:value={recurrenceInterval}
                                min={1}
                                max={365}
                                class="w-16 bg-[var(--surface)] text-[var(--text-1)] rounded-lg px-3 py-1.5 type-body
                                       outline-none focus:ring-1 focus:ring-[var(--border)] text-center"
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

                    <div class="flex items-center gap-2 text-sm text-[var(--text-2)]">
                        <span>Ends on</span>
                        <input
                            type="date"
                            bind:value={recurrenceEndDate}
                            min={newDueDate || undefined}
                            class="bg-[var(--surface)] text-[var(--text-3)] rounded-lg px-3 py-1.5 type-body
                                   outline-none focus:ring-1 focus:ring-[var(--border)] transition"
                        />
                        {#if recurrenceEndDate}
                            <button
                                type="button"
                                onclick={() => (recurrenceEndDate = '')}
                                class="type-label text-[var(--text-4)] hover:text-[var(--text-2)] transition"
                            >
                                Clear
                            </button>
                        {:else}
                            <span class="type-label text-[var(--text-4)]">optional</span>
                        {/if}
                    </div>
                </div>
            {/if}
        </div>
    {/if}
</form>
