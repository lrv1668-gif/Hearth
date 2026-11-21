import type { Photo } from '#lib/api.ts';
import type { FrameWidgetId } from '#lib/constants/frameWidgets.ts';
import { loadWeather } from '#lib/stores/WeatherStore.svelte.ts';
import { loadTasks } from '#lib/stores/TaskStore.svelte.ts';
import { loadCalendarItems } from '#lib/stores/CalendarStore.svelte.ts';
import { loadAlmanac } from '#lib/stores/AlmanacStore.svelte.ts';
import { loadDailyQuote } from '#lib/stores/DailyQuoteStore.svelte.ts';
import { loadBirds } from '#lib/stores/BirdsStore.svelte.ts';
import { loadLastPlayed } from '#lib/stores/LastPlayedStore.svelte.ts';
import { loadSports } from '#lib/stores/SportsStore.svelte.ts';
import { loadFramePhoto } from '#lib/stores/FramePhotoStore.svelte.ts';
import { loadHeadlines } from '#lib/stores/HeadlinesStore.svelte.ts';
import { loadTimetable } from '#lib/stores/TimetableStore.svelte.ts';
import { loadOnThisDay } from '#lib/stores/OnThisDayStore.svelte.ts';

/** `photo` is the frame URL's photo, for the Photo widget (see loadFramePhoto). */
type WidgetLoader = (photo: Photo | null) => Promise<void>;

/** The extra data each Daily widget needs; null for one that only uses what Daily
 *  always loads (weather, tasks, calendar and the almanac). Every widget has an entry,
 *  so a new one can't be registered in frameWidgets.ts without deciding its loader.
 *  Kept apart from frameWidgets.ts so Settings' editors don't pull in every store. */
export const frameWidgetLoaders = {
    arc: null,
    today: null,
    'coming-up': null,
    week: null,
    countdowns: null,
    almanac: null,
    moon: null,
    quote: loadDailyQuote,
    birds: loadBirds,
    'last-played': loadLastPlayed,
    sports: loadSports,
    photo: loadFramePhoto,
    headlines: loadHeadlines,
    timetable: loadTimetable,
    'on-this-day': loadOnThisDay,
} satisfies Record<FrameWidgetId, WidgetLoader | null>;

/** Everything Daily needs for `widgets`: the masthead, edition line, sun path and
 *  forecast always use weather, tasks, calendar and the almanac. */
export function dailyLoads(widgets: readonly FrameWidgetId[], photo: Photo | null): Promise<unknown>[] {
    const extras = widgets.flatMap((id) => frameWidgetLoaders[id] ?? []);
    return [loadWeather(), loadTasks(), loadCalendarItems(), loadAlmanac(), ...extras.map((load) => load(photo))];
}
