<script lang="ts">
    import { onMount } from 'svelte';
    import { Plus, X } from '@lucide/svelte';
    import type { ModeRule } from '$lib/api';
    import {
        DEFAULT_ROTATE_MINUTES,
        frameModeLabel,
        frameModes,
        MAX_RULE_MODES,
        modeDays,
        rotateIntervals,
        type FrameModeId,
        type ModeDay,
    } from '$lib/constants/frameModes';
    import { clearOverride, modeStore, saveSchedule, setOverride } from '$lib/stores/ModeStore.svelte.ts';
    import { settingsHref } from '$lib/constants/settingsSections';
    import { checkIn, DEFAULT_QUIET_HOURS } from '$lib/frame';
    import { frameSettings } from '$lib/stores/FrameSettingsStore.svelte.ts';
    import { frameStatusStore, loadFrameStatus } from '$lib/stores/FrameStatusStore.svelte.ts';
    import { poll } from '$lib/utils';
    import Button from '../ui/Button.svelte';
    import ChoiceChips from '../ui/ChoiceChips.svelte';
    import DayPicker from '../ui/DayPicker.svelte';
    import IconButton from '../ui/IconButton.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import SettingsGroup from '../ui/SettingsGroup.svelte';
    import Select from '../ui/Select.svelte';
    import TextInput from '../ui/TextInput.svelte';
    import ToggleRow from '../ui/ToggleRow.svelte';

    const HOUR_MS = 60 * 60 * 1000;
    const WEEKDAYS: ModeDay[] = ['mon', 'tue', 'wed', 'thu', 'fri'];

    let chosen = $state<FrameModeId>('daily');
    let hold = $state<'hour' | 'midnight' | 'forever'>('hour');

    const current = $derived(modeStore.current);

    // The frame's last check-in; `now` ticks with each refresh so "3 min ago" stays true
    const STATUS_REFRESH_MS = 60_000;
    let now = $state(new Date());
    onMount(() => {
        loadFrameStatus();
        return poll(() => {
            now = new Date();
            loadFrameStatus();
        }, STATUS_REFRESH_MS);
    });
    const frameCheckIn = $derived(frameStatusStore.status ? checkIn(frameStatusStore.status, now) : null);

    function setQuiet(patch: Partial<{ start: string; end: string }>) {
        const next = { ...(frameSettings.quietHours ?? DEFAULT_QUIET_HOURS), ...patch };
        // Display refuses a window that starts and ends at the same time; keep the last good one
        if (next.start && next.end && next.start !== next.end) frameSettings.quietHours = next;
    }

    function timeLabel(iso: string): string {
        const at = new Date(iso);
        const sameDay = at.toDateString() === new Date().toDateString();
        const time = at.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
        return sameDay ? time : `${at.toLocaleDateString('en-US', { weekday: 'short' })} ${time}`;
    }

    const why = $derived.by(() => {
        if (!current) return '';
        const until = current.until ? ` until ${timeLabel(current.until)}` : '';
        if (current.reason === 'override') {
            if (current.photo_id) return 'A pinned photo — until you switch it off in your photo collection.';
            return current.until ? `Your choice,${until}.` : 'Your choice, until you change it.';
        }
        if (current.reason === 'schedule') return `From the schedule${until}.`;
        return `The default mode${current.until ? `, until the schedule changes at ${timeLabel(current.until)}` : ''}.`;
    });

    function show() {
        const until =
            hold === 'hour'
                ? new Date(Date.now() + HOUR_MS)
                : hold === 'midnight'
                  ? new Date(new Date().setHours(24, 0, 0, 0))
                  : null;
        setOverride({ mode: chosen, until: until?.toISOString() ?? null });
    }

    // Rules are edited locally and saved whenever every rule has a day — Display
    // refuses a rule with none, and the user may be mid-edit.
    let rules = $state<ModeRule[]>([]);
    $effect(() => {
        rules = modeStore.schedule.rules.map((r) => ({ ...r, days: [...r.days] }));
    });

    function commit(next: ModeRule[]) {
        rules = next;
        if (next.every((r) => r.days.length > 0)) saveSchedule({ ...modeStore.schedule, rules: next });
    }

    function update(i: number, patch: Partial<ModeRule>) {
        commit(rules.map((r, j) => (j === i ? { ...r, ...patch } : r)));
    }

    // A rule's modes in order: `mode`, then `rotate`. Two or more make it rotate.
    function ruleModes(rule: ModeRule): FrameModeId[] {
        return [rule.mode, ...(rule.rotate ?? [])];
    }

    function setModes(i: number, modes: FrameModeId[]) {
        const [mode, ...rotate] = modes;
        update(i, {
            mode,
            rotate: rotate.length > 0 ? rotate : null,
            rotate_minutes: rotate.length > 0 ? (rules[i].rotate_minutes ?? DEFAULT_ROTATE_MINUTES) : null,
        });
    }

    function addMode(i: number) {
        const modes = ruleModes(rules[i]);
        // The first mode the rule doesn't show yet, so a new step changes the picture
        const next = frameModes.find((m) => !modes.includes(m.id))?.id ?? modes[0];
        setModes(i, [...modes, next]);
    }

    function toggleDay(i: number, day: ModeDay) {
        const days = rules[i].days;
        update(i, { days: days.includes(day) ? days.filter((d) => d !== day) : [...days, day] });
    }
</script>

<!--
@component
What the frame shows and when: the mode showing now (with why, and a manual choice
that beats the schedule for an hour, until midnight, or until changed), then the
default mode and the time-of-day rules (each one mode, or several it rotates through
every few minutes), and the quiet hours (saved with the frame
settings) when the panel holds its picture. Held by the Display service, shared by every
device; the frame's Button B steps to the next mode the same way. Under "Showing now",
whether the frame is still checking in for its picture.
-->
{#if modeStore.error}
    <InlineMessage tone="error" class="mb-4"
        >Can't reach the Display service — modes can't be changed right now.</InlineMessage
    >
{/if}
<div class="@container flex flex-col gap-5">
    <SettingsGroup title="Now">
        {#if current}
            <p class="type-body text-(--text-1)">
                <span class="font-semibold">{frameModeLabel(current.mode)}</span>
                <span class="text-(--text-2)">· {why}</span>
            </p>
            {#if current.reason === 'override'}
                <Button variant="link" class="mt-2" onclick={() => clearOverride()}>Back to schedule</Button>
            {/if}
        {:else if modeStore.loaded}
            <p class="type-body text-(--text-2)">Unknown.</p>
        {/if}
        {#if frameCheckIn}
            <InlineMessage tone={frameCheckIn.stale ? 'error' : 'muted'} class="mt-2">{frameCheckIn.text}</InlineMessage
            >
        {/if}

        <p class="type-label mt-4 mb-2 text-(--text-2)">Show instead</p>
        <div class="mb-3">
            <ChoiceChips
                label="Mode to show"
                options={frameModes.map((m) => ({ value: m.id, label: m.label, description: m.description }))}
                value={chosen}
                onchange={(v) => (chosen = v)}
            />
        </div>
        <div class="flex flex-wrap items-center gap-3">
            <SegmentedControl
                options={[
                    { value: 'hour', label: 'For an hour' },
                    { value: 'midnight', label: 'Until midnight' },
                    { value: 'forever', label: 'Until I change it' },
                ]}
                value={hold}
                onchange={(v) => (hold = v)}
                label="How long"
            />
            <Button variant="primary" onclick={show} disabled={modeStore.error}>
                Show {frameModeLabel(chosen)}
            </Button>
        </div>
    </SettingsGroup>

    <SettingsGroup title="Schedule">
        <label class="type-label mb-3 flex flex-wrap items-center gap-2 text-(--text-2)">
            Otherwise show
            <Select
                size="sm"
                value={modeStore.schedule.default_mode}
                onchange={(e) =>
                    saveSchedule({
                        ...modeStore.schedule,
                        default_mode: (e.target as HTMLSelectElement).value as FrameModeId,
                    })}
            >
                {#each frameModes as m (m.id)}<option value={m.id}>{m.label}</option>{/each}
            </Select>
        </label>

        <div class="flex flex-col gap-3">
            {#each rules as rule, i (i)}
                {@const modes = ruleModes(rule)}
                <div class="flex flex-wrap items-center gap-2 rounded-lg border border-(--border) p-3">
                    <span class="flex w-full flex-wrap items-center gap-1.5">
                        {#each modes as mode, k (k)}
                            <!-- A step stays on one line when the row wraps -->
                            <span class="flex items-center gap-1.5">
                                {#if k > 0}<span class="type-label text-(--text-2)">then</span>{/if}
                                <Select
                                    size="sm"
                                    value={mode}
                                    onchange={(e) =>
                                        setModes(
                                            i,
                                            modes.with(k, (e.target as HTMLSelectElement).value as FrameModeId)
                                        )}
                                    aria-label={k === 0 ? 'Mode' : `Mode ${k + 1}`}
                                >
                                    {#each frameModes as m (m.id)}<option value={m.id}>{m.label}</option>{/each}
                                </Select>
                                {#if k > 0}
                                    <IconButton
                                        label="Remove {frameModeLabel(mode)} from the rotation"
                                        size="sm"
                                        onclick={() => setModes(i, modes.toSpliced(k, 1))}
                                    >
                                        <X class="icon-sm" />
                                    </IconButton>
                                {/if}
                            </span>
                        {/each}
                        {#if modes.length < MAX_RULE_MODES}
                            <Button size="sm" variant="ghost" onclick={() => addMode(i)}>
                                <Plus class="icon-sm" /> Add a mode
                            </Button>
                        {/if}
                        {#if modes.length > 1}
                            <label class="type-label flex items-center gap-1.5 text-(--text-2)">
                                every
                                <Select
                                    size="sm"
                                    value={String(rule.rotate_minutes ?? DEFAULT_ROTATE_MINUTES)}
                                    onchange={(e) =>
                                        update(i, { rotate_minutes: Number((e.target as HTMLSelectElement).value) })}
                                >
                                    {#each rotateIntervals as r (r.minutes)}
                                        <option value={String(r.minutes)}>{r.label}</option>
                                    {/each}
                                </Select>
                            </label>
                        {/if}
                    </span>
                    <DayPicker
                        days={modeDays}
                        selected={rule.days}
                        ontoggle={(d) => toggleDay(i, d)}
                        label="Days for time {i + 1}"
                    />
                    <span class="flex items-center gap-1.5">
                        <TextInput
                            size="sm"
                            type="time"
                            value={rule.start}
                            onchange={(e) => update(i, { start: (e.target as HTMLInputElement).value })}
                            aria-label="From"
                        />
                        <span class="type-label text-(--text-2)">to</span>
                        <TextInput
                            size="sm"
                            type="time"
                            value={rule.end}
                            onchange={(e) => update(i, { end: (e.target as HTMLInputElement).value })}
                            aria-label="To"
                        />
                    </span>
                    <IconButton
                        label="Remove rule"
                        size="sm"
                        class="ml-auto"
                        onclick={() => commit(rules.filter((_, j) => j !== i))}
                    >
                        <X class="icon-sm" />
                    </IconButton>
                    {#if rule.days.length === 0}
                        <InlineMessage tone="error" class="w-full"
                            >Pick at least one day to save this rule.</InlineMessage
                        >
                    {/if}
                </div>
            {/each}
        </div>
        <Button
            class="mt-3"
            onclick={() => commit([...rules, { mode: 'daily', days: [...WEEKDAYS], start: '07:00', end: '09:00' }])}
            disabled={modeStore.error}
        >
            <Plus class="icon-sm" /> Add a time
        </Button>
        <p class="type-caption mt-2 text-(--text-2)">
            The first matching time wins. A time that ends before it starts runs past midnight. A time with more than
            one mode steps through them in order, starting on the first, and holds its place through quiet hours. Times
            are in your home’s time zone, set under <a href={settingsHref('home')} class="underline">Home</a>.
        </p>
    </SettingsGroup>

    <SettingsGroup title="Quiet hours">
        <ToggleRow
            checked={frameSettings.quietHours !== null}
            onchange={() => (frameSettings.quietHours = frameSettings.quietHours ? null : { ...DEFAULT_QUIET_HOURS })}
            title="Hold the picture overnight"
            description="No redraws on the timer, so the panel doesn't flash while you sleep. A mode change from the schedule, a button press or a change here still redraws it."
            disabled={frameSettings.error}
        />
        {#if frameSettings.quietHours}
            <span class="mt-3 flex items-center gap-1.5">
                <TextInput
                    size="sm"
                    type="time"
                    value={frameSettings.quietHours.start}
                    onchange={(e) => setQuiet({ start: (e.target as HTMLInputElement).value })}
                    aria-label="Quiet from"
                />
                <span class="type-label text-(--text-2)">to</span>
                <TextInput
                    size="sm"
                    type="time"
                    value={frameSettings.quietHours.end}
                    onchange={(e) => setQuiet({ end: (e.target as HTMLInputElement).value })}
                    aria-label="Quiet until"
                />
            </span>
        {/if}
    </SettingsGroup>
</div>
