<script lang="ts">
    import { onMount } from 'svelte';
    import { Images } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '#lib/api.ts';
    import { photoCards, photoIntervals } from '#lib/constants/frameModes.ts';
    import { upcomingCountdowns } from '#lib/frame.ts';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import { loadTasks, taskStore } from '#lib/stores/TaskStore.svelte.ts';
    import { modeStore, setOverride } from '#lib/stores/ModeStore.svelte.ts';
    import { settingsHref } from '#lib/constants/settingsSections.ts';
    import Button from '../ui/Button.svelte';
    import PhotoModePicker from './PhotoModePicker.svelte';
    import { fieldClass } from '../ui/field';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import SettingsGroup from '../ui/SettingsGroup.svelte';
    import Select from '../ui/Select.svelte';
    import TextInput from '../ui/TextInput.svelte';

    let uploads = $state<UploadedPhoto[]>([]);
    let loaded = $state(false);

    onMount(async () => {
        [uploads] = await Promise.all([api.photos.list(), loadTasks()]);
        loaded = true;
    });

    // Display's FrameSettings limits
    const NOTE_TITLE_MAX = 80;
    const NOTE_BODY_MAX = 600;
    const NOTE_LINES_MAX = 12;
    const noteLines = $derived(frameSettings.noteBody ? frameSettings.noteBody.split('\n').length : 0);

    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, new Date(), Infinity));
    // Ids that were chosen but have since been deleted don't count
    const picked = $derived(uploads.filter((u) => frameSettings.photoIds.includes(u.id)));
    const chosen = $derived(picked.length);
    // The picker grows with every upload, so it opens in a sheet; the card shows a strip of the picks.
    const STRIP_MAX = 6;
    let picking = $state(false);
</script>

<!--
@component
Choices for the modes that need them: how long Ambient and Photo keep a photo on the
panel, Photo mode's uploads (one pins it, several make an album picked from at random)
and its card, Countdown mode's countdown, and Note mode's text, with a button to put
the note up until changed.
Saved with the rest of the frame settings.
-->
<div class="@container flex flex-col gap-5">
    <SettingsGroup title="Photo">
        <p class="type-label mb-2 text-(--text-2)">New photo every</p>
        <SegmentedControl
            options={photoIntervals.map((i) => ({ value: String(i.minutes), label: i.label }))}
            value={String(frameSettings.photoIntervalMinutes)}
            onchange={(v) => (frameSettings.photoIntervalMinutes = Number(v))}
            label="New photo every"
        />
        <p class="type-caption mt-2 text-(--text-2)">
            Ambient and Photo mode on the panel. Each change is a full e-paper refresh, about 30 seconds of flashing.
        </p>

        <p class="type-label mt-4 mb-1 text-(--text-2)">Photos</p>
        <p class="type-caption mb-2 text-(--text-2)">
            {#if !loaded}
                Loading your photos…
            {:else if uploads.length === 0}
                No uploads yet — add some under <a href={settingsHref('ambient')} class="underline">Ambient</a>.
            {:else if chosen === 0}
                Pick one photo to show it, several for an album, or a saved group. With none picked, Photo mode shows
                Ambient’s photos.
            {:else}
                {chosen === 1 ? 'Showing 1 photo.' : `An album of ${chosen} photos, one per refresh.`}
            {/if}
        </p>
        {#if uploads.length > 0}
            <div class="flex flex-wrap items-center gap-2">
                {#each picked.slice(0, STRIP_MAX) as u (u.id)}
                    <img src={u.thumb_url} alt={u.caption ?? ''} class="size-12 rounded-lg object-cover" />
                {/each}
                {#if chosen > STRIP_MAX}
                    <span class="type-label px-1 text-(--text-2) tabular-nums">+{chosen - STRIP_MAX}</span>
                {/if}
                <Button onclick={() => (picking = true)}>
                    <Images class="icon-sm" />
                    {chosen === 0 ? 'Choose photos' : 'Change'}
                </Button>
            </div>
            <PhotoModePicker bind:open={picking} {uploads} />
        {/if}
        <p class="type-label mt-4 mb-2 text-(--text-2)">Card</p>
        <SegmentedControl
            options={photoCards.map((c) => ({ value: c.value, label: c.label }))}
            value={frameSettings.photoCard}
            onchange={(v) => (frameSettings.photoCard = v)}
            label="Photo card"
        />
    </SettingsGroup>

    <SettingsGroup title="Countdown">
        <label class="type-label flex flex-wrap items-center gap-2 text-(--text-2)">
            Show
            <Select
                size="sm"
                value={frameSettings.countdownId ?? ''}
                onchange={(e) => {
                    const value = (e.target as HTMLSelectElement).value;
                    frameSettings.countdownId = value === '' ? null : Number(value);
                }}
            >
                <option value="">The next one coming up</option>
                {#each countdowns as { task, days } (task.id)}
                    <option value={task.id}>{task.title} — {days === 0 ? 'today' : `${days} d`}</option>
                {/each}
            </Select>
        </label>
        {#if loaded && countdowns.length === 0}
            <p class="type-caption mt-2 text-(--text-2)">No countdowns yet — add one from the dashboard’s tasks.</p>
        {/if}
    </SettingsGroup>

    <SettingsGroup
        title="Note"
        description="House rules, a welcome, a reminder. Each line stays its own line, and a blank line adds space. The text sizes itself to fill the frame."
    >
        <TextInput
            size="sm"
            type="text"
            bind:value={frameSettings.noteTitle}
            maxlength={NOTE_TITLE_MAX}
            placeholder="Heading, e.g. House rules"
            aria-label="Note heading"
            class="mb-2 w-full"
        />
        <!-- A raw <textarea> (with the shared field look): its oninput trims el.value in place,
             which a bound TextArea would undo -->
        <textarea
            value={frameSettings.noteBody}
            oninput={(e) => {
                // Keep within Display's limit so the save isn't refused mid-typing
                const el = e.target as HTMLTextAreaElement;
                const kept = el.value.split('\n').slice(0, NOTE_LINES_MAX).join('\n');
                if (kept !== el.value) el.value = kept;
                frameSettings.noteBody = kept;
            }}
            maxlength={NOTE_BODY_MAX}
            rows="7"
            placeholder={'Shoes off at the door\nDrinks in the kitchen\nPlease keep the music down after 11'}
            aria-label="Note text"
            class="{fieldClass('sm')} w-full resize-y"></textarea>
        <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
            <p class="type-caption text-(--text-2)">
                {noteLines} / {NOTE_LINES_MAX} lines · {frameSettings.noteBody.length} / {NOTE_BODY_MAX} characters
            </p>
            {#if modeStore.current?.mode === 'note' && modeStore.current.reason === 'override'}
                <p class="type-label font-medium text-(--text-1)">Showing on the frame</p>
            {:else}
                <Button
                    variant="primary"
                    onclick={() => setOverride({ mode: 'note', until: null })}
                    disabled={!frameSettings.noteTitle.trim() && !frameSettings.noteBody.trim()}
                >
                    Show this note
                </Button>
            {/if}
        </div>
    </SettingsGroup>
</div>
