<script lang="ts">
    import {
        settings,
        subscribeToTrainStop,
        unsubscribeFromTrainStop,
        updateTrainStopLineFilter,
    } from '$lib/stores/SettingsStore.svelte.ts';
    import { fetchTrainDepartures } from '$lib/api';
    import type { TrainStop } from '$lib/constants/trains';
    import { ChevronDown, ChevronUp, Trash2 } from '@lucide/svelte';

    let newLabel = $state('');
    let newStopKey = $state('');

    function isValidNewStop() {
        return newLabel.trim() !== '' && newStopKey.trim() !== '';
    }

    function handleAdd() {
        subscribeToTrainStop(newLabel.trim(), newStopKey.trim());
        newLabel = '';
        newStopKey = '';
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
        const [result] = await fetchTrainDepartures([stop.stopKey]);
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
            [stop.stopKey]: Array.from(byLine.values()).sort((a, b) => a.routeShortName.localeCompare(b.routeShortName)),
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
</script>

<div class="flex flex-col gap-6">
    <!-- Subscribed stops list -->
    <div class="flex flex-col gap-1">
        <p class="type-body font-medium text-(--text-1)">Watched stops</p>
        <div class="mt-2 flex flex-col">
            {#if settings.trainStops.length === 0}
                <p class="type-label py-4 text-center text-(--text-3)">No stops added yet.</p>
            {:else}
                {#each settings.trainStops as stop}
                    <div class="flex flex-col">
                        <div class="group flex items-center gap-3 rounded-lg px-3 py-2 hover:bg-(--surface-hi)">
                            <div class="min-w-0 flex-1">
                                <p class="type-body font-medium text-(--text-1)">{stop.label}</p>
                                <p class="type-label truncate text-(--text-2)">{stop.stopKey}</p>
                                {#if stop.lineFilter}
                                    <p class="type-label text-(--text-3)">
                                        {stop.lineFilter.length} line{stop.lineFilter.length === 1 ? '' : 's'} shown
                                    </p>
                                {/if}
                            </div>
                            <button
                                onclick={() => toggleExpand(stop)}
                                class="shrink-0 text-(--text-2) transition-colors hover:text-(--text-1)"
                                aria-label="Filter lines for {stop.label}"
                            >
                                {#if expandedStopKey === stop.stopKey}
                                    <ChevronUp class="icon-sm" />
                                {:else}
                                    <ChevronDown class="icon-sm" />
                                {/if}
                            </button>
                            <button
                                onclick={() => unsubscribeFromTrainStop(stop.stopKey)}
                                class="shrink-0 text-(--text-2) opacity-0 transition-opacity group-hover:opacity-100 hover:text-(--text-1)"
                                aria-label="Remove {stop.label}"
                            >
                                <Trash2 class="icon-sm" />
                            </button>
                        </div>
                        {#if expandedStopKey === stop.stopKey}
                            <div class="ml-3 flex flex-col gap-2 border-l border-(--border) py-2 pl-4">
                                {#if linesByStop[stop.stopKey] === 'loading'}
                                    <p class="type-label text-(--text-3)">Loading lines…</p>
                                {:else if linesByStop[stop.stopKey] === 'empty' || !linesByStop[stop.stopKey]}
                                    <p class="type-label text-(--text-3)">
                                        No departures right now to pick from — try again during service hours.
                                    </p>
                                {:else}
                                    {@const lines = linesByStop[stop.stopKey] as PickerLine[]}
                                    <div class="flex items-center justify-between">
                                        <p class="type-label text-(--text-2)">Show these lines</p>
                                        <div class="flex items-center gap-3">
                                            <button
                                                onclick={() => updateTrainStopLineFilter(stop.stopKey, undefined)}
                                                class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)"
                                            >
                                                Select all
                                            </button>
                                            <button
                                                onclick={() => updateTrainStopLineFilter(stop.stopKey, [])}
                                                class="type-label text-(--accent) transition-colors hover:text-(--accent-hi)"
                                            >
                                                Deselect all
                                            </button>
                                        </div>
                                    </div>
                                    {#each lines as line (line.key)}
                                        <label class="flex items-center gap-2">
                                            <input
                                                type="checkbox"
                                                checked={isLineChecked(stop, line.key)}
                                                onchange={() => toggleLine(stop, lines, line.key)}
                                                class="accent-(--accent)"
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
                {/each}
            {/if}
        </div>
    </div>

    <!-- Add stop form -->
    <div class="flex flex-col gap-3 border-t border-(--border) pt-4">
        <p class="type-body font-medium text-(--text-1)">Add a stop</p>
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
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Label</span>
                <input
                    type="text"
                    bind:value={newLabel}
                    placeholder="Union Station"
                    class="type-body w-full rounded-lg border border-(--border) bg-(--surface) px-3 py-2 text-(--text-1) transition-colors outline-none placeholder:text-(--text-4) focus:border-(--text-3)"
                />
            </label>
            <label class="flex flex-col gap-1">
                <span class="type-caption tracking-widest text-(--text-2) uppercase">Stop key</span>
                <input
                    type="text"
                    bind:value={newStopKey}
                    placeholder="s-9xj5pvewxk-us36~flatironstationeastside"
                    class="type-body w-full rounded-lg border border-(--border) bg-(--surface) px-3 py-2 text-(--text-1) transition-colors outline-none placeholder:text-(--text-4) focus:border-(--text-3)"
                />
            </label>
        </div>
        <div class="flex justify-end">
            <button
                onclick={handleAdd}
                disabled={!isValidNewStop()}
                class="type-label rounded-full border border-(--border) px-4 py-1.5 text-(--text-1) transition-colors hover:bg-(--surface-hi) disabled:pointer-events-none disabled:opacity-40"
            >
                Add stop
            </button>
        </div>
    </div>
</div>
