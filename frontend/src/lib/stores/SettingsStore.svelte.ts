import { browser } from '$app/environment';
import { DEFAULT_RSS_FEEDS, type RssFeed } from '$lib/constants/rss';
import { DEFAULT_TRAIN_STOPS, type TrainStop } from '$lib/constants/trains';
import { LEGACY_FRAME_SETTINGS_KEY } from '$lib/stores/FrameSettingsStore.svelte.ts';
import {
    ALL_WIDGET_IDS,
    DEFAULT_ENABLED_WIDGETS_IDS,
    DEFAULT_WIDGET_COLUMNS,
    type WidgetId,
    type AllWidgetId,
} from '$lib/constants/widgets';
import { mergeNewWidgets } from '$lib/widgetLayout';

const STORAGE_KEY = 'hearth-settings';

class SettingsStore {
    cadenceSeconds = $state(120);
    nightCadenceSeconds = $state<number | null>(null); // null = same as day
    ambientMotion = $state(true);
    enabledWidgets = $state<WidgetId[]>(DEFAULT_ENABLED_WIDGETS_IDS);
    widgetColumns = $state<{ left: AllWidgetId[]; right: AllWidgetId[] }>(DEFAULT_WIDGET_COLUMNS);
    leftColumnWidth = $state(60);
    rssArticleCount = $state(5);
    rssFeeds = $state<RssFeed[]>(DEFAULT_RSS_FEEDS);
    trainStops = $state<TrainStop[]>(DEFAULT_TRAIN_STOPS);
    kioskMode = $state(false);

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
            const {
                frameTheme,
                frameFont,
                frameOrientation,
                frameLayout,
                frameSplit,
                photoSource,
                photoCategories,
                showAttribution: _attribution, // dropped: Unsplash credit is always shown
                knownWidgetIds,
                ...rest
            } = JSON.parse(raw);
            Object.assign(store, rest);
            // Frame and photo settings now live in the Display service
            // (FrameSettingsStore). Park any from an older snapshot so they can be
            // uploaded once, before this store's next save drops them.
            const hasLegacy = frameTheme !== undefined || photoSource !== undefined;
            if (hasLegacy && localStorage.getItem(LEGACY_FRAME_SETTINGS_KEY) === null) {
                localStorage.setItem(
                    LEGACY_FRAME_SETTINGS_KEY,
                    JSON.stringify({
                        theme: frameTheme,
                        font: frameFont,
                        orientation: frameOrientation,
                        left: frameLayout?.left,
                        right: frameLayout?.right,
                        split: frameSplit,
                        photo_source: photoSource,
                        photo_categories: photoCategories,
                    })
                );
            }

            // Merge any widgets added after this settings snapshot was saved. Snapshots from
            // before knownWidgetIds re-merged every default on each load, so they already
            // hold every current widget.
            const merged = mergeNewWidgets(
                { columns: store.widgetColumns, enabled: store.enabledWidgets },
                Array.isArray(knownWidgetIds) ? knownWidgetIds : ALL_WIDGET_IDS,
                { columns: DEFAULT_WIDGET_COLUMNS, enabled: DEFAULT_ENABLED_WIDGETS_IDS }
            );
            store.widgetColumns = merged.columns;
            store.enabledWidgets = merged.enabled;
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
                    enabledWidgets: settings.enabledWidgets,
                    widgetColumns: settings.widgetColumns,
                    // Lets the next load tell a widget turned off from one released since
                    knownWidgetIds: ALL_WIDGET_IDS,
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
