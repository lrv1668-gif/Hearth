import { get } from './http.ts';

export interface Quote {
    q: string;
    a: string;
}

export const quote = {
    today: (): Promise<Quote | null> => get<Quote>('/quote'),
};
