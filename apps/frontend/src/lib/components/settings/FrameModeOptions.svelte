<script lang="ts">
    import { onMount } from 'svelte';
    import { upcomingCountdowns } from '#lib/frame.ts';
    import { frameSettings } from '#lib/stores/FrameSettingsStore.svelte.ts';
    import { loadTasks, taskStore } from '#lib/stores/TaskStore.svelte.ts';
    import { modeStore, setOverride } from '#lib/stores/ModeStore.svelte.ts';
    import Button from '../ui/Button.svelte';
    import { fieldClass } from '../ui/field';
    import SettingsGroup from '../ui/SettingsGroup.svelte';
    import Select from '../ui/Select.svelte';
    import TextInput from '../ui/TextInput.svelte';

    let loaded = $state(false);

    onMount(async () => {
        await loadTasks();
        loaded = true;
    });

    // Display's FrameSettings limits
    const NOTE_TITLE_MAX = 80;
    const NOTE_BODY_MAX = 600;
    const NOTE_LINES_MAX = 12;
    const noteLines = $derived(frameSettings.noteBody ? frameSettings.noteBody.split('\n').length : 0);

    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, new Date(), Infinity));
</script>

<!--
@component
Choices for the modes that need them: Countdown mode's countdown, and Note mode's text,
each with a button to put that mode up until changed. Photo mode's are in Photos
(`FramePhotoSettings`).
Saved with the rest of the frame settings.
-->
<div class="@container flex flex-col gap-5">
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
        {:else if countdowns.length > 0}
            <div class="mt-3 flex justify-end">
                {#if modeStore.current?.mode === 'countdown' && modeStore.current.reason === 'override'}
                    <p class="type-label font-medium text-(--text-1)">Showing on the frame</p>
                {:else}
                    <Button variant="primary" onclick={() => setOverride({ mode: 'countdown', until: null })}>
                        Show this countdown
                    </Button>
                {/if}
            </div>
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
