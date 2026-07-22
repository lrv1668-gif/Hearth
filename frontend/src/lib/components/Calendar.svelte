<script lang="ts">
    import type { Task, CalendarItem, Item } from '$lib/api';
    import { formatTime, eventDateKey } from '$lib/utils';
    import {
        ArrowDownToLine,
        ArrowLeft,
        ArrowRight,
        Calendar as CalendarIcon,
        Check,
        ChevronLeft,
        ChevronRight,
        Plus,
        X,
    } from '@lucide/svelte';
    import DayOverflowModal from './modals/DayOverflowModal.svelte';
    import { DAY_NAMES, MONTH_NAMES } from '$lib/constants/calendar';

    interface Props {
        tasks: Task[];
        calItems: CalendarItem[];
        onToggle: (task: Task) => void;
        onDelete: (id: number) => void;
        onNewTask: () => void;
        onEdit: (task: Task) => void;
        onDateClick: (date: string) => void;
        onEventClick?: (event: CalendarItem) => void;
        onToggleCalendarTask?: (taskListId: string, taskId: string, completed: boolean) => void;
    }

    let {
        tasks,
        calItems,
        onToggle,
        onDelete,
        onNewTask,
        onEdit,
        onDateClick,
        onEventClick,
        onToggleCalendarTask,
    }: Props = $props();
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

    let calItemsByDate = $derived.by(() => {
        const map: Record<string, CalendarItem[]> = {};
        for (const e of calItems) {
            if (!e.start) continue;
            const key = eventDateKey(e);
            if (!key) continue;
            (map[key] ??= []).push(e);
        }
        return map;
    });

    let overflowDayKey = $state<string | null>(null);

    const overflowItems = $derived.by((): Item[] => {
        if (!overflowDayKey) return [];
        const t = (tasksByDate[overflowDayKey] ?? []).map((d) => ({ kind: 'task' as const, data: d }));
        const ev = (calItemsByDate[overflowDayKey] ?? []).map((d) => ({ kind: 'event' as const, data: d }));
        return [...t, ...ev];
    });
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
                ? 'cursor-default border-(--border) text-(--text-4) opacity-50'
                : 'border-(--border) text-(--text-2) hover:border-(--text-3) hover:bg-(--surface) hover:text-(--text-1)'}"
        >
            {#if isCurrentMonth}
                <ArrowDownToLine class="icon-sm" />
            {:else if todayArrowDir === 'left'}
                <ArrowLeft class="icon-sm" />
            {:else}
                <ArrowRight class="icon-sm" />
            {/if}
            Today
        </button>

        <!-- Center: prev + next arrows -->
        <div class="flex items-center gap-1">
            <button
                onclick={prevMonth}
                class="p-1.5 text-(--text-2) transition-colors hover:text-(--text-1)"
                aria-label="Previous month"
            >
                <ChevronLeft class="icon-md" />
            </button>
            <button
                onclick={nextMonth}
                class="p-1.5 text-(--text-2) transition-colors hover:text-(--text-1)"
                aria-label="Next month"
            >
                <ChevronRight class="icon-md" />
            </button>
        </div>

        <!-- Month + year -->
        <h2 class="type-body font-bold tracking-widest text-(--text-1) uppercase select-none">
            {MONTH_NAMES[viewMonth]}
            {viewYear}
        </h2>

        <div class="flex-1"></div>

        <!-- Far right: new task -->
        <button
            onclick={onNewTask}
            class="type-label flex items-center gap-1.5 rounded-lg bg-(--accent) px-3 py-1
             font-medium text-(--accent-fg)
             transition-colors hover:bg-(--accent-hi)"
        >
            <Plus class="icon-md" />
            New Task
        </button>
    </div>

    <!-- Calendar grid — gap-px + bg-(--border) creates hairline grid lines -->
    <div
        class="grid min-h-0 flex-1 grid-cols-7 gap-px overflow-hidden rounded-lg border border-(--border) bg-(--border)"
        style="grid-template-rows: auto repeat({numWeeks}, 1fr)"
    >
        <!-- Day-of-week headers -->
        {#each DAY_NAMES as day}
            <div class="type-label bg-(--surface) py-2.5 text-center font-semibold tracking-widest text-(--text-2)">
                {day}
            </div>
        {/each}

        <!-- Day cells -->
        {#each calendarCells as day, i (i)}
            {@const key = day ? cellKey(day) : ''}
            {@const isToday = key === todayKey}
            {@const dayTasks = day ? (tasksByDate[key] ?? []) : []}
            {@const dayCalItems = day ? (calItemsByDate[key] ?? []) : []}
            {@const dayItems = [...dayTasks, ...dayCalItems]}

            <!-- svelte-ignore a11y_no_noninteractive_tabindex -->
            <div
                role={day ? 'button' : undefined}
                tabindex={day ? 0 : undefined}
                onclick={() => day && onDateClick(key)}
                onkeydown={(e) => (e.key === 'Enter' || e.key === ' ') && day && onDateClick(key)}
                class="flex h-full w-full flex-col items-start p-3 text-left
                  {day
                    ? isToday
                        ? 'cursor-default bg-(--surface-hi) ring-2 ring-(--accent) transition-colors ring-inset hover:bg-(--surface-hi)'
                        : 'cursor-default bg-(--bg) transition-colors hover:bg-(--surface)'
                    : 'pointer-events-none bg-(--surface) opacity-60'}"
                aria-label={day ? `Add task on ${key}` : undefined}
            >
                {#if day}
                    <span
                        class="type-label mb-1.5 block leading-none
                               {isToday ? 'font-semibold text-(--text-1)' : 'text-(--text-3)'}"
                    >
                        {day}
                    </span>

                    {#if dayItems.length > 0}
                        <ul class="space-y-1">
                            {#each dayTasks.slice(0, 4) as task (task.id)}
                                <li class="flex min-w-0 items-center gap-1">
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            onToggle(task);
                                        }}
                                        class="h-2 w-2 flex-shrink-0 rounded-full transition-colors
                           {task.done ? 'bg-(--done-bg)' : 'bg-(--text-3) hover:bg-(--text-1)'}"
                                        aria-label="Toggle {task.title}"
                                    ></button>
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            onEdit(task);
                                        }}
                                        class="type-label min-w-0 truncate text-left transition-colors hover:underline
                               {task.done ? 'text-(--done) line-through' : 'text-(--text-1)'}"
                                    >
                                        {#if task.due_time}
                                            <span class="mr-0.5 text-(--text-3)">{formatTime(task.due_time)}</span>
                                        {/if}
                                        {task.title}
                                    </button>
                                </li>
                            {/each}
                            {#each dayCalItems.slice(0, Math.max(0, 4 - dayTasks.length)) as calItem (calItem.id)}
                                <li class="flex min-w-0 items-center gap-1">
                                    {#if calItem.kind === 'task'}
                                        <button
                                            onclick={(e) => {
                                                e.stopPropagation();
                                                if (calItem.task_list_id)
                                                    onToggleCalendarTask?.(
                                                        calItem.task_list_id,
                                                        calItem.id,
                                                        !calItem.is_completed
                                                    );
                                            }}
                                            class="h-2 w-2 flex-shrink-0 rounded-full transition-colors
                                               {calItem.is_completed
                                                ? 'bg-(--done-bg)'
                                                : 'bg-(--accent) hover:opacity-80'}"
                                            aria-label="Toggle {calItem.title}"
                                        ></button>
                                        <span
                                            class="type-label min-w-0 truncate
                                                   {calItem.is_completed
                                                ? 'text-(--done) line-through'
                                                : 'text-(--text-1)'}"
                                        >
                                            {calItem.title}
                                        </span>
                                    {:else}
                                        <CalendarIcon
                                            class="h-2 w-2 flex-shrink-0 text-(--accent)"
                                            aria-hidden="true"
                                        />
                                        <button
                                            onclick={(e) => {
                                                e.stopPropagation();
                                                onEventClick?.(calItem);
                                            }}
                                            class="type-label min-w-0 flex-1 truncate text-left text-(--text-1) transition-opacity hover:opacity-70"
                                        >
                                            {#if !calItem.is_all_day && calItem.start}
                                                <span class="mr-0.5 text-(--text-3)">
                                                    {formatTime(calItem.start.slice(11, 16))}
                                                </span>
                                            {/if}
                                            {calItem.title}
                                        </button>
                                    {/if}
                                </li>
                            {/each}
                            {#if dayItems.length > 4}
                                <li>
                                    <button
                                        onclick={(e) => {
                                            e.stopPropagation();
                                            overflowDayKey = key;
                                        }}
                                        class="type-label text-(--text-3) transition-colors hover:text-(--text-1)"
                                    >
                                        +{dayItems.length - 4} more
                                    </button>
                                </li>
                            {/if}
                        </ul>
                    {/if}
                {/if}
            </div>
        {/each}
    </div>

    <DayOverflowModal
        bind:dayKey={overflowDayKey}
        items={overflowItems}
        {onToggle}
        {onEdit}
        {onEventClick}
        {onToggleCalendarTask}
    />

    <!-- Undated tasks -->
    {#if undatedTasks.length > 0}
        <div class="space-y-3">
            <h3 class="type-label font-medium tracking-widest text-(--text-3) uppercase">No due date</h3>
            <ul class="space-y-1.5">
                {#each undatedTasks as task (task.id)}
                    <li class="group flex items-center gap-3 rounded-lg bg-(--surface) px-3 py-2.5">
                        <button
                            onclick={() => onToggle(task)}
                            class="flex h-4 w-4 flex-shrink-0 items-center justify-center rounded border transition-colors
                     {task.done ? 'border-(--done-bg) bg-(--done-bg)' : 'border-(--text-3) hover:border-(--text-1)'}"
                            aria-label="Toggle {task.title}"
                        >
                            {#if task.done}
                                <Check class="text-(--bg)" />
                            {/if}
                        </button>

                        <button
                            onclick={() => onEdit(task)}
                            class="type-body flex-1 text-left transition-colors hover:underline
                         {task.done ? 'text-(--done) line-through' : 'text-(--text-1)'}"
                        >
                            {task.title}
                        </button>

                        <button
                            onclick={() => onDelete(task.id)}
                            class="type-title leading-none text-(--text-4) opacity-0
                     transition group-hover:opacity-100 hover:text-(--text-2)"
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
