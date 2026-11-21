import { get } from './http.ts';

export interface RssArticle {
    title: string;
    link: string;
    description: string | null;
    published_at: string | null;
}

export interface RssFeedGroup {
    feed_title: string;
    feed_url: string;
    articles: RssArticle[];
}

/** One story on the frame's Headlines, picked once per clock hour by the Rss service. */
export interface Headline {
    title: string;
    feed_title: string;
    published_at: string | null;
}

export const rss = {
    articles: (urls: string[], count: number): Promise<RssFeedGroup[] | null> => {
        const params = new URLSearchParams({ count: String(count) });
        urls.forEach((url) => params.append('url', url));
        return get<RssFeedGroup[]>(`/rss/articles?${params}`);
    },

    /** The newest stories across `urls`, held for the clock hour so the panel doesn't redraw for each. */
    headlines: (urls: string[], count: number): Promise<Headline[] | null> => {
        const params = new URLSearchParams({ count: String(count) });
        urls.forEach((url) => params.append('url', url));
        return get<Headline[]>(`/rss/headlines?${params}`, { timeoutMs: 15000 });
    },
};
