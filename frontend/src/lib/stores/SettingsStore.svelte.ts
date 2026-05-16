import { browser } from '$app/environment';
import type { PhotoCategory, PhotoSource } from '$lib/constants/photos';
import {
    DEFAULT_ENABLED_WIDGETS_IDS,
    DEFAULT_WIDGET_COLUMNS,
    type WidgetId,
    type AllWidgetId,
} from '$lib/constants/widgets';

const STORAGE_KEY = 'hearth-settings';

class SettingsStore {
    cadenceSeconds  = $state(120);
    photoCategories = $state<PhotoCategory[]>(['nature', 'architecture']);
    photoSource     = $state<PhotoSource>('unsplash');
    showAttribution = $state(true);
    enabledWidgets  = $state<WidgetId[]>(DEFAULT_ENABLED_WIDGETS_IDS);
    widgetColumns   = $state<{ left: AllWidgetId[]; right: AllWidgetId[] }>(DEFAULT_WIDGET_COLUMNS);
    leftColumnWidth = $state(60);

    toggleCategory(cat: PhotoCategory) {
        this.photoCategories = this.photoCategories.includes(cat)
            ? this.photoCategories.filter((c) => c !== cat)
            : [...this.photoCategories, cat];
    }

    toggleWidget(id: WidgetId) {
        this.enabledWidgets = this.enabledWidgets.includes(id)
            ? this.enabledWidgets.filter((w) => w !== id)
            : [...this.enabledWidgets, id];
    }
}

function createSettings(): SettingsStore {
    const store = new SettingsStore();
    if (!browser) return store;
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (raw) Object.assign(store, JSON.parse(raw));
    } catch {
        // fall back to defaults
    }
    return store;
}

export const settings = createSettings();

// Auto-persist any mutation to localStorage
if (browser) {
    $effect.root(() => {
        $effect(() => {
            localStorage.setItem(
                STORAGE_KEY,
                JSON.stringify({
                    cadenceSeconds:  settings.cadenceSeconds,
                    photoCategories: settings.photoCategories,
                    photoSource:     settings.photoSource,
                    showAttribution: settings.showAttribution,
                    enabledWidgets:  settings.enabledWidgets,
                    widgetColumns:   settings.widgetColumns,
                    leftColumnWidth: settings.leftColumnWidth,
                }),
            );
        });
    });
}
