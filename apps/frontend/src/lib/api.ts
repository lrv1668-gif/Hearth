// The client for Hearth's backend services: one namespace per service, each in
// api/<service>.ts with its response types. Import from here, not from api/.
import { tasks } from './api/tasks.ts';
import { spotify } from './api/spotify.ts';
import { display } from './api/display.ts';
import { weather } from './api/weather.ts';
import { birds } from './api/birds.ts';
import { rss } from './api/rss.ts';
import { history } from './api/history.ts';
import { trains } from './api/trains.ts';
import { sports } from './api/sports.ts';
import { almanac } from './api/almanac.ts';
import { quote } from './api/quote.ts';
import { calendar } from './api/calendar.ts';
import { photos } from './api/photos.ts';
import { health, logs } from './api/health.ts';

export type * from './api/tasks.ts';
export type * from './api/spotify.ts';
export type * from './api/display.ts';
export { HomeSettingsRejected } from './api/display.ts';
export type * from './api/weather.ts';
export type * from './api/birds.ts';
export type * from './api/rss.ts';
export type * from './api/history.ts';
export type * from './api/trains.ts';
export type * from './api/sports.ts';
export type * from './api/almanac.ts';
export type * from './api/quote.ts';
export type * from './api/calendar.ts';
export type * from './api/photos.ts';
export type * from './api/health.ts';

export const api = {
    tasks,
    spotify,
    display,
    weather,
    birds,
    rss,
    history,
    trains,
    sports,
    almanac,
    quote,
    calendar,
    photos,
    logs,
    health,
};
