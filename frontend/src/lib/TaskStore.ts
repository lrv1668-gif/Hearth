import { writable } from 'svelte/store';
import { fetchTasks, createTask, updateTask, deleteTask, type Task } from './api';

export const tasks = writable<Task[]>([]);

export async function loadTasks() {
    tasks.set(await fetchTasks());
}

export async function addTask(title: string, dueDate?: string, dueTime?: string) {
    const task = await createTask(title, dueDate, dueTime);
    tasks.update(ts => [task, ...ts]);
}

export async function toggleTask(task: Task) {
    const updated = await updateTask(task.id, !task.done);
    tasks.update(ts => ts.map(t => (t.id === task.id ? updated : t)));
}

export async function removeTask(id: number) {
    await deleteTask(id);
    tasks.update(ts => ts.filter(t => t.id !== id));
}
