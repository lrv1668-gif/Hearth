<script lang="ts">
    import {
        settings,
        subscribeToRssFeed,
        unsubscribeFromRssFeed,
        updateRssArticleCount,
    } from '$lib/stores/SettingsStore.svelte.ts';
    import { Plus, Save, Trash } from '@lucide/svelte';
    import SubTitle from '../SubTitle.svelte';

    let newRssTitle = $state('');
    let newRssUrl = $state('');

    const countOptions = [5, 10, 15, 20];

    function isValidNewRssFeed() {
        let validatedUrl = undefined;
        try {
            validatedUrl = new URL(newRssUrl);
        } catch (_) {
            return false;
        }

        return newRssTitle !== '' && validatedUrl.toString() !== '';
    }
</script>

<div class="mb-4 space-y-3 border-b border-b-[var(--border)] pb-4">
    <SubTitle
        subTitleText="Feed Count"
        subTitleDescription="Configure how many articles are displayed from your RSS feeds."
    />
    <div class="flex gap-2">
        {#each countOptions as n}
            <button
                onclick={() => updateRssArticleCount(n)}
                class="rounded-full border px-4 py-1.5 text-xs tracking-wide transition-colors
                    {settings.rssArticleCount === n
                    ? 'pointer-events-none border-[var(--text-1)] bg-[var(--text-1)] text-[var(--bg)]'
                    : 'border-[var(--border)] text-[var(--text-1)] hover:border-[var(--text-2)] hover:bg-[var(--text-4)] hover:text-[var(--text-1)]'} type-label"
            >
                {n}
            </button>
        {/each}
    </div>
</div>

<div>
    <SubTitle subTitleText="Subscribed RSS Feeds" subTitleDescription="Modify the RSS feeds you're subscribed to." />
    <div class="mb-4 flex flex-col gap-2">
        {#if !settings.rssFeeds.length}
            <p class="text-body">You're not subscribed to any RSS Feeds.</p>
        {:else}
            {#each settings.rssFeeds as rssFeed}
                <div class="flex flex-col gap-2 md:flex-row">
                    <p class="type-body font-bold">{rssFeed.title}:</p>
                    <a
                        class="type-body text-[var(--text-2)] hover:text-[var(--text-1)]"
                        href={rssFeed.url}
                        target="_blank">{rssFeed.url}</a
                    >
                    <button>
                        <Trash onclick={() => unsubscribeFromRssFeed(rssFeed.title)} class="icon-sm" />
                    </button>
                </div>
            {/each}
        {/if}
    </div>

    <p class="type-body mb-2 text-[var(--text-1)]">Add New RSS Feed:</p>
    <div class="ml-2 flex flex-col gap-4 md:flex-row">
        <div class="flex items-center gap-2">
            <p class="type-label">Title:</p>
            <input
                type="text"
                bind:value={newRssTitle}
                class="type-body flex-1 rounded-lg bg-[var(--surface)]
            px-2 py-1.5 text-[var(--text-1)] placeholder-[var(--text-2)] outline-none
            transition focus:ring-1 focus:ring-[var(--border)]"
            />
        </div>
        <div class="flex items-center gap-2">
            <p class="type-label">Url:</p>
            <input
                type="text"
                bind:value={newRssUrl}
                class="type-body flex-1 rounded-lg bg-[var(--surface)]
            px-2 py-1.5 text-[var(--text-1)] placeholder-[var(--text-2)] outline-none
            transition focus:ring-1 focus:ring-[var(--border)]"
            />
        </div>
        <button
            onclick={() => {
                subscribeToRssFeed(newRssTitle, newRssUrl);
                newRssTitle = '';
                newRssUrl = '';
            }}
            disabled={!isValidNewRssFeed()}
            class="disabled:text-[var(--text-4)]"
        >
            <Save class="icon-md" />
        </button>
    </div>
</div>
