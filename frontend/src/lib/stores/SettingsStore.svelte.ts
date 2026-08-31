import { browser } from '$app/environment';
import type { PhotoCategory, PhotoSource } from '$lib/constants/photos';
import { DEFAULT_RSS_FEEDS, type RssFeed } from '$lib/constants/rss';
import { DEFAULT_TRAIN_STOPS, type TrainStop } from '$lib/constants/trains';
import {
    DEFAULT_ENABLED_WIDGETS_IDS,
    DEFAULT_WIDGET_COLUMNS,
    type WidgetId,
    type AllWidgetId,
} from '$lib/constants/widgets';

const STORAGE_KEY = 'hearth-settings';

class SettingsStore {
    cadenceSeconds = $state(120);
    nightCadenceSeconds = $state<number | null>(null); // null = same as day
    ambientMotion = $state(true);
    photoCategories = $state<PhotoCategory[]>(['nature', 'architecture']);
    photoSource = $state<PhotoSource>('unsplash');
    showAttribution = $state(true);
    enabledWidgets = $state<WidgetId[]>(DEFAULT_ENABLED_WIDGETS_IDS);
    widgetColumns = $state<{ left: AllWidgetId[]; right: AllWidgetId[] }>(DEFAULT_WIDGET_COLUMNS);
    leftColumnWidth = $state(60);
    rssArticleCount = $state(5);
    rssFeeds = $state<RssFeed[]>(DEFAULT_RSS_FEEDS);
    trainStops = $state<TrainStop[]>(DEFAULT_TRAIN_STOPS);
    kioskMode = $state(false);

    toggleCategory(cat: PhotoCategory) {
        this.photoCategories = this.photoCategories.includes(cat)
            ? this.photoCategories.filter((c) => c !== cat)
            : [...this.photoCategories, cat];
    }

    toggleWidget(id: WidgetId) {
        const inColumns =
            this.widgetColumns.left.includes(id as AllWidgetId) || this.widgetColumns.right.includes(id as AllWidgetId);

        if (inColumns) {
            this.widgetColumns = {
                left: this.widgetColumns.left.filter((w) => w !== id),
                right: this.widgetColumns.right.filter((w) => w !== id),
            };
            this.enabledWidgets = this.enabledWidgets.filter((w) => w !== id);
        } else {
            const defaultCol = DEFAULT_WIDGET_COLUMNS.left.includes(id as AllWidgetId) ? 'left' : 'right';
            this.widgetColumns = {
                ...this.widgetColumns,
                [defaultCol]: [...this.widgetColumns[defaultCol], id as AllWidgetId],
            };
            this.enabledWidgets = [...this.enabledWidgets, id];
        }
    }
}

function createSettings(): SettingsStore {
    const store = new SettingsStore();
    if (!browser) return store;
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (raw) {
            Object.assign(store, JSON.parse(raw));

            // Merge any widgets added after this settings snapshot was saved.
            const storedIds = new Set([...store.widgetColumns.left, ...store.widgetColumns.right]);
            for (const col of ['left', 'right'] as const) {
                for (const id of DEFAULT_WIDGET_COLUMNS[col]) {
                    if (!storedIds.has(id)) {
                        store.widgetColumns = {
                            ...store.widgetColumns,
                            [col]: [...store.widgetColumns[col], id],
                        };
                        storedIds.add(id);
                    }
                }
            }
            for (const id of DEFAULT_ENABLED_WIDGETS_IDS) {
                if (!store.enabledWidgets.includes(id)) {
                    store.enabledWidgets = [...store.enabledWidgets, id];
                }
            }
        }
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
                    cadenceSeconds: settings.cadenceSeconds,
                    nightCadenceSeconds: settings.nightCadenceSeconds,
                    ambientMotion: settings.ambientMotion,
                    photoCategories: settings.photoCategories,
                    photoSource: settings.photoSource,
                    showAttribution: settings.showAttribution,
                    enabledWidgets: settings.enabledWidgets,
                    widgetColumns: settings.widgetColumns,
                    leftColumnWidth: settings.leftColumnWidth,
                    rssArticleCount: settings.rssArticleCount,
                    rssFeeds: settings.rssFeeds,
                    trainStops: settings.trainStops,
                    kioskMode: settings.kioskMode,
                })
            );
        });
    });
}

export function updateRssArticleCount(n: number) {
    settings.rssArticleCount = n;
}

export function subscribeToRssFeed(title: string, url: string) {
    settings.rssFeeds = [...settings.rssFeeds, { title, url }];
}

export function unsubscribeFromRssFeed(title: string) {
    settings.rssFeeds = settings.rssFeeds.filter((rssFeed) => rssFeed.title != title);
}

export function subscribeToTrainStop(label: string, stopKey: string) {
    settings.trainStops = [...settings.trainStops, { label, stopKey }];
}

export function unsubscribeFromTrainStop(stopKey: string) {
    settings.trainStops = settings.trainStops.filter((stop) => stop.stopKey != stopKey);
}

export function updateTrainStopLineFilter(stopKey: string, lineFilter: string[] | undefined) {
    settings.trainStops = settings.trainStops.map((stop) =>
        stop.stopKey === stopKey ? { ...stop, lineFilter } : stop
    );
}
