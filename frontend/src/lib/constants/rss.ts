export interface RssFeed {
    title: string;
    url: string;
}

export const DEFAULT_RSS_FEEDS: RssFeed[] = [
    {
        title: 'The Verge',
        url: 'https://www.theverge.com/rss/index.xml',
    },
];
