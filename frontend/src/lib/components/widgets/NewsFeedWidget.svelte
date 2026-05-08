<script lang="ts">
    import { onMount } from 'svelte';
    import { Rss } from '@lucide/svelte';
    import { rssStore, loadRssArticles } from '$lib/stores/RssFeedStore.svelte.ts';

    function toTimestamp(s: string | null): number {
        if (!s) return 0;
        const d = new Date(s);
        return isNaN(d.getTime()) ? 0 : d.getTime();
    }

    function formatTime(iso: string | null): string {
        if (!iso) return '';
        const d = new Date(iso);
        if (isNaN(d.getTime())) return '';
        return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }

    const flatArticles = $derived(
        rssStore.groups
            .flatMap((group) => group.articles.map((article) => ({ ...article, feed_title: group.feed_title })))
            .sort((a, b) => toTimestamp(b.published_at) - toTimestamp(a.published_at))
    );

    onMount(() => loadRssArticles());
</script>

{#if rssStore.loading}
    <p class="type-label text-[var(--text-3)]">Loading articles…</p>
{:else if flatArticles.length === 0}
    <div class="flex items-center gap-2 text-[var(--text-3)]">
        <Rss class="icon-sm" />
        <p class="type-label">No feeds configured</p>
    </div>
{:else}
    <div class="flex max-h-64 flex-col divide-y divide-[var(--border)] overflow-y-auto">
        {#each flatArticles as article}
            <a
                href={article.link}
                target="_blank"
                rel="noopener noreferrer"
                class="group flex flex-col gap-0.5 py-2 first:pt-1 last:pb-0"
            >
                <p class="type-body text-[var(--text-1)] transition-colors group-hover:text-[var(--accent)]">
                    {@html article.title}
                </p>
                <div class="flex gap-2">
                    <p class="type-label font-medium text-[var(--text-2)]">{article.feed_title}</p>
                    {#if article.published_at}
                        <p class="type-label text-[var(--text-3)]">
                            {formatTime(article.published_at)}
                        </p>
                    {/if}
                </div>
            </a>
        {/each}
    </div>
{/if}
