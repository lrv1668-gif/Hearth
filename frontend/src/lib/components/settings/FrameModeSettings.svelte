<script lang="ts">
    import { Plus, X } from '@lucide/svelte';
    import type { ModeRule } from '$lib/api';
    import { frameModeLabel, frameModes, modeDays, type FrameModeId, type ModeDay } from '$lib/constants/frameModes';
    import { clearOverride, modeStore, saveSchedule, setOverride } from '$lib/stores/ModeStore.svelte.ts';
    import { settingsHref } from '$lib/constants/settingsSections';
    import Button from '../ui/Button.svelte';
    import ChoiceChips from '../ui/ChoiceChips.svelte';
    import DayPicker from '../ui/DayPicker.svelte';
    import IconButton from '../ui/IconButton.svelte';
    import InlineMessage from '../ui/InlineMessage.svelte';
    import SegmentedControl from '../ui/SegmentedControl.svelte';
    import Select from '../ui/Select.svelte';
    import TextInput from '../ui/TextInput.svelte';

    const HOUR_MS = 60 * 60 * 1000;
    const WEEKDAYS: ModeDay[] = ['mon', 'tue', 'wed', 'thu', 'fri'];

    let chosen = $state<FrameModeId>('daily');
    let hold = $state<'hour' | 'midnight' | 'forever'>('hour');

    const current = $derived(modeStore.current);

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

    function toggleDay(i: number, day: ModeDay) {
        const days = rules[i].days;
        update(i, { days: days.includes(day) ? days.filter((d) => d !== day) : [...days, day] });
    }
</script>

<!--
@component
What the frame shows and when: the mode showing now (with why, and a manual choice
that beats the schedule for an hour, until midnight, or until changed), then the
default mode and the time-of-day rules. Held by the Display service, shared by every
device; the frame's Button B steps to the next mode the same way.
-->
<div class="@container flex flex-col gap-6">
    {#if modeStore.error}
        <InlineMessage tone="error">Can't reach the Display service — modes can't be changed right now.</InlineMessage>
    {/if}

    <div>
        <p class="eyebrow mb-2">Showing now</p>
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
    </div>

    <div>
        <p class="eyebrow mb-2">Show instead</p>
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
    </div>

    <div>
        <p class="eyebrow mb-2">Schedule</p>
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
                <div class="flex flex-wrap items-center gap-2 rounded-lg border border-(--border) p-3">
                    <Select
                        size="sm"
                        value={rule.mode}
                        onchange={(e) => update(i, { mode: (e.target as HTMLSelectElement).value as FrameModeId })}
                        aria-label="Mode"
                    >
                        {#each frameModes as m (m.id)}<option value={m.id}>{m.label}</option>{/each}
                    </Select>
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
            The first matching time wins. A time that ends before it starts runs past midnight. Times are in your home’s
            time zone, set under <a href={settingsHref('home')} class="underline">Home</a>.
        </p>
    </div>
</div>
