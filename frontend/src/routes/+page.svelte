<script lang="ts">
    import type { Task } from '$lib/api';
    import { tasks, toggleTask, removeTask, editTask, addTask } from '$lib/TaskStore';
    import UpcomingTasksWidget from '$lib/components/widgets/UpcomingTasksWidget.svelte';
    import TaskModal from '$lib/components/modals/TaskModal.svelte';
    import NowPlayingWidget from '$lib/components/widgets/NowPlayingWidget.svelte';
    import WeatherWidget from '$lib/components/widgets/WeatherWidget.svelte';
    import MoonPhaseWidget from '$lib/components/widgets/MoonPhaseWidget.svelte';
    import CountdownWidget from '$lib/components/widgets/CountdownWidget.svelte';

    let modalOpen = $state(false);
    let editingTask = $state<Task | null>(null);

    function openEditTask(task: Task) {
        editingTask = task;
        modalOpen = true;
    }

    function handleSave(title: string, dueDate?: string, dueTime?: string, description?: string, assignee?: string) {
        if (editingTask) {
            editTask(editingTask, title, dueDate, dueTime, description, assignee);
            editingTask = null;
        }
    }
</script>

<svelte:head>
    <title>Hearth — Schedule</title>
</svelte:head>

<main class="max-w-5xl mx-auto px-6 md:px-8 py-6 md:py-4">
    <div class="grid grid-cols-1 md:grid-cols-[3fr_2fr] gap-8 items-start">
        <!-- Left column: schedule -->
        <div class="flex flex-col gap-4">
            <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3">
                Upcoming Tasks
            </h2>
            <UpcomingTasksWidget
                tasks={$tasks.filter((t) => !t.is_countdown)}
                onToggle={toggleTask}
                onDelete={removeTask}
                onEdit={openEditTask}
            />

            <div>
                <div class="flex items-baseline justify-between border-b border-[var(--border)] pb-3 mb-4">
                    <h2 class="type-title font-semibold text-[var(--text-1)]">Countdowns</h2>
                </div>
                <CountdownWidget tasks={$tasks} onEdit={openEditTask} />
            </div>

            <div>
                <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">
                    Now Playing
                </h2>
                <NowPlayingWidget />
            </div>
        </div>

        <!-- Right column: date display + music + weather + calendar teaser -->
        <div class="hidden md:flex flex-col gap-8">
            <div>
                <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">
                    Today's Date
                </h2>
                <div class="text-right">
                    <p class="type-title font-bold text-[var(--text-1)] leading-tight">
                        {new Date().toLocaleDateString('en-US', { day: 'numeric', month: 'long' })}
                    </p>
                    <p class="type-subtitle tracking-widest uppercase text-[var(--text-2)] mt-2">
                        {new Date().toLocaleDateString('en-US', { weekday: 'long' })}
                    </p>
                </div>
            </div>

            <div>
                <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">
                    Weather Forecast
                </h2>
                <WeatherWidget />
            </div>

            <div>
                <h2 class="type-title font-semibold text-[var(--text-1)] border-b border-[var(--border)] pb-3 mb-4">
                    Moon Phase
                </h2>
                <MoonPhaseWidget />
            </div>
        </div>
    </div>
</main>

<TaskModal
    bind:open={modalOpen}
    task={editingTask}
    onSave={handleSave}
    onDelete={(id, series) => {
        removeTask(id, series);
        editingTask = null;
    }}
/>
