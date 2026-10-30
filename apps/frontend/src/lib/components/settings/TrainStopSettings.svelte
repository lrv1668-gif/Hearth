<script lang="ts">
    import {
        settings,
        subscribeToTrainStop,
        unsubscribeFromTrainStop,
        updateTrainStopLineFilter,
    } from '$lib/stores/SettingsStore.svelte.ts';
    import { api } from '$lib/api';
    import type { TrainStop } from '$lib/constants/trains';
    import { ChevronDown, ChevronUp, Plus } from '@lucide/svelte';
    import Button from '../ui/Button.svelte';
    import Field from '../ui/Field.svelte';
    import IconButton from '../ui/IconButton.svelte';
    import ListRow from '../ui/ListRow.svelte';
    import TextInput from '../ui/TextInput.svelte';

    let newLabel = $state('');
    let newStopKey = $state('');
    // The add form folds behind a button once there's a stop, so the list stays short.
    let adding = $state(false);
    const formOpen = $derived(adding || settings.trainStops.length === 0);

    function isValidNewStop() {
        return newLabel.trim() !== '' && newStopKey.trim() !== '';
    }

    function handleAdd() {
        subscribeToTrainStop(newLabel.trim(), newStopKey.trim());
        newLabel = '';
        newStopKey = '';
        adding = false;
    }

    interface PickerLine {
        key: string;
        routeShortName: string;
        headsign: string | null;
    }

    let expandedStopKey = $state<string | null>(null);
    let linesByStop = $state<Record<string, 'loading' | 'empty' | PickerLine[]>>({});

    async function toggleExpand(stop: TrainStop) {
        if (expandedStopKey === stop.stopKey) {
            expandedStopKey = null;
            return;
        }
        expandedStopKey = stop.stopKey;
        if (linesByStop[stop.stopKey]) return;

        linesByStop = { ...linesByStop, [stop.stopKey]: 'loading' };
        const [result] = (await api.trains.departures([stop.stopKey])) ?? [];
        const departures = result?.departures ?? [];
        if (departures.length === 0) {
            linesByStop = { ...linesByStop, [stop.stopKey]: 'empty' };
            return;
        }

        const byLine = new Map<string, PickerLine>();
        for (const d of departures) {
            const key = `${d.route_short_name}|${d.headsign ?? ''}|${d.mode}`;
            if (!byLine.has(key)) {
                byLine.set(key, { key, routeShortName: d.route_short_name, headsign: d.headsign });
            }
        }
        linesByStop = {
            ...linesByStop,
            [stop.stopKey]: Array.from(byLine.values()).sort((a, b) =>
                a.routeShortName.localeCompare(b.routeShortName)
            ),
        };
    }

    // An unset filter means "show all"; a set filter (even []) means "show only these" —
    // [] is how "Deselect all" hides every line for a stop.
    function isLineChecked(stop: TrainStop, lineKey: string): boolean {
        return !stop.lineFilter || stop.lineFilter.includes(lineKey);
    }

    function toggleLine(stop: TrainStop, lines: PickerLine[], lineKey: string) {
        const selected = new Set(lines.filter((l) => isLineChecked(stop, l.key)).map((l) => l.key));
        if (selected.has(lineKey)) {
            selected.delete(lineKey);
        } else {
            selected.add(lineKey);
        }
        updateTrainStopLineFilter(stop.stopKey, selected.size === lines.length ? undefined : Array.from(selected));
    }
    import SettingsGroup from '../ui/SettingsGroup.svelte';
</script>

<div class="flex flex-col gap-5">
    <!-- Subscribed stops list -->
    <SettingsGroup title="Watched stops">
        <div class="flex flex-col">
            {#each settings.trainStops as stop}
                {@const expanded = expandedStopKey === stop.stopKey}
                <div class="flex flex-col">
                    <ListRow
                        title={stop.label}
                        subtitle={stop.stopKey}
                        onremove={() => unsubscribeFromTrainStop(stop.stopKey)}
                    >
                        {#snippet meta()}
                            {#if stop.lineFilter}
                                <p class="type-label text-(--text-2)">
                                    {stop.lineFilter.length} line{stop.lineFilter.length === 1 ? '' : 's'} shown
                                </p>
                            {/if}
                        {/snippet}
                        {#snippet actions()}
                            <IconButton
                                label="Filter lines for {stop.label}"
                                aria-expanded={expanded}
                                onclick={() => toggleExpand(stop)}
                            >
                                {#if expanded}
                                    <ChevronUp class="icon-sm" />
                                {:else}
                                    <ChevronDown class="icon-sm" />
                                {/if}
                            </IconButton>
                        {/snippet}
                    </ListRow>
                    {#if expanded}
                        <div class="ml-3 flex flex-col gap-2 border-l border-(--border) py-2 pl-4">
                            {#if linesByStop[stop.stopKey] === 'loading'}
                                <p class="type-label text-(--text-2)">Loading lines…</p>
                            {:else if linesByStop[stop.stopKey] === 'empty' || !linesByStop[stop.stopKey]}
                                <p class="type-label text-(--text-2)">
                                    No departures right now to pick from — try again during service hours.
                                </p>
                            {:else}
                                {@const lines = linesByStop[stop.stopKey] as PickerLine[]}
                                <div class="flex items-center justify-between">
                                    <p class="type-label text-(--text-2)">Show these lines</p>
                                    <div class="flex items-center gap-3">
                                        <Button
                                            variant="link"
                                            onclick={() => updateTrainStopLineFilter(stop.stopKey, undefined)}
                                        >
                                            Select all
                                        </Button>
                                        <Button
                                            variant="link"
                                            onclick={() => updateTrainStopLineFilter(stop.stopKey, [])}
                                        >
                                            Deselect all
                                        </Button>
                                    </div>
                                </div>
                                {#each lines as line (line.key)}
                                    <label class="flex items-center gap-2">
                                        <input
                                            type="checkbox"
                                            checked={isLineChecked(stop, line.key)}
                                            onchange={() => toggleLine(stop, lines, line.key)}
                                            class="h-4 w-4 accent-(--accent)"
                                        />
                                        <span class="type-label text-(--text-1)">
                                            {line.routeShortName}{#if line.headsign}
                                                &nbsp;→ {line.headsign}
                                            {/if}
                                        </span>
                                    </label>
                                {/each}
                            {/if}
                        </div>
                    {/if}
                </div>
            {:else}
                <p class="type-label py-4 text-center text-(--text-2)">No stops added yet.</p>
            {/each}
        </div>
    </SettingsGroup>

    <!-- Add stop form -->
    {#if !formOpen}
        <SettingsGroup>
            <Button onclick={() => (adding = true)}><Plus class="icon-sm" /> Add a stop</Button>
        </SettingsGroup>
    {:else}
        <SettingsGroup title="Add a stop">
            <div class="flex flex-col gap-3">
                <p class="type-label text-(--text-2)">
                    The stop key is a Transitland stop identifier, not a plain stop name — look yours up at
                    <a
                        href="https://www.transit.land/"
                        target="_blank"
                        rel="noopener noreferrer"
                        class="text-(--accent) transition-colors hover:text-(--accent-hi)"
                    >
                        transit.land
                    </a>
                    (its Onestop ID, e.g. <span class="font-medium">s-9xj5pvewxk-us36~flatironstationeastside</span>).
                </p>
                <div class="flex flex-col gap-2">
                    <Field label="Label">
                        <TextInput type="text" bind:value={newLabel} placeholder="Union Station" class="w-full" />
                    </Field>
                    <Field label="Stop key">
                        <TextInput
                            type="text"
                            bind:value={newStopKey}
                            placeholder="s-9xj5pvewxk-us36~flatironstationeastside"
                            class="w-full"
                        />
                    </Field>
                </div>
                <div class="flex justify-end gap-2">
                    {#if settings.trainStops.length > 0}
                        <Button variant="ghost" onclick={() => (adding = false)}>Cancel</Button>
                    {/if}
                    <Button onclick={handleAdd} disabled={!isValidNewStop()}>Add stop</Button>
                </div>
            </div>
        </SettingsGroup>
    {/if}
</div>
