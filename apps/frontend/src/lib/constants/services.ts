// Hearth's backend services, in the order Settings → Diagnostics lists them. Each answers
// GET /<id>/health (ServiceDefaults' MapHearthHealth).
export const hearthServices = [
    { id: 'tasks', label: 'Tasks & countdowns' },
    { id: 'calendar', label: 'Calendar' },
    { id: 'weather', label: 'Weather' },
    { id: 'almanac', label: 'Almanac' },
    { id: 'birds', label: 'Birds' },
    { id: 'trains', label: 'Trains & buses' },
    { id: 'sports', label: 'Sports' },
    { id: 'rss', label: 'News feeds' },
    { id: 'quote', label: 'Quote' },
    { id: 'spotify', label: 'Spotify' },
    { id: 'photos', label: 'Photos' },
    { id: 'display', label: 'Display & frame' },
] as const;

export type HearthServiceId = (typeof hearthServices)[number]['id'];
