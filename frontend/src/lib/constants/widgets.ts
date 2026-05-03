export const widgets = [
    { id: 'weather', label: 'Weather', description: 'Current conditions and 5-day forecast' },
    { id: 'countdowns', label: 'Countdowns', description: 'Upcoming countdown events' },
    { id: 'moon-phase', label: 'Moon Phase', description: 'Current lunar phase and next major phase' },
    { id: 'now-playing', label: 'Now Playing', description: "What's currently playing on Spotify" },
] as const;

export type WidgetId = (typeof widgets)[number]['id'];

export const ALL_WIDGET_IDS = widgets.map((widget) => widget.id);
