import { fetchTasks, createTask, updateTask, deleteTask, type Task } from '../api';

export const taskStore = $state({ tasks: [] as Task[] });

export async function loadTasks() {
    taskStore.tasks = await fetchTasks();
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
        isCountdown
    );
    taskStore.tasks = [task, ...taskStore.tasks];
}

export async function toggleTask(task: Task) {
    const updated = await updateTask(
        task.id,
        !task.done,
        task.title,
        task.due_date ?? undefined,
        task.due_time ?? undefined,
        task.description ?? undefined,
        task.assignee ?? undefined
    );
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
    const updated = await updateTask(task.id, task.done, title, dueDate, dueTime, description, assignee);
    taskStore.tasks = taskStore.tasks.map((t) => (t.id === task.id ? updated : t));
}

export async function removeTask(id: number, series = false) {
    await deleteTask(id, series);
    if (series) {
        await loadTasks();
    } else {
        taskStore.tasks = taskStore.tasks.filter((t) => t.id !== id);
    }
}
