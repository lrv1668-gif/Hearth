import { browser } from '$app/environment';
import type { PhotoCategory } from '$lib/constants/photos';
import { DEFAULT_RSS_FEEDS, type RssFeed } from '$lib/constants/rss';
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
    rssArticleCount: number;
    rssFeeds: RssFeed[];
}

const STORAGE_KEY = 'hearth-settings';

const DEFAULT_SETTINGS: Settings = {
    cadenceSeconds: 120,
    photoCategories: ['nature', 'architecture'],
    showAttribution: true,
    enabledWidgets: DEFAULT_ENABLED_WIDGETS_IDS,
    widgetColumns: DEFAULT_WIDGET_COLUMNS,
    leftColumnWidth: 60,
    rssArticleCount: 5,
    rssFeeds: DEFAULT_RSS_FEEDS,
};

function loadSettings(): Settings {
    if (!browser) return DEFAULT_SETTINGS;
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (!stored) return DEFAULT_SETTINGS;

        const parsed = JSON.parse(stored) as Partial<Settings>;
        const merged: Settings = { ...DEFAULT_SETTINGS, ...parsed };

        // Add new default widgets missing from stored columns (new widgets added after initial save)
        if (parsed.widgetColumns) {
            const known = new Set<AllWidgetId>([...parsed.widgetColumns.left, ...parsed.widgetColumns.right]);
            merged.widgetColumns = {
                left: [...parsed.widgetColumns.left, ...DEFAULT_WIDGET_COLUMNS.left.filter((id) => !known.has(id))],
                right: [...parsed.widgetColumns.right, ...DEFAULT_WIDGET_COLUMNS.right.filter((id) => !known.has(id))],
            };
        }

        // Enable any new toggleable widgets not present in stored list
        if (parsed.enabledWidgets) {
            const known = new Set<WidgetId>(parsed.enabledWidgets);
            merged.enabledWidgets = [
                ...parsed.enabledWidgets,
                ...DEFAULT_ENABLED_WIDGETS_IDS.filter((id) => !known.has(id)),
            ];
        }

        return merged;
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

export function updateRssArticleCount(n: number) {
    settings.rssArticleCount = n;
    save();
}

export function subscribeToRssFeed(title: string, url: string) {
    settings.rssFeeds = [...settings.rssFeeds, { title, url }];
    save();
}

export function unsubscribeFromRssFeed(title: string) {
    settings.rssFeeds = settings.rssFeeds.filter((rssFeed) => rssFeed.title != title);
    save();
}
