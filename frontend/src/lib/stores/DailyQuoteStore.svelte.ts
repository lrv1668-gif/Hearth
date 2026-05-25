export interface ZenQuote {
    q: string;
    a: string;
}

class DailyQuoteStore {
    quote = $state<ZenQuote | null>(null);
    error = $state(false);
    fetchedDate = $state<string | null>(null);
}

export const dailyQuoteStore = new DailyQuoteStore();

export async function loadDailyQuote() {
    const today = new Date().toISOString().slice(0, 10);
    if (dailyQuoteStore.fetchedDate === today) return;

    dailyQuoteStore.error = false;
    try {
        const res = await fetch('https://zenquotes.io/api/today');
        if (!res.ok) throw new Error('Failed to fetch');
        const data: ZenQuote[] = await res.json();
        dailyQuoteStore.quote = data[0] ?? null;
        dailyQuoteStore.fetchedDate = today;
    } catch {
        dailyQuoteStore.error = true;
    }
}
