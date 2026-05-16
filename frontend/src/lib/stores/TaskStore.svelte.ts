import { api, type Task } from '../api';

export const taskStore = $state({ tasks: [] as Task[] });

export async function loadTasks() {
    taskStore.tasks = await api.tasks.list();
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
    await api.tasks.delete(id, series);
    if (series) {
        await loadTasks();
    } else {
        taskStore.tasks = taskStore.tasks.filter((t) => t.id !== id);
    }
}
