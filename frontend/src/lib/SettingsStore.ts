import { browser } from '$app/environment';
import { writable } from 'svelte/store';

export type PhotoCategory = 'nature' | 'architecture' | 'interiors' | 'abstract';

export interface AmbientSettings {
    cadenceSeconds: number;
    photoCategories: PhotoCategory[];
    showAttribution: boolean;
}

const STORAGE_KEY = 'hearth-settings';

const DEFAULT_SETTINGS: AmbientSettings = {
    cadenceSeconds: 120,
    photoCategories: ['nature', 'architecture'],
    showAttribution: true,
};

function loadSettings(): AmbientSettings {
    if (!browser) return DEFAULT_SETTINGS;
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        return stored ? { ...DEFAULT_SETTINGS, ...JSON.parse(stored) } : DEFAULT_SETTINGS;
    } catch {
        return DEFAULT_SETTINGS;
    }
}

export const settings = writable<AmbientSettings>(loadSettings());

if (browser) {
    settings.subscribe((value) => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    });
}

export function updateCadence(seconds: number) {
    settings.update((s) => ({ ...s, cadenceSeconds: seconds }));
}

export function toggleCategory(cat: PhotoCategory) {
    settings.update((s) => ({
        ...s,
        photoCategories: s.photoCategories.includes(cat)
            ? s.photoCategories.filter((c) => c !== cat)
            : [...s.photoCategories, cat],
    }));
}
