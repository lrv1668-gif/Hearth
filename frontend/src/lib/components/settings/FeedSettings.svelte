<script lang="ts">
    import {
        settings,
        subscribeToRssFeed,
        unsubscribeFromRssFeed,
        updateRssArticleCount,
    } from '$lib/stores/SettingsStore.svelte.ts';
    import { Trash2 } from '@lucide/svelte';

    let newRssTitle = $state('');
    let newRssUrl = $state('');

    const countOptions = [5, 10, 15, 20];

    function isValidNewRssFeed() {
        try {
            new URL(newRssUrl);
        } catch {
            return false;
        }
        return newRssTitle.trim() !== '';
    }

    function handleAdd() {
        subscribeToRssFeed(newRssTitle.trim(), newRssUrl.trim());
        newRssTitle = '';
        newRssUrl = '';
    }
</script>

<div class="flex flex-col gap-6">
    <!-- Article count -->
    <div class="flex flex-col gap-2">
        <p class="type-body font-medium text-[var(--text-1)]">Articles per feed</p>
        <p class="type-label text-[var(--text-2)]">How many articles to show from each feed.</p>
        <div class="flex gap-2 pt-1">
            {#each countOptions as n}
                <button
                    onclick={() => updateRssArticleCount(n)}
                    class="type-label rounded-full border px-4 py-1.5 transition-colors
                        {settings.rssArticleCount === n
                        ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                        : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--surface-hi)]'}"
                >
                    {n}
                </button>
            {/each}
        </div>
    </div>

    <!-- Subscribed feeds list -->
    <div class="flex flex-col gap-1">
        <p class="type-body font-medium text-[var(--text-1)]">Subscribed feeds</p>
        <div class="mt-2 flex flex-col">
            {#if settings.rssFeeds.length === 0}
                <p class="type-label py-4 text-center text-[var(--text-3)]">No feeds added yet.</p>
            {:else}
                {#each settings.rssFeeds as feed}
                    <div class="group flex items-center gap-3 rounded-lg px-3 py-2 hover:bg-[var(--surface-hi)]">
                        <div class="min-w-0 flex-1">
                            <p class="type-body font-medium text-[var(--text-1)]">{feed.title}</p>
                            <p class="type-label truncate text-[var(--text-2)]">{feed.url}</p>
                        </div>
                        <button
                            onclick={() => unsubscribeFromRssFeed(feed.title)}
                            class="shrink-0 text-[var(--text-2)] opacity-0 transition-opacity hover:text-[var(--text-1)] group-hover:opacity-100"
                            aria-label="Remove {feed.title}"
                        >
                            <Trash2 class="icon-sm" />
                        </button>
                    </div>
                {/each}
            {/if}
        </div>
    </div>

    <!-- Add feed form -->
    <div class="flex flex-col gap-3 border-t border-[var(--border)] pt-4">
        <p class="type-body font-medium text-[var(--text-1)]">Add a feed</p>
        <div class="flex flex-col gap-2">
            <label class="flex flex-col gap-1">
                <span class="type-caption uppercase tracking-widest text-[var(--text-2)]">Title</span>
                <input
                    type="text"
                    bind:value={newRssTitle}
                    placeholder="Hacker News"
                    class="type-body w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-[var(--text-1)] outline-none transition-colors placeholder:text-[var(--text-4)] focus:border-[var(--text-3)]"
                />
            </label>
            <label class="flex flex-col gap-1">
                <span class="type-caption uppercase tracking-widest text-[var(--text-2)]">URL</span>
                <input
                    type="url"
                    bind:value={newRssUrl}
                    placeholder="https://example.com/feed.xml"
                    class="type-body w-full rounded-lg border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-[var(--text-1)] outline-none transition-colors placeholder:text-[var(--text-4)] focus:border-[var(--text-3)]"
                />
            </label>
        </div>
        <div class="flex justify-end">
            <button
                onclick={handleAdd}
                disabled={!isValidNewRssFeed()}
                class="type-label rounded-full border border-[var(--border)] px-4 py-1.5 text-[var(--text-1)] transition-colors hover:bg-[var(--surface-hi)] disabled:pointer-events-none disabled:opacity-40"
            >
                Add feed
            </button>
        </div>
    </div>
</div>
