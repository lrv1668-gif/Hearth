import { del, get, patch, post, put } from './http.ts';

export interface Photo {
    id: string;
    url: string;
    thumb_url: string | null;
    description: string | null;
    photographer_name: string | null;
    unsplash_link: string | null;
    source: string;
}

export interface UploadedPhoto {
    id: string;
    url: string;
    thumb_url: string;
    caption: string | null;
}

/** A named set of uploads (Photos service), for picking several at once. */
export interface PhotoGroup {
    id: string;
    name: string;
    photo_ids: string[];
}

export interface BatchFileResult {
    file_name: string;
    status: 'ok' | 'duplicate' | 'error';
    error: string | null;
    photo: UploadedPhoto | null;
}

export const photos = {
    sources: (): Promise<string[]> => get<string[]>('/photos/sources').then((r) => r ?? ['unsplash']),

    random: (query: string, orientation: 'portrait' | 'landscape', source = 'unsplash'): Promise<Photo | null> => {
        const params = new URLSearchParams({ query, orientation, source });
        return get<Photo>(`/photos/random?${params}`);
    },

    list: (): Promise<UploadedPhoto[]> => get<UploadedPhoto[]>('/photos/uploads').then((r) => r ?? []),

    upload: async (files: File[]): Promise<BatchFileResult[]> => {
        const form = new FormData();
        for (const file of files) form.append('file', file);
        const res = await fetch('/photos/uploads', { method: 'POST', body: form });
        if (res.status === 413) {
            return files.map((f) => ({
                file_name: f.name,
                status: 'error' as const,
                error: 'batch too large — try uploading fewer at a time',
                photo: null,
            }));
        }
        if (!res.ok) {
            return files.map((f) => ({
                file_name: f.name,
                status: 'error' as const,
                error: 'upload failed',
                photo: null,
            }));
        }
        return res.json() as Promise<BatchFileResult[]>;
    },

    delete: (id: string): Promise<boolean> => del(`/photos/uploads/${id}`),

    setCaption: (id: string, caption: string | null): Promise<boolean> => patch(`/photos/uploads/${id}`, { caption }),

    groups: (): Promise<PhotoGroup[]> => get<PhotoGroup[]>('/photos/groups').then((r) => r ?? []),

    createGroup: (name: string, photoIds: string[]): Promise<PhotoGroup> =>
        post<PhotoGroup>('/photos/groups', { name, photo_ids: photoIds }),

    updateGroup: (id: string, name: string, photoIds: string[]): Promise<PhotoGroup> =>
        put<PhotoGroup>(`/photos/groups/${id}`, { name, photo_ids: photoIds }),

    deleteGroup: (id: string): Promise<boolean> => del(`/photos/groups/${id}`),
};
