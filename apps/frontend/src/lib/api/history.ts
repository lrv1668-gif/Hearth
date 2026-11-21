import { get } from './http.ts';

/** Something that happened on today's date, from Wikipedia's "On this day" (CC BY-SA). */
export interface HistoryEvent {
    /** Negative for BC. */
    year: number;
    text: string;
    page_title: string | null;
}

export interface OnThisDay {
    /** The household's date the events are for, yyyy-MM-dd. */
    date: string;
    /** Spread across the centuries, oldest first. */
    events: HistoryEvent[];
}

export const history = {
    /** Today's events; null when the History service can't be reached or has none. */
    onThisDay: (): Promise<OnThisDay | null> => get<OnThisDay>('/history/onthisday', { timeoutMs: 10000 }),
};
