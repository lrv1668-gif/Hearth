import { api, type CreateTaskInput, type Task } from '../api';

// `error`: the Tasks service couldn't be reached and there's nothing to show, so an empty
// list means "unknown", not "nothing due" — the frame says so rather than claim a clear day.
// `loaded`: the first load has finished, successfully or not — until then an empty list is
// still loading, so the dashboard shows a skeleton rather than "Nothing scheduled".
// `saveError`: the last add, edit, tick or delete didn't reach the Tasks service, so what's on
// screen is what was there before it; cleared by the next one that does.
export const taskStore = $state({ tasks: [] as Task[], error: false, loaded: false, saveError: false });

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

/** Runs a write, leaving the list as it was and raising `saveError` when it fails. */
async function save<T>(write: () => Promise<T>): Promise<T | null> {
    try {
        const result = await write();
        taskStore.saveError = false;
        return result;
    } catch {
        taskStore.saveError = true;
        return null;
    }
}

export async function addTask(input: CreateTaskInput) {
    const task = await save(() => api.tasks.create(input));
    if (!task) return;
    // A repeating task comes back as its first instance; the rest are on the server
    if (task.series_id !== null) await loadTasks();
    else taskStore.tasks = [task, ...taskStore.tasks];
}

export async function toggleTask(task: Task) {
    const updated = await save(() =>
        api.tasks.update(task.id, {
            done: !task.done,
            title: task.title,
            due_date: task.due_date,
            due_time: task.due_time,
            description: task.description,
            assignee: task.assignee,
        })
    );
    if (updated) taskStore.tasks = taskStore.tasks.map((t) => (t.id === task.id ? updated : t));
}

export async function editTask(
    task: Task,
    title: string,
    dueDate?: string,
    dueTime?: string,
    description?: string,
    assignee?: string
) {
    const updated = await save(() =>
        api.tasks.update(task.id, {
            done: task.done,
            title,
            due_date: dueDate ?? null,
            due_time: dueTime ?? null,
            description: description ?? null,
            assignee: assignee ?? null,
        })
    );
    if (updated) taskStore.tasks = taskStore.tasks.map((t) => (t.id === task.id ? updated : t));
}

export async function removeTask(id: number, series = false) {
    // Only drop it from the list once the server has
    const deleted = await save(async () => {
        if (!(await api.tasks.delete(id, series))) throw new Error(`DELETE /tasks/${id} failed`);
        return true;
    });
    if (!deleted) return;
    if (series) {
        await loadTasks();
    } else {
        taskStore.tasks = taskStore.tasks.filter((t) => t.id !== id);
    }
}
