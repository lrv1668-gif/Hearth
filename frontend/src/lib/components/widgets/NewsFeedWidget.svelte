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
    <div class="flex flex-col gap-1 py-4 text-[var(--text-3)]">
        <Rss class="icon-sm mb-1" />
        <p class="type-label">No feeds added yet.</p>
        <a href="/settings" class="type-label text-[var(--accent)] transition-colors hover:text-[var(--accent-hi)]">
            Add feeds in Settings →
        </a>
    </div>
{:else}
    <div class="relative">
        <div class="scroll-thin flex max-h-[min(25vh,320px)] flex-col overflow-y-auto">
            {#each flatArticles as article}
                <a
                    href={article.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    class="group flex flex-col gap-0.5 border-l-2 border-transparent py-2 pl-3 transition-colors first:pt-1 last:pb-0 hover:border-[var(--accent)]"
                >
                    <p class="type-body text-[var(--text-1)] transition-colors group-hover:text-[var(--accent)]">
                        {@html article.title}
                    </p>
                    <span class="type-label text-[var(--text-3)]">
                        <span class="font-medium text-[var(--text-2)]">{article.feed_title}</span>
                        {#if article.published_at}
                            · {formatTime(article.published_at)}
                        {/if}
                    </span>
                </a>
            {/each}
        </div>
        <div
            class="pointer-events-none absolute bottom-0 left-0 right-0 h-8"
            style="background: linear-gradient(to bottom, transparent, var(--bg))"
        ></div>
    </div>
{/if}
