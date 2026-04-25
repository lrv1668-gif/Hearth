export interface Task {
    id: number;
    title: string;
    done: boolean;
    due_date: string | null;
    created_at: string;
}

export async function fetchTasks(): Promise<Task[]> {
    const res = await fetch('/tasks');
    return res.json();
}

export async function createTask(title: string, due_date?: string): Promise<Task> {
    const res = await fetch('/tasks', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, due_date: due_date ?? null }),
    });
    return res.json();
}

export async function updateTask(id: number, done: boolean): Promise<Task> {
    const res = await fetch(`/tasks/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ done }),
    });
    return res.json();
}

export async function deleteTask(id: number): Promise<void> {
    await fetch(`/tasks/${id}`, { method: 'DELETE' });
}
