import { writable } from 'svelte/store';
import { fetchTasks, createTask, updateTask, deleteTask, type Task } from './api';

export const tasks = writable<Task[]>([]);

export async function loadTasks() {
    tasks.set(await fetchTasks());
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
) {
    const task = await createTask(
        title,
        dueDate,
        dueTime,
        description,
        assignee,
        recurrenceUnit,
        recurrenceInterval,
        recurrenceDays,
        recurrenceEndDate,
    );
    tasks.update((ts) => [task, ...ts]);
}

export async function toggleTask(task: Task) {
    const updated = await updateTask(
        task.id, !task.done, task.title,
        task.due_date ?? undefined, task.due_time ?? undefined,
        task.description ?? undefined, task.assignee ?? undefined,
    );
    tasks.update((ts) => ts.map((t) => (t.id === task.id ? updated : t)));
}

export async function editTask(
    task: Task,
    title: string,
    dueDate?: string,
    dueTime?: string,
    description?: string,
    assignee?: string,
) {
    const updated = await updateTask(task.id, task.done, title, dueDate, dueTime, description, assignee);
    tasks.update((ts) => ts.map((t) => (t.id === task.id ? updated : t)));
}

export async function removeTask(id: number, series = false) {
    await deleteTask(id, series);
    if (series) {
        await loadTasks();
    } else {
        tasks.update((ts) => ts.filter((t) => t.id !== id));
    }
}
