<script lang="ts">
    import {
        settings,
        subscribeToRssFeed,
        unsubscribeFromRssFeed,
        updateRssArticleCount,
    } from '$lib/stores/SettingsStore.svelte.ts';
    import { Plus } from '@lucide/svelte';
    import Button from '../ui/Button.svelte';
    import ChoiceChips from '../ui/ChoiceChips.svelte';
    import Field from '../ui/Field.svelte';
    import ListRow from '../ui/ListRow.svelte';
    import TextInput from '../ui/TextInput.svelte';

    let newRssTitle = $state('');
    let newRssUrl = $state('');
    // The add form folds behind a button once there's a feed, so the list stays short.
    let adding = $state(false);
    const formOpen = $derived(adding || settings.rssFeeds.length === 0);

    const countOptions = [5, 10, 15, 20].map((n) => ({ value: n, label: String(n) }));

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
        adding = false;
    }
</script>

<div class="flex flex-col gap-6">
    <!-- Article count -->
    <div class="flex flex-col gap-2">
        <p class="type-body font-medium text-(--text-1)">Articles per feed</p>
        <p class="type-label text-(--text-2)">How many articles to show from each feed.</p>
        <div class="pt-1">
            <ChoiceChips
                label="Articles per feed"
                options={countOptions}
                value={settings.rssArticleCount}
                onchange={updateRssArticleCount}
            />
        </div>
    </div>

    <!-- Subscribed feeds list -->
    <div class="flex flex-col gap-1">
        <p class="type-body font-medium text-(--text-1)">Subscribed feeds</p>
        <div class="mt-2 flex flex-col">
            {#each settings.rssFeeds as feed}
                <ListRow title={feed.title} subtitle={feed.url} onremove={() => unsubscribeFromRssFeed(feed.title)} />
            {:else}
                <p class="type-label py-4 text-center text-(--text-2)">No feeds added yet.</p>
            {/each}
        </div>
    </div>

    <!-- Add feed form -->
    {#if !formOpen}
        <div class="border-t border-(--border) pt-4">
            <Button onclick={() => (adding = true)}><Plus class="icon-sm" /> Add a feed</Button>
        </div>
    {:else}
        <div class="flex flex-col gap-3 border-t border-(--border) pt-4">
            <p class="type-body font-medium text-(--text-1)">Add a feed</p>
            <div class="flex flex-col gap-2">
                <Field label="Title">
                    <TextInput type="text" bind:value={newRssTitle} placeholder="Hacker News" class="w-full" />
                </Field>
                <Field label="URL">
                    <TextInput
                        type="url"
                        bind:value={newRssUrl}
                        placeholder="https://example.com/feed.xml"
                        class="w-full"
                    />
                </Field>
            </div>
            <div class="flex justify-end gap-2">
                {#if settings.rssFeeds.length > 0}
                    <Button variant="ghost" onclick={() => (adding = false)}>Cancel</Button>
                {/if}
                <Button onclick={handleAdd} disabled={!isValidNewRssFeed()}>Add feed</Button>
            </div>
        </div>
    {/if}
</div>
