export const toggleableWidgets = [
    { id: 'weather', label: 'Weather', description: 'Current conditions and 5-day forecast' },
    { id: 'countdowns', label: 'Countdowns', description: 'Upcoming countdown events' },
    { id: 'moon-phase', label: 'Moon Phase', description: 'Current lunar phase and next major phase' },
    { id: 'rss-feeds', label: "Today's News", description: 'Latest articles from your RSS feeds' },
    { id: 'daily-quote', label: 'Daily Quote', description: 'An inspiring quote that changes each day' },
    {
        id: 'day-arc',
        label: "Today's Arc",
        description: 'The shape of today — daylight, golden hour, and what is ahead',
    },
    { id: 'birds', label: 'Birds Nearby', description: 'Recent bird sightings around your location, from eBird' },
    {
        id: 'almanac',
        label: 'Almanac',
        description: 'Season progress, daylight trend, frost countdown, and seasonal notes',
    },
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
    { id: 'day-arc', label: "Today's Arc" },
    { id: 'birds', label: 'Birds Nearby' },
    { id: 'almanac', label: 'Almanac' },
] as const;

export type AllWidgetId = (typeof allWidgets)[number]['id'];

export const DEFAULT_WIDGET_COLUMNS: { left: AllWidgetId[]; right: AllWidgetId[] } = {
    left: ['day-arc', 'upcoming-tasks'],
    right: ['weather', 'rss-feeds', 'countdowns', 'moon-phase', 'almanac', 'daily-quote', 'birds'],
};

// Items always visible in the header and footer — not configurable.
export const fixedHeaderWidgets = [{ label: 'Date & Time' }, { label: 'Current Conditions' }] as const;

export const fixedFooterWidgets = [{ label: 'Now Playing' }, { label: 'Refresh Time' }] as const;
