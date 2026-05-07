<script lang="ts">
    import { onMount } from 'svelte';
    import { Rss } from '@lucide/svelte';
    import { rssStore, loadRssArticles } from '$lib/stores/RssFeedStore.svelte.ts';
    import { settings } from '$lib/stores/SettingsStore.svelte.ts';

    function formatDate(iso: string | null): string {
        if (!iso) return '';
        const d = new Date(iso);
        if (isNaN(d.getTime())) return '';
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    onMount(() => loadRssArticles(settings.rssArticleCount));
</script>

{#if rssStore.loading}
    <p class="type-label text-[var(--text-3)]">Loading articles…</p>
{:else if rssStore.articles.length === 0}
    <div class="flex items-center gap-2 text-[var(--text-3)]">
        <Rss class="icon-sm" />
        <p class="type-label">No articles available</p>
    </div>
{:else}
    <div class="flex flex-col divide-y divide-[var(--border)]">
        {#each rssStore.articles as article}
            <a
                href={article.link}
                target="_blank"
                rel="noopener noreferrer"
                class="group flex flex-col gap-0.5 py-2.5 first:pt-0 last:pb-0"
            >
                <p class="type-body text-[var(--text-1)] transition-colors group-hover:text-[var(--accent)]">
                    {article.title}
                </p>
                <div class="flex flex-row gap-2">
                    <p class="type-label text-[var(--text-2)]">
                        The Verge
                    </p>
                    <p class="type-label text-[var(--text-3)]">
                        {article.published_at ? ' · ' + formatDate(article.published_at) : ''}
                    </p>
                </div>
            </a>
        {/each}
    </div>
{/if}
