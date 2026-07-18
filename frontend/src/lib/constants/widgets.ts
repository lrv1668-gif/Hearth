export const toggleableWidgets = [
    { id: 'weather', label: 'Weather', description: 'Current conditions and 5-day forecast' },
    { id: 'countdowns', label: 'Countdowns', description: 'Upcoming countdown events' },
    { id: 'moon-phase', label: 'Moon Phase', description: 'Current lunar phase and next major phase' },
    { id: 'rss-feeds', label: "Today's News", description: 'Latest articles from your RSS feeds' },
    { id: 'daily-quote', label: 'Daily Quote', description: 'An inspiring quote that changes each day' },
    { id: 'birds', label: 'Birds Nearby', description: 'Recent bird sightings around your location, from eBird' },
] as const;

export type WidgetId = (typeof toggleableWidgets)[number]['id'];
export const DEFAULT_ENABLED_WIDGETS_IDS: WidgetId[] = toggleableWidgets.map((w) => w.id);

// Widgets that can be placed in columns. Does not include widgets fixed in the
// header (time/date, current conditions) or footer (now playing, refresh time).
export const allWidgets = [
    { id: 'upcoming-tasks', label: 'Agenda' },
    { id: 'weather', label: 'Weather' },
    { id: 'countdowns', label: 'Countdowns' },
    { id: 'moon-phase', label: 'Moon Phase' },
    { id: 'rss-feeds', label: "Today's News" },
    { id: 'daily-quote', label: 'Daily Quote' },
    { id: 'birds', label: 'Birds Nearby' },
] as const;

export type AllWidgetId = (typeof allWidgets)[number]['id'];

export const DEFAULT_WIDGET_COLUMNS: { left: AllWidgetId[]; right: AllWidgetId[] } = {
    left: ['upcoming-tasks'],
    right: ['weather', 'rss-feeds', 'countdowns', 'moon-phase', 'daily-quote', 'birds'],
};

// Items always visible in the header and footer — not configurable.
export const fixedHeaderWidgets = [
    { label: 'Time & Date' },
    { label: 'Current Conditions' },
] as const;

export const fixedFooterWidgets = [
    { label: 'Now Playing' },
    { label: 'Refresh Time' },
] as const;
