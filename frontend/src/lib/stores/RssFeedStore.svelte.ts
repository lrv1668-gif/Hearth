import { fetchRssArticles, type RssArticle } from '$lib/api';

export const rssStore = $state<{ articles: RssArticle[]; loading: boolean }>({
    articles: [],
    loading: false,
});

export async function loadRssArticles(count: number) {
    rssStore.loading = true;
    rssStore.articles = await fetchRssArticles(count);
    rssStore.loading = false;
}
