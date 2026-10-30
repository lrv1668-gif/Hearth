<script lang="ts">
    import {
        settings,
        subscribeToRssFeed,
        unsubscribeFromRssFeed,
        updateRssArticleCount,
    } from '#lib/stores/SettingsStore.svelte.ts';
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
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

<div class="flex flex-col gap-5">
    <!-- Article count -->
    <SettingsGroup title="Articles per feed" description="How many articles to show from each feed.">
        <ChoiceChips
            label="Articles per feed"
            options={countOptions}
            value={settings.rssArticleCount}
            onchange={updateRssArticleCount}
        />
    </SettingsGroup>

    <!-- Subscribed feeds list -->
    <SettingsGroup title="Subscribed feeds">
        <div class="flex flex-col">
            {#each settings.rssFeeds as feed}
                <ListRow title={feed.title} subtitle={feed.url} onremove={() => unsubscribeFromRssFeed(feed.title)} />
            {:else}
                <p class="type-label py-4 text-center text-(--text-2)">No feeds added yet.</p>
            {/each}
        </div>
    </SettingsGroup>

    <!-- Add feed form -->
    {#if !formOpen}
        <SettingsGroup>
            <Button onclick={() => (adding = true)}><Plus class="icon-sm" /> Add a feed</Button>
        </SettingsGroup>
    {:else}
        <SettingsGroup title="Add a feed">
            <div class="flex flex-col gap-3">
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
        </SettingsGroup>
    {/if}
</div>
