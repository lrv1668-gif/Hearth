export const toggleableWidgets = [
    { id: 'weather', label: 'Weather', description: 'Current conditions and 5-day forecast' },
    { id: 'countdowns', label: 'Countdowns', description: 'Upcoming countdown events' },
    { id: 'moon-phase', label: 'Moon Phase', description: 'Current lunar phase and next major phase' },
    { id: 'now-playing', label: 'Now Playing', description: "What's currently playing on Spotify" },
    { id: 'rss-feeds', label: "Today's News", description: 'Latest articles from The Verge' },
    { id: 'daily-quote', label: 'Daily Quote', description: 'An inspiring quote that changes each day' },
] as const;

export type WidgetId = (typeof toggleableWidgets)[number]['id'];
export const DEFAULT_ENABLED_WIDGETS_IDS: WidgetId[] = toggleableWidgets.map((w) => w.id);

export const allWidgets = [
    { id: 'upcoming-tasks', label: 'Upcoming Tasks' },
    { id: 'todays-date', label: 'Time & Date' },
    { id: 'weather', label: 'Weather' },
    { id: 'countdowns', label: 'Countdowns' },
    { id: 'moon-phase', label: 'Moon Phase' },
    { id: 'now-playing', label: 'Now Playing' },
    { id: 'rss-feeds', label: 'News' },
    { id: 'daily-quote', label: 'Daily Quote' },
] as const;

export type AllWidgetId = (typeof allWidgets)[number]['id'];

export const DEFAULT_WIDGET_COLUMNS: { left: AllWidgetId[]; right: AllWidgetId[] } = {
    left: ['upcoming-tasks', 'todays-date', 'countdowns', 'now-playing'],
    right: ['weather', 'moon-phase', 'daily-quote', 'rss-feeds'],
};
