<script lang="ts">
    import { onMount } from 'svelte';
    import { Check } from '@lucide/svelte';
    import { api, type UploadedPhoto } from '$lib/api';
    import { photoCards } from '$lib/constants/frameModes';
    import { upcomingCountdowns } from '$lib/frame';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { loadTasks, taskStore } from '$lib/stores/TaskStore.svelte.ts';
    import { modeStore, setOverride } from '$lib/stores/ModeStore.svelte.ts';
    import { settingsHref } from '$lib/constants/settingsSections';
    import SegmentedControl from './SegmentedControl.svelte';

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
    const field =
        'type-label rounded-lg border border-(--border) bg-(--surface) px-2.5 py-1.5 text-(--text-1) outline-none focus:border-(--text-3)';

    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, new Date(), Infinity));
    // Ids that were chosen but have since been deleted don't count
    const chosen = $derived(uploads.filter((u) => frameSettings.photoIds.includes(u.id)).length);
</script>

<!--
@component
Choices for the modes that need them: Photo mode's uploads (one pins it, several make
an album picked from at random) and its card, Countdown mode's countdown, and Note
mode's text, with a button to put the note up until changed.
Saved with the rest of the frame settings.
-->
<div class="@container flex flex-col gap-6">
    <div>
        <p class="type-label mb-1 font-semibold tracking-widest text-(--text-2) uppercase">Photo</p>
        <p class="type-label mb-3 text-(--text-2)">
            {#if !loaded}
                Loading your photos…
            {:else if uploads.length === 0}
                No uploads yet — add some under <a href={settingsHref('ambient')} class="underline">Ambient</a>.
            {:else if chosen === 0}
                Pick one photo to show it, or several for an album. With none picked, Photo mode shows Ambient’s photos.
            {:else}
                {chosen === 1 ? 'Showing 1 photo.' : `An album of ${chosen} photos, one per refresh.`}
            {/if}
        </p>
        {#if uploads.length > 0}
            <div class="mb-3 grid grid-cols-4 gap-2 @sm:grid-cols-6">
                {#each uploads as u (u.id)}
                    {@const on = frameSettings.photoIds.includes(u.id)}
                    <button
                        onclick={() => frameSettings.togglePhoto(u.id)}
                        aria-pressed={on}
                        aria-label={u.caption ?? 'Photo'}
                        class="relative aspect-square overflow-hidden rounded-lg border-2 transition-all
                            {on ? 'border-(--accent)' : 'border-transparent opacity-70 hover:opacity-100'}"
                    >
                        <img src={u.thumb_url} alt="" class="absolute inset-0 h-full w-full object-cover" />
                        {#if on}
                            <span class="absolute top-1 right-1 rounded-full bg-(--accent) p-0.5 text-(--accent-fg)">
                                <Check class="icon-xs" />
                            </span>
                        {/if}
                    </button>
                {/each}
            </div>
        {/if}
        <SegmentedControl
            options={photoCards.map((c) => ({ value: c.value, label: c.label }))}
            value={frameSettings.photoCard}
            onchange={(v) => (frameSettings.photoCard = v)}
        />
    </div>

    <div>
        <p class="type-label mb-1 font-semibold tracking-widest text-(--text-2) uppercase">Countdown</p>
        <label class="type-label flex flex-wrap items-center gap-2 text-(--text-2)">
            Show
            <select
                value={frameSettings.countdownId ?? ''}
                onchange={(e) => {
                    const value = (e.target as HTMLSelectElement).value;
                    frameSettings.countdownId = value === '' ? null : Number(value);
                }}
                class="type-label rounded-lg border border-(--border) bg-(--surface) px-2.5 py-1.5 text-(--text-1) outline-none focus:border-(--text-3)"
            >
                <option value="">The next one coming up</option>
                {#each countdowns as { task, days } (task.id)}
                    <option value={task.id}>{task.title} — {days === 0 ? 'today' : `${days} d`}</option>
                {/each}
            </select>
        </label>
        {#if loaded && countdowns.length === 0}
            <p class="type-caption mt-2 text-(--text-2)">No countdowns yet — add one from the dashboard’s tasks.</p>
        {/if}
    </div>

    <div>
        <p class="type-label mb-1 font-semibold tracking-widest text-(--text-2) uppercase">Note</p>
        <p class="type-label mb-3 text-(--text-2)">
            House rules, a welcome, a reminder. Each line stays its own line, and a blank line adds space. The text
            sizes itself to fill the frame.
        </p>
        <input
            type="text"
            bind:value={frameSettings.noteTitle}
            maxlength={NOTE_TITLE_MAX}
            placeholder="Heading, e.g. House rules"
            aria-label="Note heading"
            class="{field} mb-2 w-full"
        />
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
            class="{field} w-full resize-y"></textarea>
        <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
            <p class="type-caption text-(--text-2)">
                {noteLines} / {NOTE_LINES_MAX} lines · {frameSettings.noteBody.length} / {NOTE_BODY_MAX} characters
            </p>
            {#if modeStore.current?.mode === 'note' && modeStore.current.reason === 'override'}
                <p class="type-label font-medium text-(--text-1)">Showing on the frame</p>
            {:else}
                <button
                    onclick={() => setOverride({ mode: 'note', until: null })}
                    disabled={!frameSettings.noteTitle.trim() && !frameSettings.noteBody.trim()}
                    class="type-label rounded-lg bg-(--accent) px-4 py-1.5 font-semibold text-(--accent-fg) transition-opacity hover:opacity-90 disabled:opacity-50"
                >
                    Show this note
                </button>
            {/if}
        </div>
    </div>
</div>
