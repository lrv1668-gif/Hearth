import { api, type Task } from '../api';

// `error`: the Tasks service couldn't be reached and there's nothing to show, so an empty
// list means "unknown", not "nothing due" — the frame says so rather than claim a clear day.
// `loaded`: the first load has finished, successfully or not — until then an empty list is
// still loading, so the dashboard shows a skeleton rather than "Nothing scheduled".
export const taskStore = $state({ tasks: [] as Task[], error: false, loaded: false });

// A failed refresh keeps the tasks already on screen
export async function loadTasks() {
    const tasks = await api.tasks.list();
    taskStore.loaded = true;
    if (!tasks) {
        taskStore.error = taskStore.tasks.length === 0;
        return;
    }
    taskStore.error = false;
    taskStore.tasks = tasks;
}

export async function addTask(
    title: string,
    dueDate?: string,
    dueTime?: string,
    description?: string,
    assignee?: string,
    recurrenceUnit?: string,
    recurrenceInterval?: number,
    recurrenceDays?: string,
    recurrenceEndDate?: string,
    isCountdown = false
) {
    const task = await api.tasks.create({
        title,
        due_date: dueDate ?? null,
        due_time: dueTime ?? null,
        description: description ?? null,
        assignee: assignee ?? null,
        recurrence_unit: recurrenceUnit ?? null,
        recurrence_interval: recurrenceInterval ?? null,
        recurrence_days: recurrenceDays ?? null,
        recurrence_end_date: recurrenceEndDate ?? null,
        is_countdown: isCountdown,
    });
    taskStore.tasks = [task, ...taskStore.tasks];
}

export async function toggleTask(task: Task) {
    const updated = await api.tasks.update(task.id, {
        done: !task.done,
        title: task.title,
        due_date: task.due_date,
        due_time: task.due_time,
        description: task.description,
        assignee: task.assignee,
    });
    taskStore.tasks = taskStore.tasks.map((t) => (t.id === task.id ? updated : t));
}

export async function editTask(
    task: Task,
    title: string,
    dueDate?: string,
    dueTime?: string,
    description?: string,
    assignee?: string
) {
    const updated = await api.tasks.update(task.id, {
        done: task.done,
        title,
        due_date: dueDate ?? null,
        due_time: dueTime ?? null,
        description: description ?? null,
        assignee: assignee ?? null,
    });
    taskStore.tasks = taskStore.tasks.map((t) => (t.id === task.id ? updated : t));
}

export async function removeTask(id: number, series = false) {
    // Only drop it from the list once the server has
    if (!(await api.tasks.delete(id, series))) return;
    if (series) {
        await loadTasks();
    } else {
        taskStore.tasks = taskStore.tasks.filter((t) => t.id !== id);
    }
}
