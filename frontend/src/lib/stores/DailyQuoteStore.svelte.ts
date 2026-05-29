export interface ZenQuote {
    q: string;
    a: string;
}

class DailyQuoteStore {
    quote = $state<ZenQuote | null>(null);
    error = $state(false);
}

export const dailyQuoteStore = new DailyQuoteStore();

export async function loadDailyQuote() {
    dailyQuoteStore.error = false;
    try {
        const res = await fetch('/quote');
        if (!res.ok) throw new Error('Failed to fetch');
        dailyQuoteStore.quote = await res.json();
    } catch {
        dailyQuoteStore.error = true;
    }
}
