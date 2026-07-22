<script lang="ts">
    import { onMount } from 'svelte';
    import { Rss } from '@lucide/svelte';
    import { rssStore, loadRssArticles } from '$lib/stores/RssFeedStore.svelte.ts';
    import SkeletonLoader from '$lib/components/SkeletonLoader.svelte';

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

    let loadPromise = $state<Promise<void>>(new Promise(() => {}));
    let listEl = $state<HTMLDivElement | null>(null);
    let atBottom = $state(false);

    onMount(() => {
        loadPromise = loadRssArticles();
    });

    function updateAtBottom() {
        if (!listEl) return;
        atBottom = listEl.scrollTop + listEl.clientHeight >= listEl.scrollHeight - 1;
    }

    // Recheck when the list renders or its contents change, so the fade only
    // shows when there actually is more to scroll to.
    $effect(() => {
        flatArticles;
        updateAtBottom();
    });
</script>

<SkeletonLoader promise={loadPromise}>
    {#if flatArticles.length === 0}
        <div class="flex flex-col gap-1 py-4 text-(--text-3)">
            <Rss class="icon-sm mb-1" />
            <p class="type-label">No feeds added yet.</p>
            <a href="/settings" class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)">
                Add feeds in Settings →
            </a>
        </div>
    {:else}
        <div class="relative">
            <div
                bind:this={listEl}
                class="scroll-thin flex max-h-[min(25vh,320px)] flex-col overflow-y-auto"
                onscroll={updateAtBottom}
            >
                {#each flatArticles as article}
                    <a
                        href={article.link}
                        target="_blank"
                        rel="noopener noreferrer"
                        class="group flex flex-col gap-0.5 border-l-2 border-transparent py-2 pl-3 transition-colors first:pt-1 last:pb-0 hover:border-(--accent)"
                    >
                        <p class="type-body text-(--text-1) transition-colors group-hover:text-(--accent)">
                            {article.title}
                        </p>
                        <span class="type-label text-(--text-3)">
                            <span class="font-medium text-(--text-2)">{article.feed_title}</span>
                            {#if article.published_at}
                                · {formatTime(article.published_at)}
                            {/if}
                        </span>
                    </a>
                {/each}
            </div>
            {#if !atBottom}
                <div
                    class="pointer-events-none absolute right-0 bottom-0 left-0 h-8"
                    style="background: linear-gradient(to bottom, transparent, var(--bg))"
                ></div>
            {/if}
        </div>
    {/if}
</SkeletonLoader>
