import { api, type Quote } from '$lib/api';

class DailyQuoteStore {
    quote = $state<Quote | null>(null);
    error = $state(false);
}

export const dailyQuoteStore = new DailyQuoteStore();

export async function loadDailyQuote() {
    dailyQuoteStore.error = false;
    try {
        const quote = await api.quote.today();
        if (!quote) throw new Error('Failed to fetch');
        dailyQuoteStore.quote = quote;
    } catch {
        dailyQuoteStore.error = true;
    }
}
