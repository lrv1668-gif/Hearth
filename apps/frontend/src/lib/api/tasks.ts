import { del, get, post, put } from './http.ts';

export interface Task {
    id: number;
    title: string;
    done: boolean;
    due_date: string | null;
    due_time: string | null;
    created_at: string;
    description: string | null;
    assignee: string | null;
    recurrence_unit: string | null;
    recurrence_interval: number | null;
    recurrence_days: string | null;
    recurrence_end_date: string | null;
    series_id: number | null;
    is_countdown: boolean;
}

export interface CreateTaskInput {
    title: string;
    due_date?: string | null;
    due_time?: string | null;
    description?: string | null;
    assignee?: string | null;
    recurrence_unit?: string | null;
    recurrence_interval?: number | null;
    recurrence_days?: string | null;
    recurrence_end_date?: string | null;
    is_countdown?: boolean;
}

export interface UpdateTaskInput {
    done: boolean;
    title: string;
    due_date?: string | null;
    due_time?: string | null;
    description?: string | null;
    assignee?: string | null;
}

export const tasks = {
    list: (): Promise<Task[] | null> => get<Task[]>('/tasks'),

    create: (input: CreateTaskInput): Promise<Task> => post<Task>('/tasks', input),

    update: (id: number, input: UpdateTaskInput): Promise<Task> => put<Task>(`/tasks/${id}`, input),

    delete: (id: number, series = false): Promise<boolean> => del(`/tasks/${id}${series ? '?series=true' : ''}`),
};
