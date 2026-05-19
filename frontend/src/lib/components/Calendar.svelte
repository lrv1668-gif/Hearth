<script lang="ts">
    import type { Task } from '$lib/api';
    import { formatTime } from '$lib/utils';
    import { ArrowLeft, ArrowRight, Check, ChevronLeft, ChevronRight, Plus, X } from '@lucide/svelte';
    import DayOverflowModal from './modals/DayOverflowModal.svelte';
    import { DAY_NAMES, MONTH_NAMES } from '$lib/constants/calendar';

    interface Props {
        tasks: Task[];
        onToggle: (task: Task) => void;
        onDelete: (id: number) => void;
        onNewTask: () => void;
        onEdit: (task: Task) => void;
        onDateClick: (date: string) => void;
    }

    let { tasks, onToggle, onDelete, onNewTask, onEdit, onDateClick }: Props = $props();
    const today = new Date();

    function dateKey(d: Date): string {
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    }

    const todayKey = dateKey(today);

    let viewYear = $state(today.getFullYear());
    let viewMonth = $state(today.getMonth());

    let isCurrentMonth = $derived(viewYear === today.getFullYear() && viewMonth === today.getMonth());

    // 'left' = view is ahead of today (Today button goes back), 'right' = view is behind
    let todayArrowDir = $derived.by(() => {
        if (isCurrentMonth) return null;
        const viewTime = new Date(viewYear, viewMonth).getTime();
        const todayTime = new Date(today.getFullYear(), today.getMonth()).getTime();
        return viewTime > todayTime ? 'left' : 'right';
    });

    function prevMonth() {
        if (viewMonth === 0) {
            viewYear--;
            viewMonth = 11;
        } else viewMonth--;
    }

    function nextMonth() {
        if (viewMonth === 11) {
            viewYear++;
            viewMonth = 0;
        } else viewMonth++;
    }

    function goToToday() {
        viewYear = today.getFullYear();
        viewMonth = today.getMonth();
    }

    let undatedTasks = $derived(tasks.filter((t) => !t.due_date));

    let tasksByDate = $derived.by(() => {
        const map: Record<string, Task[]> = {};
        for (const t of tasks) {
            if (!t.due_date) continue;
            const key = dateKey(new Date(t.due_date));
            (map[key] ??= []).push(t);
        }
        for (const key in map) {
            map[key].sort((a, b) => {
                if (!a.due_time && !b.due_time) return 0;
                if (!a.due_time) return 1;
                if (!b.due_time) return -1;
                return a.due_time.localeCompare(b.due_time);
            });
        }
        return map;
    });

    let calendarCells = $derived.by(() => {
        const firstWeekday = new Date(viewYear, viewMonth, 1).getDay();
        const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
        const cells: (number | null)[] = Array(firstWeekday).fill(null);
        for (let d = 1; d <= daysInMonth; d++) cells.push(d);
        while (cells.length % 7 !== 0) cells.push(null);
        return cells;
    });

    let numWeeks = $derived(calendarCells.length / 7);

    function cellKey(day: number): string {
        return `${viewYear}-${String(viewMonth + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    }

    let overflowDayKey = $state<string | null>(null);

    const overflowTasks = $derived(overflowDayKey ? (tasksByDate[overflowDayKey] ?? []) : []);
</script>

<div class="flex min-h-0 flex-1 flex-col gap-6">
    <!-- Month navigation -->
    <div class="flex flex-row items-center gap-4">
        <!-- Left: today -->
        <button
            onclick={goToToday}
            disabled={isCurrentMonth}
            class="type-label flex items-center gap-1 rounded border px-2.5 py-1 tracking-wide transition-colors
             {isCurrentMonth
                ? 'cursor-default border-[var(--border)] text-[var(--text-4)] opacity-50'
                : 'border-[var(--border)] text-[var(--text-2)] hover:border-[var(--text-3)] hover:bg-[var(--surface)] hover:text-[var(--text-1)]'}"
        >
            <ArrowLeft class="icon-sm {todayArrowDir === 'left' ? '' : 'invisible'}" />
            Today
            <ArrowRight class="icon-sm {todayArrowDir === 'right' ? '' : 'invisible'}" />
        </button>

        <!-- Center: prev + next arrows -->
        <div class="flex items-center gap-1">
            <button
                onclick={prevMonth}
                class="p-1.5 text-[var(--text-2)] transition-colors hover:text-[var(--text-1)]"
                aria-label="Previous month"
            >
                <ChevronLeft class="icon-md" />
            </button>
            <button
                onclick={nextMonth}
                class="p-1.5 text-[var(--text-2)] transition-colors hover:text-[var(--text-1)]"
                aria-label="Next month"
            >
                <ChevronRight class="icon-md" />
            </button>
        </div>

        <!-- Month + year -->
        <h2 class="type-body select-none font-bold uppercase tracking-widest text-[var(--text-1)]">
            {MONTH_NAMES[viewMonth]}
            {viewYear}
        </h2>

        <div class="flex-1"></div>

        <!-- Far right: new task -->
        <button
            onclick={onNewTask}
            class="type-label flex items-center gap-1.5 rounded-lg bg-[var(--accent)] px-3 py-1
             text-xs font-medium text-[var(--accent-fg)]
             transition-colors hover:bg-[var(--accent-hi)]"
        >
            <Plus class="icon-md" />
            New Task
        </button>
    </div>

    <!-- Calendar grid — gap-px + bg-[var(--border)] creates hairline grid lines -->
    <div
        class="grid min-h-0 flex-1 grid-cols-7 gap-px overflow-hidden rounded-lg border border-[var(--border)] bg-[var(--border)]"
        style="grid-template-rows: auto repeat({numWeeks}, 1fr)"
    >
        <!-- Day-of-week headers -->
        {#each DAY_NAMES as day}
            <div
                class="type-label bg-[var(--surface)] py-2.5 text-center font-semibold tracking-widest text-[var(--text-2)]"
            >
                {day}
            </div>
        {/each}

        <!-- Day cells -->
        {#each calendarCells as day, i (i)}
            {@const key = day ? cellKey(day) : ''}
            {@const isToday = key === todayKey}
            {@const dayTasks = day ? (tasksByDate[key] ?? []) : []}

            <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
            <div
                role={day ? 'button' : undefined}
                tabindex={day ? 0 : undefined}
                onclick={() => day && onDateClick(key)}
                onkeydown={(e) => (e.key === 'Enter' || e.key === ' ') && day && onDateClick(key)}
                class="flex h-full w-full flex-col items-start p-3 text-left
                  {day
                    ? isToday
                        ? 'cursor-default bg-[var(--surface-hi)] ring-2 ring-inset ring-[var(--accent)] transition-colors hover:bg-[var(--surface-hi)]'
                        : 'cursor-default bg-[var(--bg)] transition-colors hover:bg-[var(--surface)]'
                    : 'pointer-events-none bg-[var(--surface)] opacity-60'}"
                aria-label={day ? `Add task on ${key}` : undefined}
            >
                {#if day}
                    <span
                        class="type-label mb-1.5 block leading-none
                               {isToday ? 'font-semibold text-[var(--text-1)]' : 'text-[var(--text-3)]'}"
                    >
                        {day}
                    </span>

                    {#if dayTasks.length > 0}
                        <ul class="space-y-1">
                            {#each dayTasks.slice(0, 4) as task (task.id)}
                                <li class="flex min-w-0 items-center gap-1">
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            onToggle(task);
                                        }}
                                        class="h-2 w-2 flex-shrink-0 rounded-full transition-colors
                           {task.done ? 'bg-[var(--done-bg)]' : 'bg-[var(--text-3)] hover:bg-[var(--text-1)]'}"
                                        aria-label="Toggle {task.title}"
                                    ></button>
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            onEdit(task);
                                        }}
                                        class="type-label min-w-0 truncate text-left transition-colors hover:underline
                               {task.done ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                                    >
                                        {#if task.due_time}
                                            <span class="mr-0.5 text-[var(--text-3)]">{formatTime(task.due_time)}</span>
                                        {/if}
                                        {task.title}
                                    </button>
                                </li>
                            {/each}
                            {#if dayTasks.length > 4}
                                <li>
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            overflowDayKey = key;
                                        }}
                                        class="type-label text-[var(--text-3)] transition-colors hover:text-[var(--text-1)]"
                                    >
                                        +{dayTasks.length - 3} more
                                    </button>
                                </li>
                            {/if}
                        </ul>
                    {/if}
                {/if}
            </div>
        {/each}
    </div>

    <DayOverflowModal bind:dayKey={overflowDayKey} tasks={overflowTasks} {onToggle} {onEdit} />

    <!-- Undated tasks -->
    {#if undatedTasks.length > 0}
        <div class="space-y-3">
            <h3 class="type-label font-medium uppercase tracking-widest text-[var(--text-3)]">No due date</h3>
            <ul class="space-y-1.5">
                {#each undatedTasks as task (task.id)}
                    <li class="group flex items-center gap-3 rounded-lg bg-[var(--surface)] px-3 py-2.5">
                        <button
                            onclick={() => onToggle(task)}
                            class="flex h-4 w-4 flex-shrink-0 items-center justify-center rounded border transition-colors
                     {task.done
                                ? 'border-[var(--done-bg)] bg-[var(--done-bg)]'
                                : 'border-[var(--text-3)] hover:border-[var(--text-1)]'}"
                            aria-label="Toggle {task.title}"
                        >
                            {#if task.done}
                                <Check class="text-[var(--bg)]" />
                            {/if}
                        </button>

                        <button
                            onclick={() => onEdit(task)}
                            class="type-body flex-1 text-left transition-colors hover:underline
                         {task.done ? 'text-[var(--done)] line-through' : 'text-[var(--text-1)]'}"
                        >
                            {task.title}
                        </button>

                        <button
                            onclick={() => onDelete(task.id)}
                            class="type-title leading-none text-[var(--text-4)] opacity-0
                     transition hover:text-[var(--text-2)] group-hover:opacity-100"
                            aria-label="Delete {task.title}"
                        >
                            <X />
                        </button>
                    </li>
                {/each}
            </ul>
        </div>
    {/if}
</div>
