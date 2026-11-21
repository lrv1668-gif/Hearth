<script lang="ts">
    import {
        MAX_GUEST_NETWORK,
        MAX_GUEST_NOTE_LINES,
        MAX_GUEST_NOTES,
        MAX_GUEST_PASSWORD,
        MAX_GUEST_TITLE,
        MAX_NOTE_BODY,
        MAX_NOTE_LINES,
        MAX_NOTE_TITLE,
    } from '#lib/constants/frameLimits.ts';
    import { guestSecurities, type GuestSecurity } from '#lib/constants/frameModes.ts';
    import { onMount } from 'svelte';
    import { upcomingCountdowns } from '#lib/frame/agenda.ts';
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

    const noteLines = $derived(frameSettings.noteBody ? frameSettings.noteBody.split('\n').length : 0);

    const countdowns = $derived(upcomingCountdowns(taskStore.tasks, new Date(), Infinity));
</script>

<!--
@component
Choices for the modes that need them: Countdown mode's countdown, Note mode's text and
Guest mode's welcome and Wi-Fi,
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
            maxlength={MAX_NOTE_TITLE}
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
                const kept = el.value.split('\n').slice(0, MAX_NOTE_LINES).join('\n');
                if (kept !== el.value) el.value = kept;
                frameSettings.noteBody = kept;
            }}
            maxlength={MAX_NOTE_BODY}
            rows="7"
            placeholder={'Shoes off at the door\nDrinks in the kitchen\nPlease keep the music down after 11'}
            aria-label="Note text"
            class="{fieldClass('sm')} w-full resize-y"></textarea>
        <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
            <p class="type-caption text-(--text-2)">
                {noteLines} / {MAX_NOTE_LINES} lines · {frameSettings.noteBody.length} / {MAX_NOTE_BODY} characters
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

    <SettingsGroup
        title="Guest"
        description="A welcome for visitors, with your guest Wi-Fi as a QR code their phone can join from. Anyone who can see the frame can read the password."
    >
        <div class="flex flex-col gap-3">
            <TextInput
                size="sm"
                type="text"
                bind:value={frameSettings.guestTitle}
                maxlength={MAX_GUEST_TITLE}
                placeholder="Heading — Welcome"
                aria-label="Guest heading"
                class="w-full"
            />
            <div class="grid gap-2 @md:grid-cols-2">
                <TextInput
                    size="sm"
                    type="text"
                    bind:value={frameSettings.guestNetwork}
                    maxlength={MAX_GUEST_NETWORK}
                    placeholder="Network name"
                    aria-label="Guest network name"
                    autocomplete="off"
                    spellcheck="false"
                    class="w-full"
                />
                <TextInput
                    size="sm"
                    type="text"
                    bind:value={frameSettings.guestPassword}
                    maxlength={MAX_GUEST_PASSWORD}
                    placeholder="Password"
                    aria-label="Guest network password"
                    autocomplete="off"
                    spellcheck="false"
                    disabled={frameSettings.guestSecurity === 'nopass'}
                    class="w-full"
                />
            </div>
            <label class="type-label flex flex-wrap items-center gap-2 text-(--text-2)">
                Security
                <Select
                    size="sm"
                    value={frameSettings.guestSecurity}
                    onchange={(e) =>
                        (frameSettings.guestSecurity = (e.target as HTMLSelectElement).value as GuestSecurity)}
                >
                    {#each guestSecurities as s (s.value)}
                        <option value={s.value}>{s.label}</option>
                    {/each}
                </Select>
            </label>
            <textarea
                value={frameSettings.guestNotes}
                oninput={(e) => {
                    // Keep within Display's limit so the save isn't refused mid-typing
                    const el = e.target as HTMLTextAreaElement;
                    const kept = el.value.split('\n').slice(0, MAX_GUEST_NOTE_LINES).join('\n');
                    if (kept !== el.value) el.value = kept;
                    frameSettings.guestNotes = kept;
                }}
                maxlength={MAX_GUEST_NOTES}
                rows="3"
                placeholder={'Make yourself at home\nTowels are in the hall closet'}
                aria-label="Guest notes"
                class="{fieldClass('sm')} w-full resize-y"></textarea>
        </div>
        <div class="mt-2 flex justify-end">
            {#if modeStore.current?.mode === 'guest' && modeStore.current.reason === 'override'}
                <p class="type-label font-medium text-(--text-1)">Showing on the frame</p>
            {:else}
                <Button
                    variant="primary"
                    onclick={() => setOverride({ mode: 'guest', until: null })}
                    disabled={!frameSettings.guestNetwork}
                >
                    Show the guest page
                </Button>
            {/if}
        </div>
    </SettingsGroup>
</div>
