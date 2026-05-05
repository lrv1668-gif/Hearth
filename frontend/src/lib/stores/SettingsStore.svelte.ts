import { browser } from '$app/environment';
import type { PhotoCategory } from '$lib/constants/photos';
import {
    DEFAULT_ENABLED_WIDGETS_IDS,
    DEFAULT_WIDGET_COLUMNS,
    type WidgetId,
    type AllWidgetId,
} from '$lib/constants/widgets';

export interface Settings {
    cadenceSeconds: number;
    photoCategories: PhotoCategory[];
    showAttribution: boolean;
    enabledWidgets: WidgetId[];
    widgetColumns: { left: AllWidgetId[]; right: AllWidgetId[] };
    leftColumnWidth: number;
}

const STORAGE_KEY = 'hearth-settings';

const DEFAULT_SETTINGS: Settings = {
    cadenceSeconds: 120,
    photoCategories: ['nature', 'architecture'],
    showAttribution: true,
    enabledWidgets: DEFAULT_ENABLED_WIDGETS_IDS,
    widgetColumns: DEFAULT_WIDGET_COLUMNS,
    leftColumnWidth: 60,
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

function save() {
    if (browser) localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
}

export const settings = $state<Settings>(loadSettings());

export function updateCadence(seconds: number) {
    settings.cadenceSeconds = seconds;
    save();
}

export function toggleCategory(cat: PhotoCategory) {
    settings.photoCategories = settings.photoCategories.includes(cat)
        ? settings.photoCategories.filter((c) => c !== cat)
        : [...settings.photoCategories, cat];
    save();
}

export function toggleWidget(id: WidgetId) {
    settings.enabledWidgets = settings.enabledWidgets.includes(id)
        ? settings.enabledWidgets.filter((w) => w !== id)
        : [...settings.enabledWidgets, id];
    save();
}

export function toggleAttribution() {
    settings.showAttribution = !settings.showAttribution;
    save();
}

export function reorderWidgetColumns(columns: { left: AllWidgetId[]; right: AllWidgetId[] }) {
    settings.widgetColumns = columns;
    save();
}

export function updateColumnWidth(pct: number) {
    settings.leftColumnWidth = pct;
    save();
}
