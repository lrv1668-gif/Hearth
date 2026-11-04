import { api, type Headline } from '#lib/api.ts';
import { ensureHousehold, household } from './HouseholdStore.svelte.ts';

/** The most the frame's Headlines shows — an L slot's lead story plus its list. */
export const HEADLINE_COUNT = 8;

class HeadlinesStore {
    headlines = $state<Headline[]>([]);
    error = $state(false);
}

export const headlinesStore = new HeadlinesStore();

export async function loadHeadlines() {
    await ensureHousehold();
    const urls = household.rssFeeds.map((f) => f.url);
    if (urls.length === 0) {
        headlinesStore.headlines = [];
        headlinesStore.error = false;
        return;
    }
    const headlines = await api.rss.headlines(urls, HEADLINE_COUNT);
    if (headlines === null) {
        // Only surface the error when there's nothing to show; a failed refresh keeps the last edition.
        headlinesStore.error = headlinesStore.headlines.length === 0;
        return;
    }
    headlinesStore.error = false;
    headlinesStore.headlines = headlines;
}
