<script lang="ts">
    import { onMount } from 'svelte';
    import { CalendarPlus, Plus } from '@lucide/svelte';
    import { addIcsFeed, calendarStore, loadIcsFeeds, removeIcsFeed } from '#lib/stores/CalendarStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import ConnectionRow from '../ui/ConnectionRow.svelte';
    import Field from '../ui/Field.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import ListRow from '../ui/ListRow.svelte';
    import TextInput from '../ui/TextInput.svelte';

    onMount(() => loadIcsFeeds());

    let adding = $state(false);
    let name = $state('');
    let url = $state('');
    let saving = $state(false);
    let error = $state<string | null>(null);

    const count = $derived(calendarStore.icsFeeds.length);
    const status = $derived(
        count === 0
            ? 'No calendars added.'
            : `${count} ${count === 1 ? 'calendar' : 'calendars'} — events are syncing, read-only.`
    );

    function close() {
        adding = false;
        name = '';
        url = '';
        error = null;
    }

    async function handleAdd(e: SubmitEvent) {
        e.preventDefault();
        saving = true;
        error = null;
        error = await addIcsFeed(name.trim(), url.trim());
        saving = false;
        if (!error) close();
    }
</script>

<!--
@component
Calendars subscribed to by link: an iCloud calendar's public webcal:// address, or any other
.ics URL. The service keeps the link (it is all it takes to read the calendar) and only
ever sends back its host.
-->
<div class="flex flex-col gap-4">
    <ConnectionRow icon={CalendarPlus} name="Apple Calendar & calendar links" {status}>
        {#snippet actions()}
            {#if !adding}
                <Button onclick={() => (adding = true)}><Plus class="icon-sm" /> Add a calendar</Button>
            {/if}
        {/snippet}
    </ConnectionRow>

    {#if count > 0}
        <div class="flex flex-col">
            {#each calendarStore.icsFeeds as feed (feed.id)}
                <ListRow title={feed.name} subtitle={feed.host} onremove={() => removeIcsFeed(feed.id)} />
            {/each}
        </div>
    {/if}

    {#if adding}
        <form class="flex flex-col gap-3 border-t border-(--border) pt-4" onsubmit={handleAdd}>
            <InlineMessage>
                In Apple Calendar, open the calendar’s sharing options, turn on Public Calendar and copy its link (it
                starts with webcal://). Anyone who has that link can read the calendar, so Hearth stores it encrypted
                and doesn’t show it again. Any other calendar’s .ics link works too.
            </InlineMessage>
            <Field label="Calendar link">
                <TextInput
                    type="text"
                    bind:value={url}
                    placeholder="webcal://p01-caldav.icloud.com/published/2/…"
                    autocomplete="off"
                    class="w-full"
                />
            </Field>
            <Field label="Name (optional — uses the calendar’s own)">
                <TextInput type="text" bind:value={name} placeholder="Family" maxlength={60} class="w-full" />
            </Field>
            {#if error}<InlineMessage tone="error">{error}</InlineMessage>{/if}
            <div class="flex justify-end gap-2">
                <Button variant="ghost" onclick={close}>Cancel</Button>
                <Button type="submit" variant="primary" disabled={saving || url.trim() === ''}>
                    {saving ? 'Checking link…' : 'Add calendar'}
                </Button>
            </div>
        </form>
    {/if}
</div>
