import { fetchRssArticles, type RssFeedGroup } from '$lib/api';
import { settings } from './SettingsStore.svelte';

export const rssStore = $state<{ groups: RssFeedGroup[]; loading: boolean }>({
    groups: [],
    loading: false,
});

function toTimestamp(s: string | null): number {
    if (!s) return 0;
    const d = new Date(s);
    return isNaN(d.getTime()) ? 0 : d.getTime();
}

function sortGroups(groups: RssFeedGroup[]): RssFeedGroup[] {
    return groups
        .map((group) => ({
            ...group,
            articles: [...group.articles].sort((a, b) => toTimestamp(b.published_at) - toTimestamp(a.published_at)),
        }))
        .sort(
            (a, b) =>
                toTimestamp(b.articles[0]?.published_at ?? null) - toTimestamp(a.articles[0]?.published_at ?? null)
        );
}

export async function loadRssArticles() {
    const urls = settings.rssFeeds.map((f) => f.url);
    if (urls.length === 0) {
        rssStore.groups = [];
        return;
    }
    rssStore.loading = true;
    rssStore.groups = sortGroups(await fetchRssArticles(urls, settings.rssArticleCount));
    rssStore.loading = false;
}
