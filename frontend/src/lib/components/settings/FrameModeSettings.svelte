<script lang="ts">
    import { Plus, X } from '@lucide/svelte';
    import type { ModeRule } from '$lib/api';
    import { frameModeLabel, frameModes, modeDays, type FrameModeId, type ModeDay } from '$lib/constants/frameModes';
    import { clearOverride, modeStore, saveSchedule, setOverride } from '$lib/stores/ModeStore.svelte.ts';
    import { settingsHref } from '$lib/constants/settingsSections';
    import SegmentedControl from './SegmentedControl.svelte';

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

    const chip = 'type-label rounded-lg border border-(--border) px-3 py-1.5 transition-colors';
    const field =
        'type-label rounded-lg border border-(--border) bg-(--surface) px-2.5 py-1.5 text-(--text-1) outline-none focus:border-(--text-3)';
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
        <p class="type-label text-(--accent)">Can't reach the Display service — modes can't be changed right now.</p>
    {/if}

    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Showing now</p>
        {#if current}
            <p class="type-body text-(--text-1)">
                <span class="font-semibold">{frameModeLabel(current.mode)}</span>
                <span class="text-(--text-2)">· {why}</span>
            </p>
            {#if current.reason === 'override'}
                <button
                    onclick={() => clearOverride()}
                    class="type-label mt-2 text-(--accent) underline-offset-2 hover:underline"
                >
                    Back to schedule
                </button>
            {/if}
        {:else if modeStore.loaded}
            <p class="type-body text-(--text-2)">Unknown.</p>
        {/if}
    </div>

    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Show instead</p>
        <div class="mb-3 flex flex-wrap gap-2">
            {#each frameModes as m (m.id)}
                <button
                    onclick={() => (chosen = m.id)}
                    aria-pressed={chosen === m.id}
                    title={m.description}
                    aria-label={m.label}
                    class="{chip} {chosen === m.id
                        ? 'border-(--accent) bg-(--surface-hi) font-semibold text-(--text-1)'
                        : 'text-(--text-2) hover:text-(--text-1)'}"
                >
                    {m.label}
                </button>
            {/each}
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
            />
            <button
                onclick={show}
                disabled={modeStore.error}
                class="type-label rounded-lg bg-(--accent) px-4 py-1.5 font-semibold text-(--accent-fg) transition-opacity hover:opacity-90 disabled:opacity-50"
            >
                Show {frameModeLabel(chosen)}
            </button>
        </div>
    </div>

    <div>
        <p class="type-label mb-2 font-semibold tracking-widest text-(--text-2) uppercase">Schedule</p>
        <label class="type-label mb-3 flex flex-wrap items-center gap-2 text-(--text-2)">
            Otherwise show
            <select
                value={modeStore.schedule.default_mode}
                onchange={(e) =>
                    saveSchedule({
                        ...modeStore.schedule,
                        default_mode: (e.target as HTMLSelectElement).value as FrameModeId,
                    })}
                class={field}
            >
                {#each frameModes as m (m.id)}<option value={m.id}>{m.label}</option>{/each}
            </select>
        </label>

        <div class="flex flex-col gap-3">
            {#each rules as rule, i (i)}
                <div class="flex flex-wrap items-center gap-2 rounded-lg border border-(--border) p-3">
                    <select
                        value={rule.mode}
                        onchange={(e) => update(i, { mode: (e.target as HTMLSelectElement).value as FrameModeId })}
                        aria-label="Mode"
                        class={field}
                    >
                        {#each frameModes as m (m.id)}<option value={m.id}>{m.label}</option>{/each}
                    </select>
                    <div class="flex gap-1" role="group" aria-label="Days">
                        {#each modeDays as d (d.id)}
                            <button
                                onclick={() => toggleDay(i, d.id)}
                                aria-pressed={rule.days.includes(d.id)}
                                aria-label={d.id}
                                class="type-caption h-7 w-7 rounded-full border border-(--border) transition-colors {rule.days.includes(
                                    d.id
                                )
                                    ? 'border-(--accent) bg-(--accent) font-semibold text-(--accent-fg)'
                                    : 'text-(--text-2) hover:text-(--text-1)'}"
                            >
                                {d.label}
                            </button>
                        {/each}
                    </div>
                    <span class="flex items-center gap-1.5">
                        <input
                            type="time"
                            value={rule.start}
                            onchange={(e) => update(i, { start: (e.target as HTMLInputElement).value })}
                            aria-label="From"
                            class={field}
                        />
                        <span class="type-label text-(--text-2)">to</span>
                        <input
                            type="time"
                            value={rule.end}
                            onchange={(e) => update(i, { end: (e.target as HTMLInputElement).value })}
                            aria-label="To"
                            class={field}
                        />
                    </span>
                    <button
                        onclick={() => commit(rules.filter((_, j) => j !== i))}
                        aria-label="Remove rule"
                        class="ml-auto p-1 text-(--text-3) transition-colors hover:text-(--text-1)"
                    >
                        <X class="icon-sm" />
                    </button>
                    {#if rule.days.length === 0}
                        <p class="type-caption w-full text-(--accent)">Pick at least one day to save this rule.</p>
                    {/if}
                </div>
            {/each}
        </div>
        <button
            onclick={() => commit([...rules, { mode: 'daily', days: [...WEEKDAYS], start: '07:00', end: '09:00' }])}
            disabled={modeStore.error}
            class="type-label mt-3 flex items-center gap-1.5 rounded-full border border-(--border) px-4 py-1.5 text-(--text-1) transition-colors hover:border-(--text-2) hover:bg-(--surface-hi) disabled:opacity-50"
        >
            <Plus class="icon-sm" /> Add a time
        </button>
        <p class="type-caption mt-2 text-(--text-2)">
            The first matching time wins. A time that ends before it starts runs past midnight. Times are in your home’s
            time zone, set under <a href={settingsHref('home')} class="underline">Home</a>.
        </p>
    </div>
</div>
