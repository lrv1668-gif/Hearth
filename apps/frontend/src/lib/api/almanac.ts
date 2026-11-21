import { get } from './http.ts';

export interface AlmanacSeason {
    name: string; // "spring" | "summer" | "autumn" | "winter"
    label: string; // "Early summer", "Midsummer", ...
    day_of_season: number;
    total_days: number;
    progress: number; // 0–1
    next_marker: string; // "Autumn equinox", ...
    next_marker_date: string; // "YYYY-MM-DD"
    days_until_marker: number;
}

export interface AlmanacMilestone {
    label: string; // "Last 8 pm sunset", ...
    date: string; // "YYYY-MM-DD"
}

export interface AlmanacDaylight {
    trend_minutes_per_day: number; // negative = losing light; 0 = holding steady
    drift_minutes: number; // vs. the nearest solstice extreme
    drift_reference: string; // "longest day" | "shortest day"
    milestones: AlmanacMilestone[];
}

export interface AlmanacFrost {
    label: string; // "First frost" | "Last frost"
    date: string; // "YYYY-MM-DD"
    days_until: number;
}

export interface AlmanacResponse {
    season: AlmanacSeason;
    daylight: AlmanacDaylight | null; // null when the service has no coordinates
    frost: AlmanacFrost | null; // null when frost dates are not configured
    note: string | null; // seasonal phenology note; null for southern hemisphere
}

export const almanac = {
    today: (): Promise<AlmanacResponse | null> => get<AlmanacResponse>('/almanac'),
};
