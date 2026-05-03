import { browser } from '$app/environment';
import { writable } from 'svelte/store';
import { ALL_WIDGET_IDS, type WidgetId } from '$lib/constants/widgets';

export type PhotoCategory = 'nature' | 'architecture' | 'interiors' | 'abstract';

export interface Settings {
    cadenceSeconds: number;
    photoCategories: PhotoCategory[];
    showAttribution: boolean;
    enabledWidgets: WidgetId[];
}

const STORAGE_KEY = 'hearth-settings';

const DEFAULT_SETTINGS: Settings = {
    cadenceSeconds: 120,
    photoCategories: ['nature', 'architecture'],
    showAttribution: true,
    enabledWidgets: ALL_WIDGET_IDS,
};

function loadSettings(): Settings {
    if (!browser) return DEFAULT_SETTINGS;
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        return stored ? { ...DEFAULT_SETTINGS, ...JSON.parse(stored) } : DEFAULT_SETTINGS;
    } catch {
        return DEFAULT_SETTINGS;
    }
}

export const settings = writable<Settings>(loadSettings());

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

export function toggleWidget(id: WidgetId) {
    settings.update((s) => ({
        ...s,
        enabledWidgets: s.enabledWidgets.includes(id)
            ? s.enabledWidgets.filter((w) => w !== id)
            : [...s.enabledWidgets, id],
    }));
}
