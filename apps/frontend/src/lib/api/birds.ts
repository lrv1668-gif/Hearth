import { get } from './http.ts';

export interface BirdSighting {
    species_code: string;
    common_name: string;
    scientific_name: string;
    location: string;
    observed_at: string; // "YYYY-MM-DD HH:MM" in the observation's local time
    count: number | null;
    distance_mi: number;
    is_notable: boolean;
}

export const birds = {
    recent: (): Promise<BirdSighting[] | null> => get<BirdSighting[]>('/birds/recent'),
};
