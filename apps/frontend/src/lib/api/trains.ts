import { get } from './http.ts';

export interface TrainDeparture {
    route_short_name: string;
    route_long_name: string | null;
    route_type: number; // GTFS numeric mode: 0=tram,1=subway,2=rail,3=bus,4=ferry,...
    mode: string; // human-readable mode derived from route_type, e.g. "bus", "rail"
    headsign: string | null;
    scheduled_departure: string | null;
    estimated_departure: string | null;
    is_realtime: boolean;
}

/** The frame's Scheduled Departures for one stop: timetabled times for the hour and the next. */
export interface StopTimetable {
    stop_key: string;
    stop_name: string | null;
    /** ISO UTC: the top of the hour this edition belongs to, and two hours later. */
    window_start: string;
    window_end: string;
    lines: TimetableLine[];
}

export interface TimetableLine {
    route_short_name: string;
    headsign: string | null;
    mode: string;
    /** Scheduled departure instants, ISO UTC, in order. */
    departures: string[];
}

export interface StopDepartures {
    stop_key: string;
    stop_name: string | null;
    departures: TrainDeparture[];
}

export const trains = {
    departures: (stopKeys: string[]): Promise<StopDepartures[] | null> => {
        const params = new URLSearchParams();
        stopKeys.forEach((key) => params.append('stop', key));
        return get<StopDepartures[]>(`/trains/departures?${params}`);
    },

    /** Timetabled departures to the end of the next hour, held for the clock hour. */
    timetable: (stopKeys: string[]): Promise<StopTimetable[] | null> => {
        const params = new URLSearchParams();
        stopKeys.forEach((key) => params.append('stop', key));
        return get<StopTimetable[]>(`/trains/timetable?${params}`, { timeoutMs: 15000 });
    },
};
