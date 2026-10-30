import { api, type Quote } from '#lib/api.ts';

class DailyQuoteStore {
    quote = $state<Quote | null>(null);
    error = $state(false);
}

export const dailyQuoteStore = new DailyQuoteStore();

export async function loadDailyQuote() {
    try {
        const quote = await api.quote.today();
        if (!quote) throw new Error('Failed to fetch');
        dailyQuoteStore.error = false;
        dailyQuoteStore.quote = quote;
    } catch {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last quote.
        dailyQuoteStore.error = dailyQuoteStore.quote === null;
    }
}
