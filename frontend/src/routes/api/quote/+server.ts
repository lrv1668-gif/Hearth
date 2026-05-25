import { json, error } from '@sveltejs/kit';
import type { RequestHandler } from './$types';

/**
 * Handler to retrieve today's quote from ZenQuotes to avoid CORS issues.
 * @returns
 */
export const GET: RequestHandler = async () => {
    const res = await fetch('https://zenquotes.io/api/today');
    if (!res.ok) error(502, 'Failed to fetch quote');
    const data = await res.json();
    return json(data);
};
